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

namespace Segy_Coord
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Создание класс-оператора для чтения segy файла
            SegyReader reader = new SegyReader();
            //указание номера байта для координат Х и Y и шифта
            reader.XLocation = Int32.Parse(textBox3.Text);
            reader.YLocation = Int32.Parse(textBox4.Text);
            reader.ShiftLocation = Int32.Parse(textBox1.Text);

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

                //Создание log файла, куда запишутся имена профилей и соответствующие шифты
                StreamWriter streamForLogFile = File.CreateText(pathtofiles + "logfile.txt");

                //считываем и меняем файлы
                for (int j = 0; j < Filenames1.Length; j++)
                { 
                    //считывание segy файла для добавление в исходный список segy файлов
                    ISegyFile line = reader.Read(Filenames1[j]);
                    line.FileInByte = File.ReadAllBytes(Filenames1[j]);

                    //записываем в log файл имя профиля и соответствующий шифт
                    string[] nameProfile = Filenames1[j].Split('\\');
                    streamForLogFile.WriteLine(nameProfile[nameProfile.Length - 1] + " - shift " + line.Traces[0].Header.Shift);

                    //вводим шифт в трассы
                    for (int k = 0; k < line.Traces.Count; k++)
                    {
                        line.Traces[k] = MakeSfift(line.Traces[k], reader.ShiftLocation);
                    }
                                      
                    //записываем segy в файл
                    string[] nameFile = Filenames1[j].Split('.');
                    File.WriteAllBytes(nameFile[0]+ "_izm.sgy", SegyToByte(line));                    
                }

                streamForLogFile.Close(); 
                
            }
        }

        /// <summary>
        /// Вводит шифт в профиль
        /// Если в заголовках записан отрицательный шифт, то это значит, что профиль приподнимался, и надо сверху обрезать до 0, а сниз добавить нулей
        /// Если в заголовках записан положительный шифт, то это значит, что профиль опускался, и надо заполнить сверху нулями до уровня 0
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="ShiftLocation"></param>
        /// <returns></returns>

        private ITrace MakeSfift (ITrace trace, int ShiftLocation)
        {
            List<float> newTraceAmplitudes = new List<float>();
            int countOfShiftSamples = (int) Math.Abs(trace.Header.Shift / trace.Header.Sample);
            int countOfTraceListWithoutShift = newTraceAmplitudes.Count - countOfShiftSamples;
            if (trace.Header.Shift < 0)
            {
                for (int i = 0; i < countOfTraceListWithoutShift; i++)
                {
                    newTraceAmplitudes.Add(trace.Values[i + countOfShiftSamples]);
                }
                for (int i = 0; i < countOfShiftSamples; i++)
                {
                    newTraceAmplitudes.Add(0.0f);
                }
            }
            else if (trace.Header.Shift > 0)
            {
                for (int i = 0; i < countOfShiftSamples; i++)
                {
                    newTraceAmplitudes.Add(0.0f);
                }
                for (int i = 0; i < countOfTraceListWithoutShift; i++)
                {
                    newTraceAmplitudes.Add(trace.Values[i]);
                }
            }
            trace.Values = newTraceAmplitudes;
            trace.Header.Shift = 0;
            trace.Header.TextHeader[ShiftLocation] = 0;
            trace.Header.TextHeader[ShiftLocation + 1] = 0;
            return trace;
        }

        


        byte[] SegyToByte(ISegyFile isgy)
        {
            //byte[] segybyte = isgy.FileInByte.Take(3600).ToArray();
            //int m = 3600;
            //if (isgy.Traces != null)
            //{
            //    for (int j = 0; j < isgy.Traces.Count; j++)
            //    {
            //        isgy.Traces[j].Header.TextHeader.CopyTo(segybyte, m);

            //        m = m + isgy.Traces[j].Header.TextHeader.Length;
            //        for (int k = 0; k<isgy.Traces[j].Values.Count; k++)
            //        {
            //            byte[] bt = BitConverter.GetBytes(isgy.Traces[j].Values[k]);
            //            float ddd = BitConverter.ToSingle(bt, 0);

            //            // bt.CopyTo(segybyte, m);
            //            IEnumerable<byte> auto = new[] {segybyte, bt}.SelectMany(s => s);
            //            m = m + bt.Length;
            //        }
            //    }
            //}
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

        
        
    }
}
