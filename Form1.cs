using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Unplugged.Segy;
using Unplugged.IbmBits;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Reflection;

namespace Segy_Coord
{
    public partial class Form1 : Form
    {

        Dictionary<string, List<PointD>> cornerPointsAllProfiles;
        static StringBuilder logging = new StringBuilder();

        public Form1()
        {
            InitializeComponent();
            cornerPointsAllProfiles = new Dictionary<string, List<PointD>>();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //Создание класс-оператора для чтения segy файла
            SegyReader reader = new SegyReader();
            //указание номера байта для координат Х и Y и шифта
            reader.XLocation = Int32.Parse(textBox3.Text);
            reader.YLocation = Int32.Parse(textBox4.Text);

            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                

                string[] Filenames1 = openFileDialog1.FileNames;

                //создает отдельно путь до файлов
                string pathtofiles = "";
                string[] g = Filenames1[0].Split('\\');
                for (int l = 0; l < g.Length - 1; l++)
                {
                    pathtofiles = pathtofiles + g[l] + "\\";
                }

                StreamWriter streamForLogFile = File.CreateText(pathtofiles + "logfile.txt");

                Calculate(Filenames1, reader, streamForLogFile);
                //Thread t = new Thread(new ThreadStart(ThreadProc));

                //// Start ThreadProc.  Note that on a uniprocessor, the new
                //// thread does not get any processor time until the main thread
                //// is preempted or yields.  Uncomment the Thread.Sleep that
                //// follows t.Start() to see the difference.
                //t.Start();

                streamForLogFile.Close();
            }
        }

        private void Calculate(string[] Filenames1, SegyReader reader, StreamWriter streamForLogFile)
        {

            //считываем и меняем файлы
            for (int j = 0; j < Filenames1.Length; j++)
            {
                //считывание segy файла для добавление в исходный список segy файлов
                ISegyFile line = reader.Read(Filenames1[j]);
                line.FileInByte = File.ReadAllBytes(Filenames1[j]);
                logging.Append(Path.GetFileNameWithoutExtension(Filenames1[j]) + "\t");

                //записываем координаты
                PointD[] coordinates = Transform(line.Traces.Count, Path.GetFileNameWithoutExtension(Filenames1[j]));

                if (coordinates == null || coordinates.Length == 0)
                    continue;

                WriteCoordsToFile(coordinates, Filenames1[j]);

                for (int i = 0; i < line.Traces.Count; i++)
                {
                    line.Traces[i].Header.X = (int)Math.Round(coordinates[i].X, 0);
                    line.Traces[i].Header.Y = (int)Math.Round(coordinates[i].Y, 0);
                    byte[] coordXinBytes = BitConverter.GetBytes(line.Traces[i].Header.X).Reverse().ToArray();
                    byte[] coordYinBytes = BitConverter.GetBytes(line.Traces[i].Header.Y).Reverse().ToArray();
                    //Console.WriteLine(IbmConverter.ToInt32(coordXinBytes,0));
                    //Console.WriteLine(IbmConverter.ToInt32(coordYinBytes,0));
                    for (int k = 0; k < coordXinBytes.Length; k++)
                    {
                        line.Traces[i].Header.TextHeader[reader.XLocation - 1 + k] = coordXinBytes[k];
                        line.Traces[i].Header.TextHeader[reader.YLocation - 1 + k] = coordYinBytes[k];
                    }
                }


                //записываем segy в файл
                string[] nameFile = Filenames1[j].Split('.');
                File.WriteAllBytes(nameFile[0] + "_izm.segy", SegyToByte(line));
                logging.Append(" - OK"); //файл записался успешно
                streamForLogFile.WriteLine(logging.ToString());
                logging.Clear();
            }
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
                logging.Append($" - {countOfNull} count of Null values\t");
                int countNullOfEnd = (int)countOfNull / 2;
                int countNullOfStart = countOfNull - countNullOfEnd;
                logging.Append($" - {countNullOfEnd} points added at the end (Null)\t");
                logging.Append($" - {countNullOfStart} points added at the start (Null)\t");
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
                logging.Append($" - no Null values\t");
            }

            //Проверка последней ячейки
            (double dx, double dy) dXdYOfSegment = DXDYOfSegment(points[points.Length - 3], points[points.Length - 2]);
            (double dx, double dy) dXdYOfLastSegmentEnd = DXDYOfSegment(points[points.Length - 2], points[points.Length - 1]);
            double lengthOfLastSegmentEnd = LengthOfSegment(points[points.Length - 2], points[points.Length - 1]);

            double diffDXOfSegmentAllAndEnd = dXdYOfSegment.dx - dXdYOfLastSegmentEnd.dx;
            double diffDYOfSegmentAllAndEnd = dXdYOfSegment.dy - dXdYOfLastSegmentEnd.dy;

            int numberOfTracesInTheEndSegment = (int)(lengthOfLastSegmentEnd / distanceOfTraces);
            logging.Append($" - {numberOfTracesInTheEndSegment} traces can be included between last and previous traces\t");
            if (0 <= numberOfTracesInTheEndSegment && numberOfTracesInTheEndSegment < 3)
            {
                points[points.Length - 1] = new PointD(points[points.Length - 2].X + dXdYOfSegment.dx, points[points.Length - 2].Y + dXdYOfSegment.dy);
                logging.Append($" - last point was changed (Distance)\t");
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
                logging.Append($" - {shiftOnStart + 1} points added at the end (Distance)\t");
                logging.Append($" - {shiftOnStart} points shifted forward at the start (Distance)\t");
            }
            return points;
        }

        //Запись координат в отдельный файл
        private static void WriteCoordsToFile(PointD[] points, string FileName)
        {
            string path = Path.GetDirectoryName(FileName);
            string filename = Path.GetFileNameWithoutExtension(FileName);
            string newFileName = path + "\\" + filename + "_coords.txt";
            StreamWriter streamWriter = new StreamWriter(newFileName);
            streamWriter.WriteLine("X\tY");
            foreach (PointD point in points)
            {
                if (point != null)
                    streamWriter.WriteLine(point.X + "\t" + point.Y);
            }
            streamWriter.Close();
        }

        /// <summary>
        /// Считывает в отдельную коллекцию координаты трасс  
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="ShiftLocation"></param>
        /// <returns></returns>

        private PointD[] Transform(int countOfTraces, string lineName)
        {
            PointD[] coordinates = new PointD[countOfTraces];
            List<PointD> cornerPoints = cornerPointsAllProfiles[lineName];

            if (cornerPoints == null || cornerPoints.Count == 0)
            {
                logging.Append("No corner points\t");
                return null;
            }
            logging.Append($" - {cornerPoints.Count} corner points\t");
            coordinates[0] = new PointD(cornerPoints[0].X, cornerPoints[0].Y);
            coordinates[coordinates.Length - 1] = new PointD(cornerPoints[cornerPoints.Count - 1].X, cornerPoints[cornerPoints.Count - 1].Y);
            double distanceOfTrace = 0;
            double lengthOfProfile = LengthOfProfile(cornerPoints); //длина всего профиля
            logging.Append($" - {lengthOfProfile} length of profile\t");

            if (checkBox1.Checked)
            {
                distanceOfTrace = (double)numericUpDown1.Value;
            }
            else
            {                
                distanceOfTrace = lengthOfProfile / (countOfTraces - 1); //расстояние между трассами
            }

            logging.Append($" - {distanceOfTrace} distance between traces\t");

            PointD startPointOfSegment = new PointD(cornerPoints[0].X, cornerPoints[0].Y);
            int numberOfTrace = 0;

            if (cornerPoints.Count == 2)
            {
                lengthOfProfile = LengthOfSegment(cornerPoints[0], cornerPoints[1]); //длина всего профиля
                int numTracesForProfile = (int)(lengthOfProfile / distanceOfTrace);
                if (numTracesForProfile == (countOfTraces - 1))
                {
                    numTracesForProfile--;
                }
                //приращение по х и у для каждой трассы в сегменте
                (double dx, double dy) dXdYOfProfile = DXDYOfSegment(cornerPoints[0], cornerPoints[1]);
                double dXOfTrace = distanceOfTrace * dXdYOfProfile.dx / lengthOfProfile;
                double dYOfTrace = distanceOfTrace * dXdYOfProfile.dy / lengthOfProfile;

                if (numTracesForProfile >= countOfTraces) numTracesForProfile = countOfTraces - 2;
                //расчет координат трасс в сегменте и добавление их в общую коллекцию
                for (int k = 1; k <= numTracesForProfile; k++)
                {
                    numberOfTrace++;
                    startPointOfSegment.X += dXOfTrace;
                    startPointOfSegment.Y += dYOfTrace;
                    coordinates[numberOfTrace] = new PointD(startPointOfSegment.X, startPointOfSegment.Y);
                }
            }
            else
            {
                for (int i = 0; i < cornerPoints.Count - 1; i++)
                {
                    //расчет начальной точки каждого сегмента так, чтобы расстояние между трассами сохранялось
                    if (i > 0)
                    {
                        numberOfTrace++;
                        startPointOfSegment = CalculateNextStartPoint(startPointOfSegment, cornerPoints[i], cornerPoints[i + 1], distanceOfTrace);
                        coordinates[numberOfTrace] = new PointD(startPointOfSegment.X, startPointOfSegment.Y);
                    }

                    //длина сегмента и количество трасс в сегменте
                    double lengthOfSegmentFromStartPoint = LengthOfSegment(startPointOfSegment, cornerPoints[i + 1]);
                    int numTracesForSegment = (int)(lengthOfSegmentFromStartPoint / distanceOfTrace);

                    //приращение по х и у для каждой трассы в сегменте
                    (double dx, double dy) dXdYOfSegment = DXDYOfSegment(cornerPoints[i], cornerPoints[i + 1]);
                    double lengthOfSegment = LengthOfSegment(cornerPoints[i], cornerPoints[i + 1]);
                    double dXOfTrace = distanceOfTrace * dXdYOfSegment.dx / lengthOfSegment;
                    double dYOfTrace = distanceOfTrace * dXdYOfSegment.dy / lengthOfSegment;
                    
                    if ((numberOfTrace + numTracesForSegment) >= countOfTraces) numTracesForSegment = countOfTraces - numberOfTrace - 2;
                    //расчет координат трасс в сегменте и добавление их в общую коллекцию
                    for (int k = 1; k <= numTracesForSegment; k++)
                    {
                        numberOfTrace++;
                        startPointOfSegment.X += dXOfTrace;
                        startPointOfSegment.Y += dYOfTrace;
                        coordinates[numberOfTrace] = new PointD(startPointOfSegment.X, startPointOfSegment.Y);
                    }
                    
                }
            }
            coordinates = CheckForNullAndDistance(coordinates, distanceOfTrace);
            return coordinates;
        }

        //Расчет следующей начальной точки следующего сегмента
        //Рассчитывается таким образом, чтобы точка находилась на линии сегмента и
        //расстояние между ней и конечной точкой предыдущего сегмента было равно заданному расстоянию между трассами
        private static PointD CalculateNextStartPoint(PointD pointPrev, PointD pointStart, PointD pointEnd, double distanceOfTrace)
        {
            double shiftFromStart = CalculateShiftFromStart(pointPrev, pointStart, pointEnd, distanceOfTrace);

            (double dx, double dy) dXdYOfSegment = DXDYOfSegment(pointStart, pointEnd);
            double lengthOfSegment = LengthOfSegment(pointStart, pointEnd);
            double dXOfTrace = shiftFromStart * dXdYOfSegment.dx / lengthOfSegment;
            double dYOfTrace = shiftFromStart * dXdYOfSegment.dy / lengthOfSegment;

            return new PointD(pointStart.X + dXOfTrace, pointStart.Y + dYOfTrace);
        }

        //Расчет расстояния между следующей начальной точкой и стартом сегмента
        private static double CalculateShiftFromStart(PointD pointPrev, PointD pointStart, PointD pointEnd, double distanceOfTrace)
        {
            double sinAngle = CalCulateSinAngle(pointPrev, pointStart, pointEnd);
            double lengthOfSegment = LengthOfSegment(pointPrev, pointStart);
            double deltaPerpPrev = lengthOfSegment * sinAngle;
            double deltaPerpStart = Math.Sqrt(lengthOfSegment * lengthOfSegment - deltaPerpPrev * deltaPerpPrev);
            double deltaPerpNext = Math.Sqrt(distanceOfTrace * distanceOfTrace - deltaPerpPrev * deltaPerpPrev);
            double deltaStartNext = deltaPerpNext;

            if (deltaPerpStart < deltaPerpNext)
                deltaStartNext -= deltaPerpStart;

            return deltaStartNext;
        }

        //Расчет синуса угла из косинуса
        private static double CalCulateSinAngle(PointD pointPrev, PointD pointStart, PointD pointEnd)
        {
            double cosA = CalCulateCosA(pointPrev, pointStart, pointEnd);
            return (double)Math.Sqrt(1 - cosA * cosA);
        }

        //Расчет косинуса угла между двумя векторами
        private static double CalCulateCosA(PointD pointPrev, PointD pointStart, PointD pointEnd)
        {
            double multiplyOfModuls = LengthOfSegment(pointPrev, pointStart) * LengthOfSegment(pointStart, pointEnd);
            return CalCulateScalarMultiply(pointPrev, pointStart, pointEnd) / multiplyOfModuls;
        }

        //Расчет скалярного произведения двух векторов
        private static double CalCulateScalarMultiply(PointD pointPrev, PointD pointStart, PointD pointEnd)
        {
            PointD pointStartPrev = Subtract(pointStart, pointPrev);
            PointD pointEndStart = Subtract(pointEnd, pointStart);
            return (pointStartPrev.X * pointEndStart.X + pointStartPrev.Y * pointEndStart.Y);
        }

        //Расчет смещений по х и у вдоль сегмента
        private static (double dx, double dy) DXDYOfSegment(PointD point1, PointD point2)
        {
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            return (dx, dy);
        }

        //Расчет длины всего профиля, состоящего из сегментов
        private static double LengthOfProfile(List<PointD> points)
        {
            double length = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                double lengthOfSegment = LengthOfSegment(points[i], points[i + 1]);
                length += lengthOfSegment;
            }
            return length;
        }

        //Расчет длины сегмента по координатам точек начала и конца
        private static double LengthOfSegment(PointD point1, PointD point2)
        {
            return (double)(Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y)));
        }

        //Расчет разницы двух векторов point1 - point2
        private static PointD Subtract(PointD point1, PointD point2)
        {
            PointD result = new PointD(point1.X, point1.Y);
            result.X -= point2.X;
            result.Y -= point2.Y;
            return result;
        }

        //Сохранение экземпляра класса ISegyFile в массив байтов
        byte[] SegyToByte(ISegyFile isgy)
        {

            byte[] segybyte = new byte[isgy.FileInByte.Length];
            for (int i = 0; i < 3600; i++)
            {
                segybyte[i] = isgy.FileInByte[i];
            }
            int m = 3600;

            if (isgy.Traces != null)
            {

                for (int j = 0; j < isgy.Traces.Count; j++)
                {
                    isgy.Traces[j].Header.TextHeader.CopyTo(segybyte, m);
                    m += isgy.Traces[j].Header.TextHeader.Length;
                    for (int k = 0; k < isgy.Traces[j].Values.Count; k++)
                    {
                        byte[] bt = BitConverter.GetBytes(isgy.Traces[j].Values[k]).Reverse().ToArray();
                        if (BitConverter.IsLittleEndian == false)
                        { bt = BitConverter.GetBytes(isgy.Traces[j].Values[k]); }
                        //float ddd = BitConverter.ToSingle(bt, 0);
                        bt.CopyTo(segybyte, m);
                        //IEnumerable<byte> auto = new[] { segybyte, bt }.SelectMany(s => s);
                        m += bt.Length;
                    }
                }
            }
            if (m < isgy.FileInByte.Length)
            {
                byte[] sdybt = new byte[m];
                for (int s = 0; s < m; s++)
                {
                    sdybt[s] = segybyte[s];
                }
                segybyte = sdybt;
            }
            short formatnum = 5;
            byte[] fn = BitConverter.GetBytes(formatnum).Reverse().ToArray();
            fn.CopyTo(segybyte, 3224);
            return segybyte;

        }



        private void Form1_Load(object sender, EventArgs e)
        {

        }



        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                string[] Filenames1 = openFileDialog1.FileNames;

                //считываем и меняем файлы
                for (int j = 0; j < Filenames1.Length; j++)
                {
                    string line;
                    StreamReader streamReader = new StreamReader(Filenames1[j]);
                    List<string> linesList = new List<string>();
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        linesList.Add(line.ToString());
                    }
                    List<PointD> cornerPoints = new List<PointD>();
                    char[] razdelitel = new char[] { '\t', ' ', ',' };
                    string[] tempLine = linesList[1].Split(razdelitel);
                    string nameOfProfile = tempLine[1];
                    for (int i = 1; i < linesList.Count; i++)
                    {
                        string[] temp = linesList[i].Split(razdelitel);
                        PointD cornerPoint = new PointD(Convert.ToDouble(temp[2]), Convert.ToDouble(temp[3]));
                        if (temp[1] != nameOfProfile)
                        {
                            cornerPointsAllProfiles[nameOfProfile] = cornerPoints;
                            cornerPoints = new List<PointD>();
                            nameOfProfile = temp[1];
                        }
                        cornerPoints.Add(cornerPoint);
                    }

                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked) { numericUpDown1.Enabled = true; }
            else { numericUpDown1.Enabled = false; }
        }
    }
}
