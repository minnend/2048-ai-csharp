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
            expectedDeath = si.expectedDeath;
            dir = si.dir;
            moveNext = si.moveNext;
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
            if (startingBoard.IsDead()) return Board.Direction.None;

            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Find Best Move:");
            startingBoard.Print();

            Stopwatch swTotal = Stopwatch.StartNew();

            SearchInfo root = new SearchInfo();
            root.board = startingBoard;
            root.movesLeft = 3;

            Queue<SearchInfo> Q = new Queue<SearchInfo>();
            Q.Enqueue(root);

            int maxDepthProcessed = 0;
            int nProcessed = 0;
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
                        si.dir = dir;
                        si.moveNext = false;
                        --si.movesLeft;
                        ++si.depth;
                        si.isDead = si.board.IsDead();
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
                    double prob2 = 0.9 / n;
                    double prob4 = 0.1 / n;
                    for (int i = 0; i < n; ++i) {
                        RandomTile rt = tiles[i];
                        if (rt.val == 2)
                            Q.Enqueue(BuildChild(parent, rt.ix, 1, prob2));
                        else
                            Q.Enqueue(BuildChild(parent, rt.ix, 2, prob4));
                    }
                }
            }
            //Console.WriteLine("#nodes: {0}", nProcessed);
            Board.Direction bestDir = FindBestMove(root);

            Console.WriteLine("Move: {0}  (max depth: {1})", bestDir, maxDepthProcessed);
            Board fb = root.board.Dup();
            fb.Slide(bestDir);
            fb.Print();
            EvalBoard(fb, true);

            return bestDir;
        }

        protected SearchInfo BuildChild(SearchInfo parent, int ix, byte val, double prob)
        {
            SearchInfo si = new SearchInfo(parent);
            si.board.board[ix] = val;
            si.prob *= prob;
            si.isDead = si.board.IsDead();
            si.dir = Board.Direction.None;
            ++si.depth;
            si.moveNext = true;
            parent.kids.Add(si);
            return si;
        }

        protected Board.Direction FindBestMove(SearchInfo root)
        {
            AccumDirInfo(root);
            SearchInfo best = new SearchInfo();
            foreach (SearchInfo si in root.kids) {
                if (best.dir == Board.Direction.None
                    || (si.expectedDeath < best.expectedDeath - 0.01)
                    || (Math.Abs(si.expectedDeath - best.expectedDeath) < 0.01
                        && si.expectedScore > best.expectedScore))
                    best = si;
            }
            return best.dir;
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
                Debug.Assert(!parent.moveNext);
                parent.expectedScore = EvalBoard(parent.board);                
                return;
            }

            if (parent.moveNext) { // kids are result of a move
                Debug.Assert(parent.dir == Board.Direction.None);
                double expectedScore = 0.0;
                double expectedDeath = 1.0;
                Board.Direction bestDir = Board.Direction.None;
                foreach (SearchInfo kid in parent.kids) {
                    AccumDirInfo(kid);
                    Debug.Assert(kid.dir != Board.Direction.None);
                    if (bestDir == Board.Direction.None
                        || kid.expectedDeath < expectedDeath
                        || (Math.Abs(kid.expectedDeath - expectedDeath) < 0.001
                            && kid.expectedScore > expectedScore)) {
                        expectedScore = kid.expectedScore;
                        expectedDeath = kid.expectedDeath;
                        bestDir = kid.dir;
                    }
                }
                Debug.Assert(bestDir != Board.Direction.None);
                parent.dir = bestDir;
                parent.expectedScore = expectedScore;
                parent.expectedDeath = expectedDeath;
            }
            else { // kids are result of a random new tile
                double expectedScore = 0.0;
                double expectedDeath = 0.0;
                double prob = 0.0;
                foreach (SearchInfo kid in parent.kids) {
                    AccumDirInfo(kid);
                    prob += kid.prob;
                    expectedScore += kid.expectedScore * kid.prob;
                    expectedDeath += kid.expectedDeath * kid.prob;
                }
                parent.expectedScore = expectedScore / prob;
                parent.expectedDeath = expectedDeath / prob;
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
