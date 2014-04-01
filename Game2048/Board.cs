using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game2048
{
    class Board
    {
        public enum Direction { Up, Right, Down, Left, None }
        public static Direction[] AllDirs = new Direction[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

        public static Random rng = new Random(1234);

        public Board(int w, int h)
        {
            Width = w;
            Height = h;
            NumTiles = Width * Height;
            board = new byte[NumTiles];
            score = 0;
        }

        public Board(int w, int h, byte[] data)
            : this(w, h)
        {
            Array.Copy(data, board, NumTiles);
        }

        public bool HasOpenTiles()
        {
            return (NumAvailableTiles > 0);
        }

        public int NumAvailableTiles
        {
            get
            {
                int n = 0;
                for (int i = 0; i < NumTiles; ++i)
                    if (board[i] == 0) ++n;
                return n;
            }
        }

        public List<Coord> GetAvailableTiles()
        {
            List<Coord> tiles = new List<Coord>();
            int ix = 0;
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    if (board[ix++] == 0)
                        tiles.Add(new Coord(x, y));
            return tiles;
        }

        public bool IsDead()
        {
            if (HasOpenTiles()) return false;
            return (GetLegalMoves().Count() == 0);
        }

        public List<Direction> GetLegalMoves()
        {
            List<Direction> legal = new List<Direction>();
            foreach (Direction dir in AllDirs)
                if (CanSlide(dir)) legal.Add(dir);
            return legal;
        }

        public bool CanSlide(Direction dir)
        {
            return Dup().Slide(dir);
        }

        public bool Slide(Direction dir)
        {
            switch (dir) {
                case Direction.Up: return SlideUp();
                case Direction.Right: return SlideRight();
                case Direction.Down: return SlideDown();
                case Direction.Left: return SlideLeft();
                default: return false;
            }
        }

        public bool SlideRight()
        {
            bool bMoved = false;
            for (int y = 0; y < Height; ++y) {
                int ofs = y * Width;
                int xbase = Width - 1;
                for (int x0 = Width - 2; x0 >= 0; --x0) {
                    byte val = board[ofs + x0];
                    if (val == 0) continue;
                    for (int x = x0 + 1; x <= xbase; ++x) {
                        if (board[ofs + x] == 0) {
                            bMoved = true;
                            board[ofs + x] = val;
                            board[ofs + x - 1] = 0;
                            continue;
                        }
                        if (board[ofs + x] == val) {
                            bMoved = true;
                            ++board[ofs + x];
                            score += (1 << board[ofs + x]);
                            xbase = x - 1;
                            board[ofs + x - 1] = 0;
                        }
                        break;
                    }
                }
            }

            return bMoved;
        }

        public bool SlideLeft()
        {
            bool bMoved = false;
            for (int y = 0; y < Height; ++y) {
                int ofs = y * Width;
                int xbase = 0;
                for (int x0 = 1; x0 < Width; ++x0) {
                    byte val = board[ofs + x0];
                    if (val == 0) continue;
                    for (int x = x0 - 1; x >= xbase; --x) {
                        if (board[ofs + x] == 0) {
                            bMoved = true;
                            board[ofs + x] = val;
                            board[ofs + x + 1] = 0;
                            continue;
                        }
                        if (board[ofs + x] == val) {
                            bMoved = true;
                            ++board[ofs + x];
                            score += (1 << board[ofs + x]);
                            xbase = x + 1;
                            board[ofs + x + 1] = 0;
                        }
                        break;
                    }
                }
            }

            return bMoved;
        }

        public bool SlideDown()
        {
            bool bMoved = false;
            for (int x = 0; x < Width; ++x) {
                int ybase = Height - 1;
                for (int y0 = Height - 2; y0 >= 0; --y0) {
                    byte val = board[y0 * Width + x];
                    if (val == 0) continue;
                    for (int y = y0 + 1; y <= ybase; ++y) {
                        int ofs = y * Width + x;
                        if (board[ofs] == 0) {
                            bMoved = true;
                            board[ofs] = val;
                            board[ofs - Width] = 0;
                            continue;
                        }
                        if (board[ofs] == val) {
                            bMoved = true;
                            ++board[ofs];
                            score += (1 << board[ofs]);
                            ybase = y - 1;
                            board[ofs - Width] = 0;
                        }
                        break;
                    }
                }
            }

            return bMoved;
        }

        public bool SlideUp()
        {
            bool bMoved = false;
            for (int x = 0; x < Width; ++x) {
                int ybase = 0;
                for (int y0 = 1; y0 < Height; ++y0) {
                    byte val = board[y0 * Width + x];
                    if (val == 0) continue;
                    for (int y = y0 - 1; y >= ybase; --y) {
                        int ofs = y * Width + x;
                        if (board[ofs] == 0) {
                            bMoved = true;
                            board[ofs] = val;
                            board[ofs + Width] = 0;
                            continue;
                        }
                        if (board[ofs] == val) {
                            bMoved = true;
                            ++board[ofs];
                            score += (1 << board[ofs]);
                            ybase = y + 1;
                            board[ofs + Width] = 0;
                        }
                        break;
                    }
                }
            }

            return bMoved;
        }

        public bool AddRandomTile()
        {
            List<Coord> tiles = GetAvailableTiles();
            if (tiles.Count == 0) return false;

            int r = rng.Next(tiles.Count);
            Coord tile = tiles[r];
            byte value = (byte)(rng.NextDouble() < 0.9 ? 1 : 2);
            board[tile.y * Width + tile.x] = value;
            return true;
        }

        public Board Dup()
        {
            Board dup = new Board(Width, Height);
            dup.CopyFrom(this);
            return dup;
        }

        public Board CopyFrom(Board other)
        {
            Width = other.Width;
            Height = other.Height;
            NumTiles = other.NumTiles;
            Array.Copy(other.board, board, NumTiles);
            score = other.score;
            return this;
        }

        public void Print()
        {
            int ofs = 0;
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    if (board[ofs] > 0)
                        Console.Write(" " + (1 << board[ofs]).ToString().PadLeft(4));
                    else
                        Console.Write("    .");
                    ++ofs;
                }
                Console.WriteLine();
            }
        }

        public override bool Equals(Object obj)
        {
            return Equals(obj as Board);
        }

        public override int GetHashCode()
        {
            int hash = 11;
            hash = hash * 17 + Width;
            hash = hash * 19 + Height;
            for (int i = 0; i < NumTiles; ++i) {
                hash = unchecked(hash * 17 + board[i]);
                hash = hash * 3 + 11;
            }
            return hash;
        }

        public bool Equals(Board b)
        {
            if (b == null || b.Width != Width || b.Height != Height) return false;
            for (int i = 0; i < NumTiles; ++i)
                if (board[i] != b.board[i]) return false;
            return true;
        }

        public int MaxTile
        {
            get
            {
                int val = 0;
                for (int i = 0; i < NumTiles; ++i)
                    if (board[i] > val) val = board[i];
                return val;
            }
        }

        public int SumTiles
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < NumTiles; ++i)
                    sum += board[i];
                return sum;
            }
        }

        public int NumMergeablePairs
        {
            get
            {
                int n = 0;
                for (int y = 0; y < Height; ++y) {
                    int ofs = y * Width;
                    for (int x = 1; x < Width; ++x)
                        if (board[ofs + x - 1] == board[ofs + x]) ++n;
                }
                for (int y = 1; y < Height; ++y) {
                    int ofs = y * Width;
                    for (int x = 0; x < Width; ++x)
                        if (board[ofs + x - Width] == board[ofs + x]) ++n;
                }
                return n;
            }
        }

        public Board GetRotated()
        {
            Board b = Dup();
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x)
                    b.board[y * Width + x] = board[(Width - x - 1) * Width + y];
            }
            return b;
        }

        public Board GetHorizontalReflection()
        {
            Board b = Dup();
            for (int y = 0; y < Height; ++y) {
                int ofs = y * Width;
                for (int x = 0; x < Width; ++x)
                    b.board[ofs + x] = board[ofs + Width - x - 1];
            }
            return b;
        }

        public Board GetVerticalReflection()
        {
            Board b = Dup();
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    b.board[y * Width + x] = board[(Height - y - 1) * Width + x];
            return b;
        }

        public double GetCanonicalScore()
        {
            double score = 0;
            for (int y = 0; y < Height; ++y) {
                int ofs = y * Width;
                for (int x = 0; x < Width; ++x)
                    score += (1 << board[ofs + x]) * (x + y * 1.1 + 1);
            }
            return score;
        }

        public Board GetCanonical()
        {
            Board best = null;
            double bestScore = 0.0;

            Board b = Dup();
            for (int i = 0; i < 4; i++) {
                double score = b.GetCanonicalScore();
                if (score > bestScore) {
                    best = b;
                    bestScore = score;
                }
                Board c = b.GetHorizontalReflection();
                score = c.GetCanonicalScore();
                if (score > bestScore) {
                    best = c;
                    bestScore = score;
                }
                Board d = b.GetVerticalReflection();
                score = d.GetCanonicalScore();
                if (score > bestScore) {
                    best = d;
                    bestScore = score;
                }
                b = b.GetRotated();
            }
            return best;
        }

        public int Score { get { return score; } }

        public int Width, Height;
        public int NumTiles;
        public byte[] board;
        protected int score;
    }
}
