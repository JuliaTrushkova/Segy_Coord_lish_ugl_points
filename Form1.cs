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
                Logging.FileOfLogs = $"{pathtofiles}\\logfile_{dateTime: dd_MM_hhh_mmm} .txt";

                //считываем и меняем файлы
                for (int j = 0; j < Filenames1.Length; j++)
                {
                    try
                    {
                        //считывание segy файла для добавление в исходный список segy файлов
                        ISegyFile line = reader.Read(Filenames1[j]);
                        line.FileInByte = File.ReadAllBytes(Filenames1[j]);

                        //Считывание названия профиля, расстояния между трассами и количества трасс
                        string NameOfProfile = Path.GetFileNameWithoutExtension(Filenames1[j]);
                        Logging.SendNameOfLine(NameOfProfile);

                        double initialDistanceOfTrace = checkBox1.Checked ? (double)numericUpDown1.Value : 0;

                        int countOfTraces = line.Traces.Count;
                        PointD[] coordinates = new PointD[countOfTraces];

                        //записываем координаты
                        if (cornerPointsAllProfiles.TryGetValue(NameOfProfile, out List<Vector> vectorOfCornerPoints))
                            coordinates = VectorPointTransform.TransformCornerPointsToLinePoints(countOfTraces, vectorOfCornerPoints, initialDistanceOfTrace);

                        if (coordinates == null || coordinates.Length == 0)
                            continue;

                        //Записываем координаты в файл
                        VectorPointTransform.WriteCoordsToFile(coordinates, Filenames1[j]);

                        //Записываем координаты в заголовки трасс
                        bool IsInteger = false;
                        if (radioButton1.Checked) IsInteger = true;
                        if (radioButton2.Checked) IsInteger = false;
                        SegyWriter.WriteCoordinatesToTraceHeaders(ref line, coordinates, reader.XLocation, reader.YLocation, IsInteger);

                        //записываем segy в файл
                        string changedFileName = pathtofiles + "\\" + NameOfProfile + "_izm.segy";
                        File.WriteAllBytes(changedFileName, SegyWriter.SegyToByte(line));

                        Logging.SendOk(); //файл записался успешно           

                        Logging.WriteLogToFile();
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        MessageBox.Show("Corner points don't confirm segy file\n" + ex.Message);                                
                    }
                    catch (ArgumentNullException ex)
                    {
                        MessageBox.Show("Your file has not any corner points\n" + ex.Message);                       
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show("Your file is not correct\n" + ex.Message);                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);                        
                    }
                }
            }
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
                    try
                    {
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
                    catch (ArgumentNullException ex)
                    {
                        MessageBox.Show("Your file has not any corner points\n" + ex.Message);
                        cornerPointsAllProfiles.Clear();
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show("Your file is not correct\n" + ex.Message);
                        cornerPointsAllProfiles.Clear();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        cornerPointsAllProfiles.Clear();
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
