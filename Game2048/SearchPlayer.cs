using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Game2048
{
    class SearchInfo
    {
        public SearchInfo()
        {
            movesLeft = 0;
            depth = 0;
            prob = 1.0;
            expectedScore = 0.0;
            expectedDeath = 0.0;
            dir = Board.Direction.None;
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
            expectedDeath = si.expectedDeath;
            dir = si.dir;
            isDead = si.isDead;
            kids = new List<SearchInfo>();
        }

        public Board board;
        public int movesLeft;
        public int depth;
        public double prob;
        public double expectedScore;
        public double expectedDeath;
        public Board.Direction dir;
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

        protected int GenPostMoveBoards(SearchInfo parent)
        {
            List<Board.Direction> moves = parent.board.GetLegalMoves();
            List<Board> uniqueBoards = new List<Board>();
            List<Board.Direction> uniqueMoves = new List<Board.Direction>();
            foreach (Board.Direction dir in moves) {
                //Console.WriteLine("Possible Move: {0}", dir);
                Board b = parent.board.Dup();
                if (!b.Slide(dir)) continue;
                Board cb = b.GetCanonical();
                if (uniqueBoards.Contains(cb)) continue;
                uniqueBoards.Add(cb);
                uniqueMoves.Add(dir);
            }

            foreach (Board.Direction dir in uniqueMoves) {
                SearchInfo si = new SearchInfo(parent);
                si.board.Slide(dir);
                si.dir = dir;
                --si.movesLeft;
                ++si.depth;
                si.isDead = si.board.IsDead();
                parent.kids.Add(si);
            }

            return parent.kids.Count;
        }

        public Board.Direction FindBestMove(Board startingBoard)
        {
            if (startingBoard.IsDead()) return Board.Direction.None;

            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Find Best Move:");
            startingBoard.Print();

            Stopwatch swTotal = Stopwatch.StartNew();

            // seed search with possible moves from starting board
            SearchInfo root = new SearchInfo();
            root.board = startingBoard;
            root.movesLeft = 3;
            if (GenPostMoveBoards(root) < 1) return Board.Direction.None;

            Queue<SearchInfo> Q = new Queue<SearchInfo>();
            foreach (SearchInfo si in root.kids)
                Q.Enqueue(si);

            int maxDepthProcessed = 0;
            int nProcessed = 1;
            while (Q.Count > 0) {
                SearchInfo parent = Q.Dequeue();
                ++nProcessed;

                if (parent.depth > maxDepthProcessed) {
                    maxDepthProcessed = parent.depth;
                    //bestMove = FindBestMove(root);
                }
                if (parent.isDead) continue;
                if (parent.movesLeft <= 0) continue;

                //if (swTotal.ElapsedMilliseconds > 500) break;

                BuildSuccessors(parent);
                foreach (SearchInfo si in parent.kids)
                    Q.Enqueue(si);
            }
            Console.WriteLine("#nodes: {0}", nProcessed);
            AccumDirInfo(root);
            Board.Direction bestDir = root.dir;

            Console.WriteLine("Move: {0}  (max depth: {1})", bestDir, maxDepthProcessed);
            Board fb = root.board.Dup();
            fb.Slide(bestDir);
            fb.Print();
            EvalBoard(fb, true);

            return bestDir;
        }

        protected bool BuildSuccessors(SearchInfo parent)
        {
            List<int> avail = parent.board.GetAvailableTiles();
            double prob2 = parent.prob * 0.9 / avail.Count;
            double prob4 = parent.prob * 0.1 / avail.Count;

            //Console.WriteLine("Parent Board ({0}):", parent.dir);
            //parent.board.Print();
            //Console.WriteLine();

            foreach (Board.Direction dir in Board.AllDirs) {                
                if (!BuildSuccessors(parent, avail, dir, 1, prob2)
                    || !BuildSuccessors(parent, avail, dir, 2, prob4)) return false;
            }
            //Console.WriteLine("Successors: {0}  (max: {1})", parent.kids.Count, avail.Count * 8);            
            return true;
        }

        protected bool BuildSuccessors(SearchInfo parent, List<int> avail,
            Board.Direction dir, byte val, double prob)
        {            
            int nTested = 0;
            Dictionary<Board, SearchInfo> uniqueBoards = new Dictionary<Board, SearchInfo>();
            foreach (int ix in avail) {
                SearchInfo si = new SearchInfo(parent);
                si.board.AddTile(ix, val);
                //Console.WriteLine("Random Tile:");
                //si.board.Print();
                if (!si.board.Slide(dir)) continue;

                ++nTested;

                ++si.depth;
                --si.movesLeft;
                si.isDead = si.board.IsDead();
                si.dir = dir;
                si.prob = prob;
                Board canonical = si.board.GetCanonical();

                //Console.WriteLine("Board ({0}):", dir);
                //si.board.Print();
                //Console.WriteLine("Canonical ({0}):", uniqueBoards.ContainsKey(canonical) ? "dup" : "new");
                //canonical.Print();

                if (uniqueBoards.ContainsKey(canonical))
                    uniqueBoards[canonical].prob += prob;
                else {
                    uniqueBoards.Add(canonical, si);
                    parent.kids.Add(si);
                }
            }
            //Console.WriteLine("Kept ({0},{1}): {2} / {3}", dir, 2*val, uniqueBoards.Count, nTested);
            return true;
        }

        protected void AccumDirInfo(SearchInfo parent)
        {
            //Console.WriteLine("Accum: depth={0}  dir={1}  prob={2}  kids={3}",
            //    parent.depth, parent.dir, parent.prob, parent.kids.Count);
            if (parent.isDead) {
                Debug.Assert(parent.dir == Board.Direction.None);
                Debug.Assert(parent.kids.Count == 0);
                parent.expectedScore = EvalBoard(parent.board);
                parent.expectedDeath = 1.0;
                return;
            }

            if (parent.kids.Count == 0) {
                parent.expectedScore = EvalBoard(parent.board);
                return;
            }

            foreach (SearchInfo kid in parent.kids) {
                Debug.Assert(kid.dir != Board.Direction.None);
                AccumDirInfo(kid);
            }

            SearchInfo[] accums = new SearchInfo[4];
            for (int i = 0; i < 4; ++i) {
                accums[i] = new SearchInfo();
                accums[i].dir = (Board.Direction)i;
                accums[i].prob = 0.0;
            }

            //Console.WriteLine("Parent: {0} depth={1}  kids={2}  prob={3:0.000}",
            //    parent.dir, parent.depth, parent.kids.Count, parent.prob);

            // accumulate weigthed scores of children
            foreach (SearchInfo kid in parent.kids) {
                SearchInfo accum = accums[(int)kid.dir];
                //Console.WriteLine(" Kid: {0} {1:0.000}", kid.dir, kid.prob);
                accum.expectedScore += kid.expectedScore * kid.prob;
                accum.expectedDeath += kid.expectedDeath * kid.prob;
                accum.prob += kid.prob;
            }

            // normalize weighted scores
            foreach (SearchInfo si in accums) {
                if (si.prob <= 0.0) continue;
                si.expectedScore /= si.prob;
                si.expectedDeath /= si.prob;
                //Console.WriteLine(" accum {0}: {1:0.000}  {2:0.000}", si.dir, si.prob, si.expectedScore);
            }

            // choose best move
            double expectedScore = 0.0;
            double expectedDeath = 1.0;
            Board.Direction bestDir = Board.Direction.None;
            foreach (SearchInfo si in accums) {
                if (si.prob <= 0.0) continue;
                if (bestDir == Board.Direction.None
                    || si.expectedDeath < expectedDeath
                    || (Math.Abs(si.expectedDeath - expectedDeath) < 0.001
                        && si.expectedScore > expectedScore)) {
                    expectedScore = si.expectedScore;
                    expectedDeath = si.expectedDeath;
                    bestDir = si.dir;
                }
            }
            Debug.Assert(bestDir != Board.Direction.None);
            parent.expectedScore = expectedScore;
            parent.expectedDeath = expectedDeath;
            if (parent.dir == Board.Direction.None)
                parent.dir = bestDir;
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
