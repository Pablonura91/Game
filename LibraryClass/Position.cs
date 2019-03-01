using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Position
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Position(int posX, int posY, int width, int height)
        {
            PosX = posX;
            PosY = posY;
            Width = width;
            Height = height;
        }

        public Position() { }
    }
}
