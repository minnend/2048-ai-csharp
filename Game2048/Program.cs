using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Game2048
{


    class Program
    {
        static void Test()
        {
            Board b1, b2;

            // Test SlideRight
            b1 = new Board(4, 1, new byte[] { 2, 2, 1, 1 });
            b2 = new Board(4, 1, new byte[] { 0, 0, 3, 2 });
            Debug.Assert(b1.SlideRight());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(4, 1, new byte[] { 1, 1, 1, 1 });
            b2 = new Board(4, 1, new byte[] { 0, 0, 2, 2 });
            Debug.Assert(b1.SlideRight());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(4, 1, new byte[] { 1, 2, 3, 4 });
            Debug.Assert(!b1.SlideRight());

            b1 = new Board(4, 1, new byte[] { 0, 0, 0, 1 });
            Debug.Assert(!b1.SlideRight());

            // Test SlideLeft
            b1 = new Board(4, 1, new byte[] { 1, 1, 2, 2 });
            b2 = new Board(4, 1, new byte[] { 2, 3, 0, 0 });
            Debug.Assert(b1.SlideLeft());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(4, 1, new byte[] { 1, 1, 1, 1 });
            b2 = new Board(4, 1, new byte[] { 2, 2, 0, 0 });
            Debug.Assert(b1.SlideLeft());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(4, 1, new byte[] { 1, 2, 3, 4 });
            Debug.Assert(!b1.SlideLeft());

            b1 = new Board(4, 1, new byte[] { 1, 0, 0, 0 });
            Debug.Assert(!b1.SlideLeft());

            // Test SlideUp
            b1 = new Board(1, 4, new byte[] { 1, 1, 2, 2 });
            b2 = new Board(1, 4, new byte[] { 2, 3, 0, 0 });
            Debug.Assert(b1.SlideUp());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(1, 4, new byte[] { 1, 1, 1, 1 });
            b2 = new Board(1, 4, new byte[] { 2, 2, 0, 0 });
            Debug.Assert(b1.SlideUp());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(1, 4, new byte[] { 1, 2, 3, 4 });
            Debug.Assert(!b1.SlideUp());

            b1 = new Board(1, 4, new byte[] { 1, 0, 0, 0 });
            Debug.Assert(!b1.SlideUp());

            // Test SlideDown
            b1 = new Board(1, 4, new byte[] { 2, 2, 1, 1 });
            b2 = new Board(1, 4, new byte[] { 0, 0, 3, 2 });
            Debug.Assert(b1.SlideDown());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(1, 4, new byte[] { 1, 1, 1, 1 });
            b2 = new Board(1, 4, new byte[] { 0, 0, 2, 2 });
            Debug.Assert(b1.SlideDown());
            Debug.Assert(b1.Equals(b2));

            b1 = new Board(1, 4, new byte[] { 1, 2, 3, 4 });
            Debug.Assert(!b1.SlideDown());

            b1 = new Board(1, 4, new byte[] { 0, 0, 0, 1 });
            Debug.Assert(!b1.SlideDown());

            // Test board rotation
            b1 = new Board(3, 3, new byte[] { 1, 2, 3,
                                              4, 5, 6,
                                              7, 8, 9 });
            b2 = new Board(3, 3, new byte[] { 7, 4, 1,
                                              8, 5, 2,
                                              9, 6, 3 });
            Debug.Assert(b1.GetRotated().Equals(b2));

            // Test canonicalization
            b1 = new Board(3, 3, new byte[] { 1, 2, 3,
                                              4, 5, 6,
                                              7, 8, 9 });
            b2 = new Board(3, 3, new byte[] { 7, 4, 1,
                                              8, 5, 2,
                                              9, 6, 3 });
            Debug.Assert(b1.GetCanonical().Equals(b2.GetCanonical()));

            b1 = new Board(3, 3, new byte[] { 0, 2, 0,
                                              0, 0, 0,
                                              0, 0, 2 });
            b2 = new Board(3, 3, new byte[] { 0, 2, 0,
                                              0, 0, 0,
                                              2, 0, 0 });
            Debug.Assert(b1.GetCanonical().Equals(b2.GetCanonical()));
        }

        static Board NewGame()
        {
            Board board = new Board(4, 4);
            board.AddRandomTile();
            board.AddRandomTile();
            return board;
        }

        static void PlayInteractive()
        {
            Board board = NewGame();
            while (true) {
                Console.WriteLine("Score: {0}", board.Score);
                board.Print();
                while (true) {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.LeftArrow) {
                        if (!board.SlideLeft()) continue;
                    }
                    else if (key.Key == ConsoleKey.RightArrow) board.SlideRight();
                    else if (key.Key == ConsoleKey.UpArrow) board.SlideUp();
                    else if (key.Key == ConsoleKey.DownArrow) board.SlideDown();
                    else if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape) Environment.Exit(0);
                    else continue;
                    break;
                }
                board.AddRandomTile();
                Console.WriteLine();
            }
        }

        static void EvalRandomPlayer()
        {
            using (StreamWriter file = new StreamWriter(@"g:\eval-random-moves.txt", true)) {
                Random rng = new Random();
                for (int iter = 0; iter < 900000; ++iter) {
                    Board board = NewGame();
                    while (true) {
                        List<Board.Direction> moves = board.GetLegalMoves();
                        if (moves.Count == 0) break;

                        board.Slide(moves[rng.Next(moves.Count)]);
                        board.AddRandomTile();
                    }
                    file.WriteLine("{0}  {1}", 1 << board.MaxTile, board.Score);
                    //Console.WriteLine("{0}  {1}", 1 << board.MaxTile, board.Score);
                    if (iter % 1000 == 0) Console.WriteLine("{0}", iter);
                }
            }
        }

        static void EvalSearchPlayer(SearchPlayer player)
        {
            Board board = NewGame();
            while (true) {
            //Stopwatch sw = Stopwatch.StartNew();
            //for (int i = 0; i < 10; ++i) {
                Board.Direction move = player.FindBestMove(board);
                if (move == Board.Direction.None) break;
                board.Slide(move);
                board.AddRandomTile();
            }
            //sw.Stop();
            Console.WriteLine("{0}  {1}", 1 << board.MaxTile, board.Score);
            //Console.WriteLine("Time: {0:0.0}ms", sw.ElapsedMilliseconds);
            //if (iter % 1000 == 0) Console.WriteLine("{0}", iter);

            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Test();

            //PlayInteractive();
            //EvalRandomPlayer();

            EvalSearchPlayer(new SearchPlayer());
        }
    }
}
