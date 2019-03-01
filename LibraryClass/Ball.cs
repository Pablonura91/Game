using Game;
using System;
using System.Windows.Media;

namespace Game
{
    public class Ball
    {
        //public int PosX { get; set; }
        //public int PosY { get; set; }
        //public int Width { get; set; }
        //public int Height { get; set; }
        public Color color { get; set; }
        //public BallGraphics BallDraw;

        public Ball() { }

        public Ball(Color color)
        {
            //PosX = posX;
            //PosY = posY;
            //Width = width;
            //Height = height;
            this.color = color;

            //BallDraw = new BallGraphics(width, height, color);
        }

        //public Ball(int posX, int posY, int width, int height)
        //{
        //    PosX = posX;
        //    PosY = posY;
        //    Width = width;
        //    Height = height;            
        //}
    }
}
