using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game2048
{
    class Coord
    {
        public Coord() { x = -1; y = -1; }
        public Coord(int x, int y) { this.x = x; this.y = y; }
        public int x, y;
    }
}
