using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Segy_Coord
{
    internal class PointD
    {
        public double X;
        public double Y;

        public PointD()
        {
            X = 0; Y = 0;
        }

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
