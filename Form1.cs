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

        Dictionary<string, List<Vector>> cornerPointsAllProfiles;
        

        public Form1()
        {
            InitializeComponent();
            cornerPointsAllProfiles = new Dictionary<string, List<Vector>>();
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
                string pathtofiles = Path.GetDirectoryName(Filenames1[0]);
                DateTime dateTime = DateTime.Now;
                Logging.FileOfLogs = $"{pathtofiles} + \\logfile_ + {dateTime: dd hh mm} +.txt";

                //считываем и меняем файлы
                for (int j = 0; j < Filenames1.Length; j++)
                {
                    //считывание segy файла для добавление в исходный список segy файлов
                    ISegyFile line = reader.Read(Filenames1[j]);
                    line.FileInByte = File.ReadAllBytes(Filenames1[j]);
                    
                    string NameOfProfile = Path.GetFileNameWithoutExtension(Filenames1[j]);
                    Logging.SendNameOfLine(NameOfProfile);
                    //записываем координаты
                    if (cornerPointsAllProfiles.TryGetValue())
                    PointD[] coordinates = VectorPointTransform.TransformCornerPointsToLinePoints(line.Traces.Count, Path.GetFileNameWithoutExtension(Filenames1[j]), );

                    if (coordinates == null || coordinates.Length == 0)
                        continue;

                    VectorPointTransform.WriteCoordsToFile(coordinates, Filenames1[j]);

                    for (int i = 0; i < line.Traces.Count; i++)
                    {
                        line.Traces[i].Header.X = (float)coordinates[i].X;
                        line.Traces[i].Header.Y = (float)coordinates[i].Y;

                        if (radioButton1.Checked)
                        {
                            byte[] coordXinBytes = BitConverter.GetBytes((int)line.Traces[i].Header.X).Reverse().ToArray();
                            byte[] coordYinBytes = BitConverter.GetBytes((int)line.Traces[i].Header.Y).Reverse().ToArray();

                            for (int k = 0; k < coordXinBytes.Length; k++)
                            {
                                line.Traces[i].Header.TextHeader[reader.XLocation - 1 + k] = coordXinBytes[k];
                                line.Traces[i].Header.TextHeader[reader.YLocation - 1 + k] = coordYinBytes[k];
                            }
                        }
                        if (radioButton2.Checked)
                        {
                            byte[] coordXinBytes = BitConverter.GetBytes(line.Traces[i].Header.X).ToArray();
                            byte[] coordYinBytes = BitConverter.GetBytes(line.Traces[i].Header.Y).ToArray();

                            for (int k = 0; k < coordXinBytes.Length; k++)
                            {
                                line.Traces[i].Header.TextHeader[reader.XLocation - 1 + 3 - k] = coordXinBytes[k];
                                line.Traces[i].Header.TextHeader[reader.YLocation - 1 + 3 - k] = coordYinBytes[k];
                            }
                        }
                    }

                    //записываем segy в файл
                    string[] nameFile = Filenames1[j].Split('.');
                    File.WriteAllBytes(nameFile[0] + "_izm.segy", SegyToByte(line));

                    Logging.SendOk(); //файл записался успешно           

                    using (StreamWriter streamForLogFile = File.AppendText(Logging.FileOfLogs))
                    {
                        streamForLogFile.WriteLine(Logging.LoggingMessage.ToString());
                    }

                    Logging.LoggingMessage.Clear();
                }

                // Calculate(Filenames1, reader, streamForLogFile);
                //Thread t = new Thread(new ThreadStart(ThreadProc));

                //// Start ThreadProc.  Note that on a uniprocessor, the new
                //// thread does not get any processor time until the main thread
                //// is preempted or yields.  Uncomment the Thread.Sleep that
                //// follows t.Start() to see the difference.
                //t.Start();

                //cornerPointsAllProfiles.Clear();                
            }
        }

        private void Calculate(string[] Filenames1, SegyReader reader, StreamWriter streamForLogFile)
        {


        }



        
        //Сохранение экземпляра класса ISegyFile в массив байтов
        byte[] SegyToByte(ISegyFile isgy)
        {

            byte[] segybyte = new byte[isgy.FileInByte.Length];
            const int BYTES_OF_SEGYHEAD = 3600;
            for (int i = 0; i < BYTES_OF_SEGYHEAD; i++)
            {
                segybyte[i] = isgy.FileInByte[i];
            }
            int numberOfBytes = BYTES_OF_SEGYHEAD;

            if (isgy.Traces != null)
            {

                for (int j = 0; j < isgy.Traces.Count; j++)
                {
                    isgy.Traces[j].Header.TextHeader.CopyTo(segybyte, numberOfBytes);
                    numberOfBytes += isgy.Traces[j].Header.TextHeader.Length;
                    for (int k = 0; k < isgy.Traces[j].Values.Count; k++)
                    {
                        byte[] bt = BitConverter.GetBytes(isgy.Traces[j].Values[k]).Reverse().ToArray();
                        if (BitConverter.IsLittleEndian == false)
                        { bt = BitConverter.GetBytes(isgy.Traces[j].Values[k]); }
                        //float ddd = BitConverter.ToSingle(bt, 0);
                        bt.CopyTo(segybyte, numberOfBytes);
                        //IEnumerable<byte> auto = new[] { segybyte, bt }.SelectMany(s => s);
                        numberOfBytes += bt.Length;
                    }
                }
            }
            if (numberOfBytes < isgy.FileInByte.Length)
            {
                byte[] sdybt = new byte[numberOfBytes];
                for (int s = 0; s < numberOfBytes; s++)
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
                        if (temp[1] != nameOfProfile || i == linesList.Count - 1)
                        {
                            if (i == linesList.Count - 1)
                                cornerPoints.Add(cornerPoint);
                            List<Vector> vectorsOfCornerPoints = VectorPointTransform.ConvertPointsToVectors(cornerPoints);
                            cornerPointsAllProfiles[nameOfProfile] = vectorsOfCornerPoints;
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

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
