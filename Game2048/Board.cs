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

        public Board(int w, int h)
        {
            if (rng == null) rng = new Random();
            board = new int[h][];
            for (int i = 0; i < h; i++) board[i] = new int[w];
            score = 0;
        }

        public Board(int w, int h, int[] data)
            : this(w, h)
        {
            int ix = 0;
            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                    board[y][x] = data[ix++];
        }

        public bool HasOpenTiles()
        {
            return (NumAvailableTiles > 0);
        }

        public int NumAvailableTiles
        {
            get { return GetAvailableTiles().Count; }
        }

        public List<Coord> GetAvailableTiles()
        {
            List<Coord> tiles = new List<Coord>();
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    if (board[y][x] == 0)
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
                int[] row = board[y];
                int xbase = Width - 1;
                for (int x0 = Width - 2; x0 >= 0; --x0) {
                    int val = row[x0];
                    if (val == 0) continue;
                    for (int x = x0 + 1; x <= xbase; ++x) {
                        if (row[x] == 0) {
                            bMoved = true;
                            row[x] = val;
                            row[x - 1] = 0;
                            continue;
                        }
                        if (row[x] == val) {
                            bMoved = true;
                            ++row[x];
                            score += (1 << row[x]);
                            xbase = x - 1;
                            row[x - 1] = 0;
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
                int[] row = board[y];
                int xbase = 0;
                for (int x0 = 1; x0 < Width; ++x0) {
                    int val = row[x0];
                    if (val == 0) continue;
                    for (int x = x0 - 1; x >= xbase; --x) {
                        if (row[x] == 0) {
                            bMoved = true;
                            row[x] = val;
                            row[x + 1] = 0;
                            continue;
                        }
                        if (row[x] == val) {
                            bMoved = true;
                            ++row[x];
                            score += (1 << row[x]);
                            xbase = x + 1;
                            row[x + 1] = 0;
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
                    int val = board[y0][x];
                    if (val == 0) continue;
                    for (int y = y0 + 1; y <= ybase; ++y) {
                        if (board[y][x] == 0) {
                            bMoved = true;
                            board[y][x] = val;
                            board[y - 1][x] = 0;
                            continue;
                        }
                        if (board[y][x] == val) {
                            bMoved = true;
                            ++board[y][x];
                            score += (1 << board[y][x]);
                            ybase = y - 1;
                            board[y - 1][x] = 0;
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
                    int val = board[y0][x];
                    if (val == 0) continue;
                    for (int y = y0 - 1; y >= ybase; --y) {
                        if (board[y][x] == 0) {
                            bMoved = true;
                            board[y][x] = val;
                            board[y + 1][x] = 0;
                            continue;
                        }
                        if (board[y][x] == val) {
                            bMoved = true;
                            ++board[y][x];
                            score += (1 << board[y][x]);
                            ybase = y + 1;
                            board[y + 1][x] = 0;
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
            int value = (rng.NextDouble() < 0.9 ? 1 : 2);
            board[tile.y][tile.x] = value;
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
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    board[y][x] = other.board[y][x];
            score = other.score;
            return this;
        }

        public int Width { get { return (board != null && board.Length > 0 ? board[0].Length : 0); } }
        public int Height { get { return (board == null ? 0 : board.Length); } }
        public int NumTiles { get { return Width * Height; } }

        public void Print()
        {
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x)
                    if (board[y][x] > 0)
                        Console.Write(" " + (1 << board[y][x]).ToString().PadLeft(4));
                    else
                        Console.Write("    .");
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
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x)
                    hash = unchecked(hash * 17 + board[y][x]);
                hash = hash * 3 + 11;
            }
            return hash;
        }

        public bool Equals(Board b)
        {
            if (b == null || b.Width != Width || b.Height != Height) return false;
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    if (board[y][x] != b.board[y][x]) return false;
            return true;
        }

        public int MaxTile
        {
            get
            {
                int val = 0;
                for (int y = 0; y < Height; ++y)
                    for (int x = 0; x < Width; ++x)
                        if (board[y][x] > val) val = board[y][x];
                return val;
            }
        }

        public int SumTiles
        {
            get
            {
                int sum = 0;
                for (int y = 0; y < Height; ++y)
                    for (int x = 0; x < Width; ++x)
                        sum += board[y][x];
                return sum;
            }
        }

        public int NumMergeablePairs
        {
            get
            {
                int n = 0;
                for (int y = 0; y < Height; ++y) {
                    int[] row = board[y];
                    for (int x = 1; x < Width; ++x)
                        if (row[x - 1] == row[x]) ++n;
                }
                for (int x = 0; x < Width; ++x)
                    for (int y = 1; y < Height; ++y)
                        if (board[y - 1][x] == board[y][x]) ++n;
                return n;
            }
        }

        public Board GetRotated()
        {
            Board b = Dup();
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    b.board[y][x] = board[Width - x - 1][y];
            return b;
        }

        public Board GetHorizontalReflection()
        {
            Board b = Dup();
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    b.board[y][x] = board[y][Width - x - 1];
            return b;
        }

        public Board GetVerticalReflection()
        {
            Board b = Dup();
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    b.board[y][x] = board[Height - y - 1][x];
            return b;
        }

        public double GetCanonicalScore()
        {
            double score = 0;
            for (int y = 0; y < Height; ++y) {
                int[] row = board[y];
                for (int x = 0; x < Width; ++x)
                    score += (1 << row[x]) * (x + y * 1.1 + 1);
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

        public int[][] board;
        protected int score;
        protected static Random rng;
    }
}
