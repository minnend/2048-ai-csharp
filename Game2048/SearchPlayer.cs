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
            depth = 0;
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
            depth = si.depth;
            prob = si.prob;
            expectedScore = si.expectedScore;
            firstDir = si.firstDir;
            moveNext = si.moveNext;
            kids = new List<SearchInfo>();
        }

        public Board board;
        public int movesLeft;
        public int depth;
        public double prob;
        public double expectedScore;
        public Board.Direction firstDir;
        public bool moveNext;
        public List<SearchInfo> kids;
    }

    class SearchPlayer
    {
        public Board.Direction FindBestMove(Board startingBoard)
        {
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Find Best Move:");
            startingBoard.Print();

            Stopwatch sw = Stopwatch.StartNew();

            SearchInfo root = new SearchInfo();
            root.board = startingBoard;
            root.movesLeft = 30;
            root.expectedScore = EvalBoard(root.board);

            Queue<SearchInfo> Q = new Queue<SearchInfo>();
            Q.Enqueue(root);

            int maxDepthProcessed = 0;
            MoveInfo bestMove = new MoveInfo(Board.Direction.None);

            while (Q.Count > 0) {
                SearchInfo parent = Q.Dequeue();

                if (parent.depth > maxDepthProcessed) {
                    maxDepthProcessed = parent.depth;
                    if (!parent.moveNext)
                        bestMove = FindBestMove(root);
                    //Console.WriteLine("New depth: {0}  Just moved? {1}  Best Dir: {2}",
                    //    maxDepthProcessed, parent.moveNext ? "no" : "yes",
                    //    bestMove.dir);
                }
                
                if (sw.ElapsedMilliseconds > 200) break;

                if (parent.moveNext) { // make a move
                    if (parent.movesLeft <= 0) continue;

                    List<Board.Direction> moves = parent.board.GetLegalMoves();
                    if (moves.Count == 0) {
                        parent.expectedScore *= 0.01; // penalty for losing
                    }
                    else {
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
                            parent.kids.Add(si);
                            Q.Enqueue(si);
                        }                        
                    }
                }
                else { // add random tile
                    List<Coord> avail = parent.board.GetAvailableTiles();
                    double prob2 = 0.9 / avail.Count;
                    double prob4 = 0.1 / avail.Count;                    
                    foreach (Coord coord in avail) {
                        // 90% chance of adding a 2
                        SearchInfo si = new SearchInfo(parent);
                        si.board.board[coord.y][coord.x] = 2;
                        si.prob *= prob2;
                        si.expectedScore = EvalBoard(si.board);
                        ++si.depth;
                        si.moveNext = true;
                        parent.kids.Add(si);
                        Q.Enqueue(si);

                        // 10% chance of adding a 4
                        si = new SearchInfo(parent);
                        si.board.board[coord.y][coord.x] = 4;
                        si.prob *= prob4;
                        si.expectedScore = EvalBoard(si.board);
                        ++si.depth;
                        si.moveNext = true;
                        parent.kids.Add(si);
                        Q.Enqueue(si);
                    }
                }
            }

            Console.WriteLine("Move: {0}  (max depth: {1})", bestMove.dir, maxDepthProcessed);
            Board fb = root.board.Dup();
            fb.Slide(bestMove.dir);
            fb.Print();
            EvalBoard(fb, true);

            return bestMove.dir;
        }

        protected MoveInfo FindBestMove(SearchInfo root)
        {
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
                //Console.WriteLine("dir={0}  count={1}  depth={2}  =>  {3:0.000}",
                //    dir, mi.count, mi.depth, mi.score);
                if (mi.score > bestMove.score)
                    bestMove = mi;
            }

            return bestMove;
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

        protected double EvalBoard(Board board, bool bPrint = false)
        {
            double a = (board.Score == 0 ? 0.0 : Math.Log(board.Score));
            double b = Math.Log(board.MaxTile);
            double c = board.NumAvailableTiles;
            double d = board.NumMergeablePairs;
            double e = (double)board.GetCanonical().GetCanonicalScore() / (board.MaxTile);

            if (bPrint)
                Console.WriteLine("Eval: {0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}, {4:0.00}", a, b, c, d, e);
            return 0.1 * a
                + 0.1 * b
                + 0.5 * c
                + 0.1 * d
                + 0.2 * e;
        }
    }
}
