using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Game
{
    public class BallGraphics
    {
        public Ellipse ShapeBall { get; set; }
        public Color Color { get; set; }

        public BallGraphics(int width, int height, Color color)
        {
            ShapeBall = new Ellipse();
            ShapeBall.Width = width;
            ShapeBall.Height = height;
            Color = color;
            ShapeBall.Fill = new SolidColorBrush(Color);

        }
    }
}
