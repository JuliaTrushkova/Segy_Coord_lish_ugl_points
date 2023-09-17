using System;
using System.Collections.Generic;
using System.Drawing;
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

        private static void CalculateStartPoint(ref int numberOfTrace, ref PointD startPointOfSegment, double distanceOfTrace, Vector vectorNextSegment, ref PointD[] coordinates)
        {
            numberOfTrace++;
            startPointOfSegment = CalculateNextStartPoint(startPointOfSegment, vectorNextSegment, distanceOfTrace);
            coordinates[numberOfTrace] = new PointD(startPointOfSegment.X, startPointOfSegment.Y);
        }

        //Расчет следующей начальной точки следующего сегмента
        //Рассчитывается таким образом, чтобы точка находилась на линии сегмента и
        //расстояние между ней и конечной точкой предыдущего сегмента было равно заданному расстоянию между трассами
        private static PointD CalculateNextStartPoint(PointD pointPrev, Vector vectorNext, double distanceOfTrace)
        {
            if (vectorNext is null)
            {
                throw new ArgumentNullException(nameof(vectorNext));
            }

            double shiftFromStart = CalculateShiftFromStart(pointPrev, vectorNext, distanceOfTrace);

            (double dx, double dy) dXdYOfSegment = vectorNext.DXDYOfVector();

            double dXOfTrace = shiftFromStart * dXdYOfSegment.dx / vectorNext.Length;
            double dYOfTrace = shiftFromStart * dXdYOfSegment.dy / vectorNext.Length;
            PointD pointNext = new PointD(vectorNext.StartPoint.X + dXOfTrace, vectorNext.StartPoint.Y + dYOfTrace);
            return pointNext;
        }

        //Расчет расстояния между начальной точкой сегмента и стартом сегмента
        public static double CalculateShiftFromStart(PointD pointPrev, Vector vectorNext, double distanceOfTrace)
        {
            Vector vectorFromPrev = new Vector(vectorNext.StartPoint, pointPrev);
            decimal cosA = CalCulateCosA(vectorFromPrev, vectorNext);
            double sinAngle = CalCulateSinAngle(cosA);

            double deltaPerpPrev = vectorFromPrev.Length * sinAngle;

            double deltaPerpStart = Math.Sqrt(vectorFromPrev.Length * vectorFromPrev.Length - deltaPerpPrev * deltaPerpPrev);
            double deltaPerpNext = Math.Sqrt(distanceOfTrace * distanceOfTrace - deltaPerpPrev * deltaPerpPrev);
            
            double deltaStartNext;

            if (cosA <= 0) 
            { 
                deltaStartNext = deltaPerpNext - deltaPerpStart; 
            }
            else
            {
                if (deltaPerpStart <= deltaPerpNext)
                    deltaStartNext = deltaPerpNext + deltaPerpStart;
                else
                    deltaStartNext = deltaPerpStart - deltaPerpNext;
            }
            

            return deltaStartNext;
        }

        //Расчет скалярного произведения двух векторов
        public static double CalCulateScalarMultiply(Vector vector1, Vector vector2)
        {
            return (vector1.VectorCoordinates.X * vector2.VectorCoordinates.X + vector1.VectorCoordinates.Y * vector2.VectorCoordinates.Y);
        }

        //Расчет косинуса угла между двумя векторами
        private static decimal CalCulateCosA(Vector vector1, Vector vector2)
        {
            decimal multiplyOfModuls = Convert.ToDecimal(vector1.Length) * Convert.ToDecimal(vector2.Length);
            decimal scalarMultiply = Convert.ToDecimal(CalCulateScalarMultiply(vector1, vector2));
            decimal cosA = Convert.ToDecimal(scalarMultiply) / Convert.ToDecimal(multiplyOfModuls);

            if (cosA < -1) cosA = -1;
            if (cosA > 1) cosA = 1;
            
            return cosA;
        }

        //Расчет синуса угла между двумя векторами
        public static double CalCulateSinAngle(decimal cosA)
        {            
            double sinA = (double)Math.Sqrt((double)(1 - cosA * cosA));

            if (sinA < -1) sinA = -1;
            if (sinA > 1) sinA = 1;

            return sinA;
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

        //Запись координат в отдельный файл (формат Х Y)
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
        /// Преобразует угловые точки в координаты для каждой трассы (разделение каждого сегмента угловых точек на заданное расстояние между трассами)
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="ShiftLocation"></param>
        /// <returns></returns>       
        public static PointD[] TransformCornerPointsToLinePoints(int countOfTraces, List<Vector> vectorsCornerPoints, double initialDistanceOfTrace)
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

            //длина всего профиля
            double lengthOfProfile = LengthOfProfile(vectorsCornerPoints);
            Logging.SendLengthOfProfile(lengthOfProfile);

            //расстояние между трассами (рассчитывается исходя из длины всего профиля, количества трасс и заданного или нет пользователем расстояния между трассами)
            double distanceOfTrace = CalculateDistanceOfTraces(initialDistanceOfTrace, lengthOfProfile, countOfTraces); 
            Logging.SendDistanceOfTrace(distanceOfTrace);

            //начальная точка в сегменте (сначала это первая угловая точка)
            PointD startPointOfSegment = new PointD(vectorsCornerPoints[0].StartPoint.X, vectorsCornerPoints[0].StartPoint.Y); 

            int numberOfTrace = 0;

            //Если количество угловых точек две, то значит это координаты начала и конца профиля. 
            if (vectorsCornerPoints.Count == 1)
            {
                //Количество трасс на профиле
                int numTracesForProfile = CalcaluteNumberOfTracesForSegment(null, vectorsCornerPoints[0], distanceOfTrace, 0, countOfTraces);

                //приращение по х и у для каждой трассы в сегменте
                (double dX, double dY) dXdYOfTrace = CalculatedXdYForTrace(vectorsCornerPoints[0], distanceOfTrace);

                //расчет координат трасс в сегменте и добавление их в общую коллекцию
                FillTableOfCoordinates(ref startPointOfSegment, ref coordinates, numTracesForProfile, ref numberOfTrace, dXdYOfTrace);

            }
            //Если больше двух точек, то уже надо отдельно рассчитывать для каждого сегмента координаты трасс с учетом сохранения расстояния между трассами и направления вектора сегмента
            else
            {
                for (int i = 0; i < vectorsCornerPoints.Count; i++)
                {
                    //расчет начальной точки каждого сегмента так, чтобы расстояние между трассами сохранялось
                    if (i > 0)
                    {
                        CalculateStartPoint(ref numberOfTrace, ref startPointOfSegment, distanceOfTrace, vectorsCornerPoints[i], ref coordinates);
                    }

                    //количество трасс в сегменте                    
                    int numTracesForSegment = CalcaluteNumberOfTracesForSegment(startPointOfSegment, vectorsCornerPoints[i], distanceOfTrace, numberOfTrace, countOfTraces);

                    //приращение по х и у для каждой трассы в сегменте
                    (double dX, double dY) dXdYOfTrace = CalculatedXdYForTrace(vectorsCornerPoints[i], distanceOfTrace);

                    //расчет координат трасс в сегменте и добавление их в общую коллекцию
                    FillTableOfCoordinates(ref startPointOfSegment, ref coordinates, numTracesForSegment, ref numberOfTrace, dXdYOfTrace);

                }
            }
            //Проверяет остались ли незаполненные ячейки координат
            //(в случае если задано такое расстояние между трассами, что длины профиля либо не хватает, либо много для полного заполнения таблицы координат для всех трасс)
            CheckForNullAndDistance(ref coordinates, distanceOfTrace);
            return coordinates;
        }


        //Проверяет остались ли незаполненные ячейки координат.
        //Если есть, то считает сколько их и в зависимости от их количества добавляет точки сначала и с конца пополам.
        //Далее проверяет расстояние между последней и предпоследней точками.
        //Если расстояние не совпадает с расстоянием в целом по сегменту, то меняет значение последней точки на новое (предпоследняя точка + сдвиг по сегменту)
        private static void CheckForNullAndDistance(ref PointD[] points, double distanceOfTraces)
        {
            //Поиск всех Null значений
            CheckNullValues(ref points);

            //Проверка последней ячейки
            CheckDistanceOfLastTrace(ref points, distanceOfTraces);
        }

        //Проверяет остались ли незаполненные ячейки координат.
        //Если есть, то считает сколько их и в зависимости от их количества добавляет точки сначала и с конца пополам.
        private static void CheckNullValues(ref PointD[] points)
        {
            //Поиск всех Null значений
            int countOfNull = points.Where(p => p == null).Count();
            //Далее количество Null значений будет уменьшаться за счет добавления дополнительных точек
            int countOfRestNull = countOfNull;

            //Заполнение null путем добавления точек с начала и конца профиля
            if (countOfNull > 0)
            {
                FillNullTraces(ref points, countOfNull, ref countOfRestNull);
            }
            else
            {
                Logging.SendNoNullValues();
            }
        }

        //Проверяет расстояние между последней и предпоследней точками.
        //Если расстояние не совпадает с расстоянием в целом по сегменту, то меняет значение последней точки на новое (предпоследняя точка + сдвиг по сегменту)
        private static void CheckDistanceOfLastTrace(ref PointD[] points, double distanceOfTraces)
        {
           //Считаем количество трасс, которые помещаются между последней и предпоследней трассой (делим расстояние между ними на заданное расстояние между трассами)            
            Vector vectorLast2Traces = new Vector(points[points.Length - 2], points[points.Length - 1]);            
            double lengthOfLastSegmentEnd = vectorLast2Traces.Length;
            int numberOfTracesInTheEndSegment = (int)(lengthOfLastSegmentEnd / distanceOfTraces);
            Logging.SendCountOfAddedEndTraces(numberOfTracesInTheEndSegment);

            //Расчет смещений вдоль последнего сегмента
            Vector vectorPenult2Traces = new Vector(points[points.Length - 3], points[points.Length - 2]);
            (double dx, double dy) dXdYOfPenultSegment = vectorPenult2Traces.DXDYOfVector();

            //Если помещается меньше 2 трасс, то просто меняем координату последней трассы на новую (рассчитанное смещение предпоследней)
            if (0 <= numberOfTracesInTheEndSegment && numberOfTracesInTheEndSegment < 3)
            {
                points[points.Length - 1] = new PointD(points[points.Length - 2].X + dXdYOfPenultSegment.dx, points[points.Length - 2].Y + dXdYOfPenultSegment.dy);

                Logging.SendLastPointChanged();
            }
            //Если помещается больше 2 трасс, то смещаем профиль на нужное количество отсчетов
            else if (numberOfTracesInTheEndSegment >= 3)
            {
                FillTracesToEndAndShiftStart(ref points, numberOfTracesInTheEndSegment, dXdYOfPenultSegment);
            }
        }

        //Смещение профиля на половину количества трасс, которые вмещаются между последней и предпоследней трассой
        private static void FillTracesToEndAndShiftStart(ref PointD[] points, int numberOfTracesInTheEndSegment, (double dx, double dy) dXdYOfSegment)
        {
            //Рассчитывает количество точек, на которое нужно сдвинуть начало профиля (половина всех вмещаемых трасс)
            int shiftOnStart = (int)(numberOfTracesInTheEndSegment / 2);
            //Сдвигает начало профиля и убирает последнюю трассу
            List<PointD> pointsList = points.Skip(shiftOnStart).Take(points.Length - 1 - shiftOnStart).ToList();
            
            //Заполняет конец профиля нужными координатами со смещением по Х и Y последнего сегмента
            for (int k = 0; k < shiftOnStart + 1; k++)
            {
                PointD lastPoint = new PointD(pointsList[pointsList.Count - 1].X + dXdYOfSegment.dx, pointsList[pointsList.Count - 1].Y + dXdYOfSegment.dy);
                pointsList.Add(lastPoint);
            }

            points = pointsList.ToArray();

            Logging.SendCountOfAddedEndPointsForDistance(shiftOnStart);
            Logging.SendCountOfAddedStartPointsForDistance(shiftOnStart);
        }

        //Заполнение рассчитанными координатами ячейки с Null значениями
        private static void FillNullTraces(ref PointD[] points, int countOfNull, ref int countOfRestNull)
        {
            Logging.SendCountOfNullValues(countOfNull);
            //в конец и начало профиля добавляется равное количество точек для убирания всех значений Null
            //если нечетное количество Null, то в начало добавляется больше на 1
            //если количество Null равно 1, то точка добавляется в начало
            int countNullOfEnd = (int)countOfNull / 2;
            int countNullOfStart = countOfNull - countNullOfEnd;
            Logging.SendCountOfAddedEndPointsForNull(countNullOfEnd);
            Logging.SendCountOfAddedStartPointsForNull(countNullOfStart);

            //Заполнение конца профиля точками в количестве половины колчиества Null вдоль направления последнего сегмента
            FillEndOfProfile(ref points, countOfNull, countNullOfEnd, ref countOfRestNull);

            //Заполнение начала профиля точками в количестве половины колчиества Null вдоль направления первого сегмента
            FillStartOfProfile(ref points, countNullOfStart, ref countOfRestNull);
        }

        //Заполнение конца профиля точками в количестве половины количества Null вдоль направления последнего сегмента
        private static void FillEndOfProfile(ref PointD[] points, int countOfNull, int countNullOfEnd, ref int countOfRestNull)
        {
            //Расчет индекса последнего ненулевого значения
            int indexOfLastNotNull = points.Length - 1 - countOfNull - 1;

            //Задание направления добавления точек в конец профиля путем расчета смещения по х и у по последней паре трасс с не Null значениями
            Vector vectorOfLastSegment = new Vector(points[indexOfLastNotNull - 1], points[indexOfLastNotNull]);
            (double dx, double dy) dXdYOfSegmentEnd = vectorOfLastSegment.DXDYOfVector();            

            //Заполнение точками в конец профиля
            while (countNullOfEnd > 0)
            {
                int indexOfFirstNull = points.Length - 1 - countOfRestNull;
                points[indexOfFirstNull] = new PointD(points[indexOfFirstNull - 1].X + dXdYOfSegmentEnd.dx, points[indexOfFirstNull - 1].Y + dXdYOfSegmentEnd.dy);
                countOfRestNull--; //уменьшение количества оставшихся Null
                countNullOfEnd--; //уменьшение количества оставшихся Null для конца профиля
            }
        }
        
        //Заполнение начала профиля точками в количестве половины количества Null вдоль направления первого сегмента
        private static void FillStartOfProfile(ref PointD[] points, int countNullOfStart, ref int countOfRestNull)
        {
            //Задание направления добавления точек в начало профиля путем расчета смещения по х и у по первой паре трасс
            Vector vectorOfFirstSegment = new Vector(points[1], points[0]);
            (double dx, double dy) dXdYOfSegmentStart = vectorOfFirstSegment.DXDYOfVector();

            //Переделывание массива в лист, чтобы можно было удобнее удалять/добавлять элементы
            List<PointD> pointsList = points.ToList();
            pointsList.RemoveRange(points.Length - 1 - countOfRestNull, countNullOfStart);

            //Расчет точек, которые нужно вставить в начало, с одинаковым смещением друг от друга как в первом сегменте
            List<PointD> pointsToAdd = new List<PointD>();
            int count = 1;
            while (countNullOfStart > 0)
            {
                PointD point = new PointD(points[0].X + dXdYOfSegmentStart.dx * count, points[0].Y + dXdYOfSegmentStart.dy * count);
                pointsToAdd.Add(point);
                count++;               
                countNullOfStart--;
            }
            //добавление новых точек в начало массива точек
            pointsList.InsertRange(0, pointsToAdd);
            points = pointsList.ToArray();
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
