using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Segy_Coord
{
    internal class Vector
    {
        private PointD _pointStart; //начальная точка
        private PointD _pointEnd; //конечная точка
        private PointD _vectorCoordinates; //координаты вектора       

        public Vector()
        {
            _pointStart = new PointD();
            _pointEnd = new PointD();
            _vectorCoordinates = new PointD();
        }

        public Vector(PointD pointStart, PointD pointEnd)
        {
            _pointStart = new PointD(pointStart.X, pointStart.Y);
            _pointEnd = new PointD(pointEnd.X, pointEnd.Y);
            _vectorCoordinates = PointD.SubstractP2FromP1(pointEnd, pointStart);
        }

        public Vector(PointD pointEnd)
        {
            _pointStart = new PointD();
            _pointEnd = new PointD(pointEnd.X, pointEnd.Y);
            _vectorCoordinates = new PointD(pointEnd.X, pointEnd.Y);
        }

        public Vector(double x, double y)
        {
            _pointStart = new PointD();
            _pointEnd = new PointD(x, y);
            _vectorCoordinates = new PointD(x, y);
        }

        //проверяет из начала идет вектор или нет
        public static bool IsFromStartOfCoordinates(Vector vector)
        {
            if (vector._pointStart.X == 0 && vector._pointStart.X == 0)
                return true;
            else
                return false;
        }        

        //Расчет длины вектора по координатам точек начала и конца
        public double Length
        {
            get
            {
                return Math.Sqrt(VectorPointTransform.CalCulateScalarMultiply(this, this));
            }
        }

        public PointD StartPoint
        {
            get
            {
                return _pointStart;
            }
        }
        public PointD EndPoint
        {
            get
            {
                return _pointEnd;
            }
        }

        public PointD VectorCoordinates
        {
            get
            {
                return _vectorCoordinates;
            }
        }                

        //Расчет смещений по х и у вдоль вектора
        public (double dx, double dy) DXDYOfVector()
        {
            double dx = _pointEnd.X - _pointStart.X;
            double dy = _pointEnd.Y - _pointStart.Y;
            return (dx, dy);
        }  

    }
}
