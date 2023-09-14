using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace Segy_Coord
{
    internal class VectorPointTransform
    {
        //Расчет разницы двух векторов point1 - point2
        public static Vector SubtractV2FromV1(Vector vector1, Vector vector2)
        {
            return new Vector(vector2.VectorCoordinates, vector1.VectorCoordinates);
        }

        //Расчет следующей начальной точки следующего сегмента
        //Рассчитывается таким образом, чтобы точка находилась на линии сегмента и
        //расстояние между ней и конечной точкой предыдущего сегмента было равно заданному расстоянию между трассами
        private static PointD CalculateNextStartPoint(PointD pointPrev, Vector vector, double distanceOfTrace)
        {
            double shiftFromStart = CalculateShiftFromStart(pointPrev, vector, distanceOfTrace);

            (double dx, double dy) dXdYOfSegment = vector.DXDYOfVector();

            double dXOfTrace = shiftFromStart * dXdYOfSegment.dx / vector.Length;
            double dYOfTrace = shiftFromStart * dXdYOfSegment.dy / vector.Length;
            pointPrev.ShiftCoordinates(dXOfTrace, dYOfTrace);
            return pointPrev;
        }

        //Расчет расстояния между начальной точкой сегмента и стартом сегмента
        public static double CalculateShiftFromStart(PointD pointPrev, Vector vector, double distanceOfTrace)
        {
            Vector vectorFromPrev = new Vector(pointPrev, vector.StartPoint);
            double sinAngle = CalCulateSinAngle(vectorFromPrev, vector);

            double deltaPerpPrev = vectorFromPrev.Length * sinAngle;

            double deltaPerpStart = Math.Sqrt(vectorFromPrev.Length * vectorFromPrev.Length - deltaPerpPrev * deltaPerpPrev);
            double deltaPerpNext = Math.Sqrt(distanceOfTrace * distanceOfTrace - deltaPerpPrev * deltaPerpPrev);
            double deltaStartNext = deltaPerpNext;

            if (deltaPerpStart < deltaPerpNext)
                deltaStartNext -= deltaPerpStart;

            return deltaStartNext;
        }

        //Расчет скалярного произведения двух векторов
        public static double CalCulateScalarMultiply(Vector vector1, Vector vector2)
        {
            return (vector1.VectorCoordinates.X * vector2.VectorCoordinates.X + vector1.VectorCoordinates.Y * vector2.VectorCoordinates.Y);
        }

        //Расчет косинуса угла между двумя векторами
        private static double CalCulateCosA(Vector vector1, Vector vector2)
        {
            double multiplyOfModuls = vector1.Length * vector2.Length;
            return CalCulateScalarMultiply(vector1, vector2) / multiplyOfModuls;
        }

        //Расчет синуса угла между двумя векторами
        public static double CalCulateSinAngle(Vector vector1, Vector vector2)
        {
            double cosA = CalCulateCosA(vector1, vector2);
            return (double)Math.Sqrt(1 - cosA * cosA);
        }

        //Расчет длины всего профиля, состоящего из сегментов
        public static double LengthOfProfile(List<Vector> vectors)
        {
            double length = 0;
            for (int i = 0; i < vectors.Count; i++)
            {
                length += vectors[i].Length;
            }
            return length;
        }


        //Запись координат в отдельный файл
        public static void WriteCoordsToFile(PointD[] points, string FileName)
        {
            string path = Path.GetDirectoryName(FileName);
            string filename = Path.GetFileNameWithoutExtension(FileName);
            string newFileName = path + "\\" + filename + "_coords.txt";
            using (StreamWriter streamWriter = new StreamWriter(newFileName))
            {
                streamWriter.WriteLine("X\tY");
                foreach (PointD point in points)
                {
                    if (point != null)
                        streamWriter.WriteLine(point.ToString());
                }
            }
        }

        /// <summary>
        /// Считывает в отдельную коллекцию координаты трасс  
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="ShiftLocation"></param>
        /// <returns></returns>
        //    public PointD[] TransformCornerPointsToLinePoints(int countOfTraces, string lineName, Dictionary<string, List<PointD>> cornerPointsAllProfiles)
        //    {
        //        PointD[] coordinates = new PointD[countOfTraces];

        //        if (!cornerPointsAllProfiles.Keys.Contains(lineName))
        //        {
        //            Logging.SendNoCornerPoints();
        //            return null;
        //        }

        //List<PointD> cornerPoints = cornerPointsAllProfiles[lineName];


        public PointD[] TransformCornerPointsToLinePoints(int countOfTraces, string lineName, List<Vector> vectorsCornerPoints, double initialDistanceOfTrace)
        {
            PointD[] coordinates = new PointD[countOfTraces];

            if (vectorsCornerPoints == null || vectorsCornerPoints.Count == 0)
            {
                Logging.SendNoCornerPoints();
                return null;
            }

            Logging.SendCountOfCornerPoints(vectorsCornerPoints.Count + 1);

            //Записываем в таблицу координат первую и последнюю угловую точку 
            coordinates[0] = new PointD(vectorsCornerPoints[0].StartPoint.X, vectorsCornerPoints[0].StartPoint.Y);
            coordinates[coordinates.Length - 1] = new PointD(vectorsCornerPoints[vectorsCornerPoints.Count - 1].EndPoint.X, vectorsCornerPoints[vectorsCornerPoints.Count - 1].EndPoint.Y);


            double lengthOfProfile = LengthOfProfile(vectorsCornerPoints); //длина всего профиля
            Logging.SendLengthOfProfile(lengthOfProfile);

            double distanceOfTrace = CalculateDistanceOfTraces(initialDistanceOfTrace, lengthOfProfile, countOfTraces); //расстояние между трассами
            Logging.SendDistanceOfTrace(distanceOfTrace);

            PointD startPointOfSegment = new PointD(vectorsCornerPoints[0].StartPoint.X, vectorsCornerPoints[0].StartPoint.Y); //начальная точка в сегменте

            int numberOfTrace = 0;

            if (vectorsCornerPoints.Count == 1)
            {
                //Количество трасс на профиле
                int numTracesForProfile = CalcaluteNumberOfTracesForSegment(null, vectorsCornerPoints[0], distanceOfTrace, 0, countOfTraces);                

                //приращение по х и у для каждой трассы в сегменте
                (double dX, double dY) dXdYOfTrace = CalculatedXdYForTrace(vectorsCornerPoints[0], distanceOfTrace);

                //расчет координат трасс в сегменте и добавление их в общую коллекцию
                FillTableOfCoordinates(ref startPointOfSegment, ref coordinates, numTracesForProfile, ref numberOfTrace, dXdYOfTrace);

            }
            else
            {
                for (int i = 0; i < vectorsCornerPoints.Count; i++)
                {
                    //расчет начальной точки каждого сегмента так, чтобы расстояние между трассами сохранялось
                    if (i > 0)
                    {
                        numberOfTrace++;
                        startPointOfSegment = CalculateNextStartPoint(startPointOfSegment, vectorsCornerPoints[i], distanceOfTrace);
                        coordinates[numberOfTrace] = new PointD(startPointOfSegment.X, startPointOfSegment.Y);
                    }

                    //количество трасс в сегменте                    
                    int numTracesForSegment = CalcaluteNumberOfTracesForSegment(startPointOfSegment, vectorsCornerPoints[i], distanceOfTrace, numberOfTrace, countOfTraces);

                    //приращение по х и у для каждой трассы в сегменте
                    (double dX, double dY) dXdYOfTrace = CalculatedXdYForTrace(vectorsCornerPoints[i], distanceOfTrace);

                    //расчет координат трасс в сегменте и добавление их в общую коллекцию
                    FillTableOfCoordinates(ref startPointOfSegment, ref coordinates, numTracesForSegment, ref numberOfTrace, dXdYOfTrace);

                }
            }
            coordinates = CheckForNullAndDistance(coordinates, distanceOfTrace);
            return coordinates;
        }


        //Проверяет остались ли незаполненные ячейки координат.
        //Если есть, то считает сколько их и в зависимости от их количества добавляет точки сначала и с конца пополам.
        //Далее проверяет расстояние между последней и предпоследней точками.
        //Если расстояние не совпадает с расстоянием в целом по сегменту, то меняет значение последней точки на новое (предпоследняя точка + сдвиг по сегменту)
        private static PointD[] CheckForNullAndDistance(PointD[] points, double distanceOfTraces)
        {
            int countOfNull = points.Where(p => p == null).Count();
            int countOfRestNull = countOfNull;

            //Заполнение null
            if (countOfNull > 0)
            {
                Logging.SendCountOfNullValues(countOfNull);

                int countNullOfEnd = (int)countOfNull / 2;
                int countNullOfStart = countOfNull - countNullOfEnd;

                Logging.SendCountOfAddedEndPointsForNull(countNullOfEnd);
                Logging.SendCountOfAddedStartPointsForNull(countNullOfStart);

                int indexOfLastNotNull = points.Length - 1 - countOfNull - 1;
                (double dx, double dy) dXdYOfSegmentEnd = DXDYOfSegment(points[indexOfLastNotNull - 1], points[indexOfLastNotNull]);
                while (countNullOfEnd > 0)
                {
                    int indexOfFirstNull = points.Length - 1 - countOfRestNull;
                    points[indexOfFirstNull] = new PointD(points[indexOfFirstNull - 1].X + dXdYOfSegmentEnd.dx, points[indexOfFirstNull - 1].Y + dXdYOfSegmentEnd.dy);
                    countOfRestNull--;
                    countNullOfEnd--;
                }
                (double dx, double dy) dXdYOfSegmentStart = DXDYOfSegment(points[1], points[0]);
                while (countNullOfStart > 0)
                {
                    PointD point = new PointD(points[0].X + dXdYOfSegmentStart.dx, points[0].Y + dXdYOfSegmentStart.dy);
                    List<PointD> pointsList = points.ToList();
                    pointsList.RemoveAt(points.Length - 1 - countOfRestNull);
                    points = pointsList.Prepend(point).ToArray();
                    countOfRestNull--;
                    countNullOfStart--;
                }
            }
            else
            {
                Logging.SendNoNullValues();
            }

            //Проверка последней ячейки
            (double dx, double dy) dXdYOfSegment = DXDYOfSegment(points[points.Length - 3], points[points.Length - 2]);
            (double dx, double dy) dXdYOfLastSegmentEnd = DXDYOfSegment(points[points.Length - 2], points[points.Length - 1]);
            double lengthOfLastSegmentEnd = LengthOfSegment(points[points.Length - 2], points[points.Length - 1]);

            double diffDXOfSegmentAllAndEnd = dXdYOfSegment.dx - dXdYOfLastSegmentEnd.dx;
            double diffDYOfSegmentAllAndEnd = dXdYOfSegment.dy - dXdYOfLastSegmentEnd.dy;

            int numberOfTracesInTheEndSegment = (int)(lengthOfLastSegmentEnd / distanceOfTraces);

            Logging.SendCountOfAddedEndTraces(numberOfTracesInTheEndSegment);

            if (0 <= numberOfTracesInTheEndSegment && numberOfTracesInTheEndSegment < 3)
            {
                points[points.Length - 1] = new PointD(points[points.Length - 2].X + dXdYOfSegment.dx, points[points.Length - 2].Y + dXdYOfSegment.dy);

                Logging.SendLastPointChanged();
            }
            else if (numberOfTracesInTheEndSegment >= 3)
            {
                int shiftOnStart = (int)(numberOfTracesInTheEndSegment / 2);
                List<PointD> pointsList = points.ToList();
                List<PointD> pointsList2 = pointsList.Skip(shiftOnStart).ToList();
                List<PointD> pointsList3 = pointsList2.Take(pointsList2.Count - 1).ToList();
                for (int k = 0; k < shiftOnStart + 1; k++)
                {
                    PointD lastPoint = new PointD(pointsList3[pointsList3.Count - 1].X + dXdYOfSegment.dx, pointsList3[pointsList3.Count - 1].Y + dXdYOfSegment.dy);
                    pointsList3.Add(lastPoint);
                }
                points = pointsList3.ToArray();

                Logging.SendCountOfAddedEndPointsForDistance(shiftOnStart);
                Logging.SendCountOfAddedStartPointsForDistance(shiftOnStart);
            }
            return points;
        }

        //Переделывает список точек в список векторов (сегментов)
        public static List<Vector> ConvertPointsToVectors(List<PointD> cornerPoints)
        {
            List<Vector> vectorsOfCornerPoints = new List<Vector>();

            for (int i = 0; i < cornerPoints.Count - 1; i++)
            {
                Vector vector = new Vector(cornerPoints[i], cornerPoints[i + 1]);
                vectorsOfCornerPoints.Add(vector);
            }

            return vectorsOfCornerPoints;
        }

        //Запись в таблиу координат нужного количества точек с заданным шагом по координатам, начиная с нужного номера
        private static void FillTableOfCoordinates(ref PointD startPointOfSegment, ref PointD[] coordinates, int countOfAddPoints, ref int numberOfTrace, (double dX, double dY) dXdYOfTrace)
        {
            for (int k = 1; k <= countOfAddPoints; k++)
            {
                numberOfTrace++;
                startPointOfSegment.ShiftCoordinates(dXdYOfTrace.dX, dXdYOfTrace.dY);
                coordinates[numberOfTrace] = new PointD(startPointOfSegment.X, startPointOfSegment.Y);
            }
        }

        //Расчет расстояния между трассами - делится вся длина профиля на количество трасс.
        //Если задано ненулевое значение initialDistanceOfTrace, то расстояние равно initialDistanceOfTrace (то, что задается в форме)
        private static double CalculateDistanceOfTraces(double initialDistanceOfTrace, double lengthOfProfile, int countOfTraces)
        {
            double distanceOfTrace;
            if (initialDistanceOfTrace > 0.0d)
            {
                distanceOfTrace = initialDistanceOfTrace;
            }
            else
            {
                distanceOfTrace = lengthOfProfile / (countOfTraces - 1); //расстояние между трассами
            }
            return distanceOfTrace;
        }

        private static (double dX, double dY) CalculatedXdYForTrace(Vector vector, double distanceOfTrace)
        {
            (double dx, double dy) dXdYOfSegment = vector.DXDYOfVector();
            double lengthOfSegment = vector.Length;
            double dXOfTrace = distanceOfTrace * dXdYOfSegment.dx / lengthOfSegment;
            double dYOfTrace = distanceOfTrace * dXdYOfSegment.dy / lengthOfSegment;
            return (dXOfTrace, dYOfTrace);
        }

        private static int CalcaluteNumberOfTracesForSegment(PointD startPointOfSegment, Vector vectorCornerPoint, double distanceOfTrace, int numberOfTrace, int countOfTraces)
        {
            Vector vectorFromStartPointToEnd;
            if (startPointOfSegment != null)
            {
                vectorFromStartPointToEnd = new Vector(startPointOfSegment, vectorCornerPoint.EndPoint);
            }
            else
            {
                vectorFromStartPointToEnd = vectorCornerPoint;
            }

            double lengthOfSegmentFromStartPoint = vectorFromStartPointToEnd.Length;
            int numTracesForSegment = (int)(lengthOfSegmentFromStartPoint / distanceOfTrace);

            if ((numberOfTrace + numTracesForSegment) >= countOfTraces) numTracesForSegment = countOfTraces - numberOfTrace - 2;

            if (numTracesForSegment == (countOfTraces - 1))
            {
                numTracesForSegment--;
            }

            return numTracesForSegment;
        }
       
    }
}
