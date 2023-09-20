using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unplugged.Segy;

namespace Segy_Coord
{
    internal class SegyWriter
    {

        public static void WriteCoordinatesToTraceHeaders(ref ISegyFile line, PointD[] coordinates, int byteOfXlocation, int byteOfYlocation, bool IsInteger)
        {
            for (int i = 0; i < line.Traces.Count; i++)
            {
                if (IsInteger)
                {
                    WriteBytesToTraceHeaders(ref line, coordinates, byteOfXlocation, byteOfYlocation, i, true);
                }
                else
                {
                    WriteBytesToTraceHeaders(ref line, coordinates, byteOfXlocation, byteOfYlocation, i, false);
                }
            }

        }

        private static void WriteBytesToTraceHeaders(ref ISegyFile line, PointD[] coordinates, int byteOfXlocation, int byteOfYlocation, int index, bool IsInteger)
        {

            line.Traces[index].Header.X = (float)coordinates[index].X;
            line.Traces[index].Header.Y = (float)coordinates[index].Y;

            float X = line.Traces[index].Header.X;
            float Y = line.Traces[index].Header.Y;

            if (IsInteger)
            {
                X = (int)Math.Round(X, 0);
                Y = (int)Math.Round(Y, 0);
            }

            byte[] coordXinBytes = BitConverter.GetBytes(X).Reverse().ToArray();
            byte[] coordYinBytes = BitConverter.GetBytes(Y).Reverse().ToArray();
            
            for (int k = 0; k < 4; k++)
            {
                line.Traces[index].Header.TextHeader[byteOfXlocation - 1 + k] = coordXinBytes[k];
                line.Traces[index].Header.TextHeader[byteOfYlocation - 1 + k] = coordYinBytes[k];
            }
        }

        //Сохранение экземпляра класса ISegyFile в массив байтов
        public static byte[] SegyToByte(ISegyFile isgy)
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

    }
}
