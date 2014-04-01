﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Game2048
{
    class MoveInfo
    {
        public MoveInfo(Board.Direction dir)
        {
            this.dir = dir;
            score = 0.0;
            death = 0.0;
            wsum = 0.0;
            count = 0;
            depth = 0;
        }

        public Board.Direction dir;
        public double score, death, wsum;
        public int count;
        public int depth;
    }

    class SearchInfo
    {
        public SearchInfo()
        {
            movesLeft = 0;
            depth = 0;
            prob = 1.0;
            expectedScore = 0.0;
            firstDir = Board.Direction.None;
            moveNext = true;
            isDead = false;
            kids = new List<SearchInfo>();
        }

        public SearchInfo(SearchInfo si)
        {
            board = si.board.Dup();
            movesLeft = si.movesLeft;
            depth = si.depth;
            prob = si.prob;
            expectedScore = si.expectedScore;
            firstDir = si.firstDir;
            moveNext = si.moveNext;
            isDead = si.isDead;
            kids = new List<SearchInfo>();
        }

        public Board board;
        public int movesLeft;
        public int depth;
        public double prob;
        public double expectedScore;
        public Board.Direction firstDir;
        public bool moveNext;
        public bool isDead;
        public List<SearchInfo> kids;
    }

    class RandomTile
    {
        public RandomTile(int ix, int val)
        {
            this.ix = ix;
            this.val = val;
        }
        public int ix, val;
    }

    class SearchPlayer
    {
        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Board.rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public Board.Direction FindBestMove(Board startingBoard)
        {
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Find Best Move:");
            startingBoard.Print();

            Stopwatch swTotal = Stopwatch.StartNew();

            SearchInfo root = new SearchInfo();
            root.board = startingBoard;
            root.movesLeft = 3;
            root.expectedScore = EvalBoard(root.board);

            Queue<SearchInfo> Q = new Queue<SearchInfo>();
            Q.Enqueue(root);

            int maxDepthProcessed = 0;
            MoveInfo bestMove = new MoveInfo(Board.Direction.None);

            while (Q.Count > 0) {
                SearchInfo parent = Q.Dequeue();

                if (parent.depth > maxDepthProcessed) {
                    maxDepthProcessed = parent.depth;
                    //bestMove = FindBestMove(root);
                }
                if (parent.isDead) continue;
                if (parent.movesLeft <= 0) continue;

                //if (swTotal.ElapsedMilliseconds > 500) break;

                if (parent.moveNext) { // make a move
                    List<Board.Direction> moves = parent.board.GetLegalMoves();
                    List<Board> uniqueBoards = new List<Board>();
                    List<Board.Direction> uniqueMoves = new List<Board.Direction>();
                    foreach (Board.Direction dir in moves) {
                        //Console.WriteLine("Possible Move: {0}", dir);
                        Board b = parent.board.Dup();
                        if (!b.Slide(dir)) continue;
                        Board cb = b.GetCanonical();
                        if (uniqueBoards.Contains(cb)) {
                            //Console.WriteLine("Dup dir: {0}", dir);
                            continue;
                        }
                        uniqueBoards.Add(cb);
                        uniqueMoves.Add(dir);
                    }

                    foreach (Board.Direction dir in uniqueMoves) {
                        //Console.WriteLine("Search Move: {0}", dir);
                        SearchInfo si = new SearchInfo(parent);
                        si.board.Slide(dir);
                        si.expectedScore = EvalBoard(si.board);
                        if (si.firstDir == Board.Direction.None)
                            si.firstDir = dir;
                        si.moveNext = false;
                        --si.movesLeft;
                        ++si.depth;
                        si.isDead = si.board.IsDead();
                        if (si.isDead) si.expectedScore *= 0.01; // penalty for dying
                        parent.kids.Add(si);
                        Q.Enqueue(si);
                    }
                }
                else { // add random tile
                    List<int> avail = parent.board.GetAvailableTiles();
                    List<RandomTile> tiles = new List<RandomTile>();
                    foreach (int ix in avail) {
                        tiles.Add(new RandomTile(ix, 2));
                        tiles.Add(new RandomTile(ix, 4));
                    }
                    //Shuffle(tiles);
                    //int n = Math.Min(10, tiles.Count);
                    int n = tiles.Count; // TODO

                    double prob2 = 0.9 / avail.Count;
                    double prob4 = 0.1 / avail.Count;
                    for(int i=0; i<n; ++i){
                        RandomTile rt = tiles[i];
                        if (rt.val == 2)
                            Q.Enqueue(BuildChild(parent, rt.ix, 1, prob2));
                        else
                            Q.Enqueue(BuildChild(parent, rt.ix, 2, prob4));
                    }
                }
            }
            bestMove = FindBestMove(root);

            Console.WriteLine("Move: {0}  (max depth: {1})", bestMove.dir, maxDepthProcessed);
            Board fb = root.board.Dup();
            fb.Slide(bestMove.dir);
            fb.Print();
            EvalBoard(fb, true);

            return bestMove.dir;
        }

        protected SearchInfo BuildChild(SearchInfo parent, int ix, byte val, double prob)
        {
            SearchInfo si = new SearchInfo(parent);
            si.board.board[ix] = val;
            si.prob *= prob;
            si.expectedScore = EvalBoard(si.board);
            si.isDead = si.board.IsDead();
            if (si.isDead) si.expectedScore *= 0.01; // penalty for dying
            ++si.depth;
            si.moveNext = true;
            parent.kids.Add(si);
            return si;
        }

        protected MoveInfo FindBestMove(SearchInfo root)
        {
            // find best move
            Dictionary<Board.Direction, MoveInfo> map = new Dictionary<Board.Direction, MoveInfo>();
            foreach (Board.Direction dir in Board.AllDirs)
                map.Add(dir, new MoveInfo(dir));
            AccumDirInfo(root, map);
            MoveInfo bestMove = new MoveInfo(Board.Direction.None);
            foreach (Board.Direction dir in Board.AllDirs) {
                MoveInfo mi = map[dir];
                if (mi.count < 1) continue;
                mi.score /= mi.wsum;
                mi.death /= mi.wsum;
                //if (mi.depth >= 5)
                //    Console.WriteLine("dir={0}  count={1}  depth={2}  death={3:0.000}  =>  {4:0.000}",
                //        dir, mi.count, mi.depth, mi.death, mi.score);
                if (bestMove.dir == Board.Direction.None
                    || (mi.death < bestMove.death - 0.01)
                    || (Math.Abs(mi.death - bestMove.death) < 0.01
                        && mi.score > bestMove.score))
                    bestMove = mi;
            }

            return bestMove;
        }

        protected void AccumDirInfo(SearchInfo node,
                                    Dictionary<Board.Direction, MoveInfo> map)
        {
            if (node.kids.Count == 0) {
                if (node.firstDir == Board.Direction.None) return;
                MoveInfo mi = map[node.firstDir];
                mi.score += node.expectedScore * node.prob;
                if (node.isDead) mi.death += node.prob;
                mi.wsum += node.prob;
                mi.count++;
                if (node.depth > mi.depth) mi.depth = node.depth;
            }
            else {
                foreach (SearchInfo kid in node.kids)
                    AccumDirInfo(kid, map);
            }
        }

        protected double EvalBoard(Board board, bool bPrint = false)
        {
            double a = (board.Score == 0 ? 0.0 : Math.Log(board.Score));
            double b = board.MaxTile;
            double c = board.NumAvailableTiles;
            double d = board.NumMergeablePairs;
            double e = Math.Log(board.GetCanonical().GetCanonicalScore());

            if (bPrint)
                Console.WriteLine("Eval: {0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}, {4:0.00}", a, b, c, d, e);
            return 0.1 * a
                + 0.3 * b
                + 0.1 * c
                + 0.1 * d
                + 0.4 * e;
        }
    }
}
