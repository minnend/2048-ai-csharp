using System;
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
            wsum = 0.0;
            count = 0;
            depth = 0;
        }

        public Board.Direction dir;
        public double score, wsum;
        public int count;
        public int depth;
    }

    class SearchInfo
    {
        public SearchInfo()
        {
            movesLeft = 0;
            prob = 1.0;
            expectedScore = 0.0;
            firstDir = Board.Direction.None;
            moveNext = true;
            kids = new List<SearchInfo>();
        }

        public SearchInfo(SearchInfo si)
        {
            board = si.board.Dup();
            movesLeft = si.movesLeft;
            prob = si.prob;
            expectedScore = si.expectedScore;
            firstDir = si.firstDir;
            moveNext = si.moveNext;
            kids = new List<SearchInfo>();
        }

        public Board board;
        public int movesLeft;
        public double prob;
        public double expectedScore;
        public Board.Direction firstDir;
        public bool moveNext;
        public List<SearchInfo> kids;
    }

    class SearchPlayer
    {
        public static void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
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

            Stopwatch sw = Stopwatch.StartNew();

            SearchInfo root = new SearchInfo();
            root.board = startingBoard;
            root.movesLeft = 3;
            root.expectedScore = EvalBoard(root.board);

            Queue<SearchInfo> Q = new Queue<SearchInfo>();
            Q.Enqueue(root);

            while (Q.Count > 0) {
                //if (sw.ElapsedMilliseconds > 250) break;

                SearchInfo parent = Q.Dequeue();
                if (parent.moveNext) { // make a move
                    if (parent.movesLeft <= 0) continue;

                    //Console.WriteLine("Search score={0:0.000},  movesLeft={1}:",
                    //    EvalBoard(parent.board), parent.movesLeft);
                    //parent.board.Print();

                    List<Board.Direction> moves = parent.board.GetLegalMoves();
                    if (moves.Count == 0) {
                        parent.expectedScore *= 0.1; // penalty for losing
                    }
                    else {
                        Shuffle(moves);
                        foreach (Board.Direction dir in moves) {
                            SearchInfo si = new SearchInfo(parent);
                            if (!si.board.Slide(dir)) continue;
                            si.expectedScore = EvalBoard(si.board);
                            if (si.firstDir == Board.Direction.None)
                                si.firstDir = dir;
                            si.moveNext = false;
                            --si.movesLeft;
                            parent.kids.Add(si);
                            Q.Enqueue(si);
                        }
                    }
                }
                else { // add random tile
                    List<Coord> avail = parent.board.GetAvailableTiles();
                    double prob2 = 0.9 / avail.Count;
                    double prob4 = 0.1 / avail.Count;
                    Shuffle(avail);
                    foreach (Coord coord in avail) {
                        // 90% chance of adding a 2
                        SearchInfo si = new SearchInfo(parent);
                        si.board.board[coord.y][coord.x] = 2;
                        si.prob *= prob2;
                        si.expectedScore = EvalBoard(si.board);
                        si.moveNext = true;
                        parent.kids.Add(si);
                        Q.Enqueue(si);

                        // 10% chance of adding a 4
                        si = new SearchInfo(parent);
                        si.board.board[coord.y][coord.x] = 4;
                        si.prob *= prob4;
                        si.expectedScore = EvalBoard(si.board);
                        si.moveNext = true;
                        parent.kids.Add(si);
                        Q.Enqueue(si);
                    }
                }
            }

            // find best move
            Dictionary<Board.Direction, MoveInfo> map = new Dictionary<Board.Direction, MoveInfo>();
            foreach (Board.Direction dir in Board.AllDirs)
                map.Add(dir, new MoveInfo(dir));
            AccumDirInfo(root, map, 0);
            MoveInfo bestMove = new MoveInfo(Board.Direction.None);
            foreach (Board.Direction dir in Board.AllDirs) {
                MoveInfo mi = map[dir];
                if (mi.count < 1) continue;
                mi.score /= mi.wsum;
                Console.WriteLine("dir={0}  count={1}  depth={2}  =>  {3:0.000}",
                    dir, mi.count, mi.depth, mi.score);
                if (mi.score > bestMove.score)
                    bestMove = mi;
            }

            Console.WriteLine("Move: {0}", bestMove.dir);
            Board b = startingBoard.Dup();
            b.Slide(bestMove.dir);
            b.Print();
            EvalBoard(b, true);

            return bestMove.dir;
        }

        protected void AccumDirInfo(SearchInfo node,
                                    Dictionary<Board.Direction, MoveInfo> map,
                                    int depth)
        {
            if (node.kids.Count == 0) {
                if (node.firstDir == Board.Direction.None) return;
                MoveInfo mi = map[node.firstDir];
                mi.score += node.expectedScore;
                mi.wsum += node.prob;
                mi.count++;
                if (mi.depth < depth) mi.depth = depth;
            }
            else {
                foreach (SearchInfo kid in node.kids)
                    AccumDirInfo(kid, map, kid.moveNext ? depth : depth + 1);
            }
        }

        protected double EvalBoard(Board board, bool bPrint=false)
        {
            double a = (board.Score == 0 ? 0.0 : Math.Log(board.Score));
            double b = Math.Log(board.MaxTile);
            double c = (double)board.NumAvailableTiles / board.NumTiles;

            int maxMergeable = (board.Width - 1) * board.Height
                + (board.Height - 1) * board.Width;
            double d = (double)board.NumMergeablePairs / maxMergeable;

            if (bPrint)
                Console.WriteLine("Eval: {0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}", a, b, c, d);
            return 0.1 * a
                + 0.4 * b
                + 0.25 * c
                + 0.25 * d;
        }
    }
}
