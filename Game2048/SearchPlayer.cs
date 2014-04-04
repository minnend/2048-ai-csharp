using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Game2048
{
    class SearchPlayer
    {
        public Board.Direction FindBestMove(Board startingBoard)
        {
            if (startingBoard.IsDead()) return Board.Direction.None;

            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Find Best Move:");
            startingBoard.Print();

            Stopwatch swTotal = Stopwatch.StartNew();

            Dictionary<Board, NodeMove> layerMoves = new Dictionary<Board, NodeMove>();
            Dictionary<Board, NodeRandomTile> layerRandom = new Dictionary<Board, NodeRandomTile>();

            // seed search with possible moves from starting board
            NodeMove root = new NodeMove();
            root.board = startingBoard;

            layerMoves.Add(root.board.GetCanonical(), root);
            int nNodes = 1;

            int maxMoves = 40;
            int maxMS = 100;            
            int nMoves = 0;
            while (true) {
                //Console.WriteLine("Move Step| Depth: {0}  move nodes: {1}", depth, layerMoves.Count);
                ExpandMoves(layerMoves, layerRandom);
                //Console.WriteLine(" Next Layer: {0}", layerRandom.Count);
                layerMoves.Clear();
                nNodes += layerRandom.Count;                
                ++nMoves;
                if (nMoves >= maxMoves) break;
                if (swTotal.ElapsedMilliseconds >= maxMS) break;

                //Console.WriteLine("Random Tile Step| Depth: {0}  random nodes: {1}", depth, layerRandom.Count);                
                ExpandRandomTiles(layerMoves, layerRandom);
                //Console.WriteLine(" Next Layer: {0}", layerMoves.Count);
                layerRandom.Clear();
                nNodes += layerMoves.Count;                
                if (swTotal.ElapsedMilliseconds >= maxMS) break;
            }
            Console.WriteLine("#nodes: {0}  ms: {1}", nNodes, swTotal.ElapsedMilliseconds);
            AccumInfo(root);

            Console.WriteLine("Move: {0}  (moves: {1})", root.bestDir, nMoves);
            Board fb = root.board.Dup();
            fb.Slide(root.bestDir);
            fb.Print();
            //EvalBoard(fb, true);

            return root.bestDir;
        }

        protected bool ExpandMoves(Dictionary<Board, NodeMove> layerMoves,
            Dictionary<Board, NodeRandomTile> layerRandom)
        {
            foreach (NodeMove parent in layerMoves.Values) {
                if (parent.isDead) continue;
                
                //Console.WriteLine("Parent:");
                //parent.board.Print();

                foreach (Board.Direction dir in parent.board.GetLegalMoves()) {
                    Board board = parent.board.Dup();
                    board.Slide(dir);
                    Board canonical = board.GetCanonical();

                    //Console.WriteLine("Move: {0}", dir);
                    //board.Print();
                    //Console.WriteLine("Canonical:");
                    //canonical.Print();

                    if (layerRandom.ContainsKey(canonical)) { // repeat board
                        if (!parent.boards.Contains(canonical)) {
                            parent.kids.Add(dir, layerRandom[canonical]);
                            parent.boards.Add(canonical);
                        }
                    }
                    else { // new board
                        NodeRandomTile kid = new NodeRandomTile();
                        kid.board = board;
                        layerRandom.Add(canonical, kid);
                        parent.kids.Add(dir, kid);
                        parent.boards.Add(canonical);
                    }
                }
            }
            return true;
        }

        protected void ExpandRandomTiles(Dictionary<Board, NodeMove> layerMoves,
            Dictionary<Board, NodeRandomTile> layerRandom)
        {
            foreach (NodeRandomTile parent in layerRandom.Values) {                 
                List<int> avail = parent.board.GetAvailableTiles();
                double prob2 = 0.9 / avail.Count;
                double prob4 = 0.1 / avail.Count;

                foreach (int ix in avail) {
                    ExpandRandomTile(parent, ix, 1, prob2, layerMoves);
                    ExpandRandomTile(parent, ix, 2, prob4, layerMoves);
                }
            }            
        }

        protected void ExpandRandomTile(NodeRandomTile parent, int ix, byte val, double prob,
                                        Dictionary<Board, NodeMove> layerMoves)
        {
            Board board = parent.board.Dup();
            board.AddTile(ix, 1);
            Board canonical = board.GetCanonical();

            if (layerMoves.ContainsKey(canonical)) { // repeat board
                if (parent.kids.ContainsKey(canonical))
                    parent.kids[canonical].prob += prob;
                else
                    parent.kids[canonical] = new NodeMoveWrapper(layerMoves[canonical], prob);                
            }
            else { // we have a new board
                NodeMove kid = new NodeMove();
                kid.board = board;
                kid.isDead = kid.board.IsDead();                

                layerMoves.Add(canonical, kid);                
                parent.kids[canonical] = new NodeMoveWrapper(kid, prob);
            }            
        }

        protected void AccumInfo(NodeMove parent)
        {
            if (parent.bAccumulated) return;

            if (parent.isDead) {
                Debug.Assert(parent.kids.Count == 0);
                parent.expectedDeath = 1.0;
            }
            else if (parent.kids.Count == 0) {
                parent.expectedScore = EvalBoard(parent.board);
                Debug.Assert(parent.expectedDeath == 0.0);
            }
            else {
                // max over kids
                Debug.Assert(parent.bestDir == Board.Direction.None);
                parent.expectedScore = double.NegativeInfinity;
                parent.expectedDeath = double.PositiveInfinity;
                foreach (KeyValuePair<Board.Direction, NodeRandomTile> pair in parent.kids) {
                    Board.Direction dir = pair.Key;
                    NodeRandomTile kid = pair.Value;
                    AccumInfo(kid);
                    if (parent.bestDir == Board.Direction.None
                        || kid.expectedDeath < parent.expectedDeath
                        || (Math.Abs(kid.expectedDeath - parent.expectedDeath) < 0.001)
                        && (kid.expectedScore > parent.expectedScore)) {
                        parent.bestDir = dir;
                        parent.expectedScore = kid.expectedScore;
                        parent.expectedDeath = kid.expectedDeath;
                    }
                }
                Debug.Assert(parent.bestDir != Board.Direction.None);
            }

            parent.bAccumulated = true;
        }

        protected void AccumInfo(NodeRandomTile parent)
        {
            if (parent.bAccumulated) return;

            if (parent.kids.Count == 0) {
                parent.expectedScore = EvalBoard(parent.board);
                Debug.Assert(parent.expectedDeath == 0.0);
            }
            else {
                // expectation over kids
                Debug.Assert(parent.expectedScore == 0.0);
                Debug.Assert(parent.expectedDeath == 0.0);
                double wsum = 0.0;
                foreach (NodeMoveWrapper wrapper in parent.kids.Values) {
                    Debug.Assert(wrapper.prob > 0.0);                    
                    wsum += wrapper.prob;
                    AccumInfo(wrapper.node);
                    parent.expectedScore += wrapper.node.expectedScore * wrapper.prob;
                    parent.expectedDeath += wrapper.node.expectedDeath * wrapper.prob;
                }
                parent.expectedScore /= wsum;
                parent.expectedDeath /= wsum;
            }
            parent.bAccumulated = true;
        }

        protected double EvalBoard(Board board, bool bPrint = false)
        {
            double a = (board.Score == 0 ? 0.0 : Math.Log(board.Score));
            double b = board.MaxTile;
            double c = board.NumAvailableTiles;
            double d = board.NumMergeablePairs;
            double e = Math.Log(board.GetCanonical().GetCanonicalScore());
            double f = Math.Log(board.SmoothnessCost + 1);

            //if (bPrint)
            //    Console.WriteLine("Eval: {0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}, {4:0.00}, {5:0.00}", a, b, c, d, e, f);
            return 2.0 * a
                + 1.0 * b
                + 1.0 * c
                + 1.0 * d
                + 100.0 * e
                - 5.0 * f;
        }
    }
}
