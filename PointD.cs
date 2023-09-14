using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Segy_Coord
{
    internal class PointD
    {
        private double _x;
        private double _y;

        public PointD()
        {
            _x = 0; _y = 0;
        }

        public PointD(double x, double y)
        {
            _x = x;
            _y = y;
        }

        public double X 
        { 
            get { return _x; } 
            set { _x = value; }
        }

        public double Y 
        { 
            get { return _y; }
            set { _y = value; }
        }


        public double getY() { return Y; }

        public static PointD SubstractP2FromP1(PointD point1, PointD point2)
        {
            PointD result = new PointD(point1._x, point1._y);
            result._x -= point2._x;
            result._y -= point2._y;
            return result;
        }

        public void ShiftCoordinates(double dx, double dy)
        {
            _x += dx;
            _y += dy;
        }

        public override string ToString()
        {
            return this._x + "\t" + this._y;
        }

    }
}
