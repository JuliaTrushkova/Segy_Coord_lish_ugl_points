using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Segy_Coord
{
    internal class Logging
    {
        public static StringBuilder LoggingMessage = new StringBuilder();

        public static StringBuilder LoggingError = new StringBuilder();

        public static string FileOfLogs;

        public static void SendOk()
        {
            LoggingMessage.Append(" - OK");
        }

        public static void SendCountOfNullValues(int countOfNull)
        {
            LoggingMessage.Append($" - {countOfNull} count of Null values\t");
        }

        public static void SendCountOfAddedEndPointsForNull(int countNullOfEnd)
        {
            LoggingMessage.Append($" - {countNullOfEnd} points added at the end (Null)\t");
        }

        public static void SendCountOfAddedStartPointsForNull(int countNullOfStart)
        {
            LoggingMessage.Append($" - {countNullOfStart} points added at the start (Null)\t");
        }

        public static void SendNoNullValues()
        {
            LoggingMessage.Append(" - no Null values\t");
        }

        public static void SendCountOfAddedEndTraces(int numberOfTracesInTheEndSegment)
        {
            LoggingMessage.Append($" - {numberOfTracesInTheEndSegment} traces can be included between last and previous traces\t");
        }

        public static void SendLastPointChanged()
        {
            LoggingMessage.Append(" - last point was changed (Distance)\t");
        }

        public static void SendCountOfAddedEndPointsForDistance(int shiftOnStart)
        {
            LoggingMessage.Append($" - {shiftOnStart + 1} points added at the end (Distance)\t");
        }

        public static void SendCountOfAddedStartPointsForDistance(int shiftOnStart)
        {
            LoggingMessage.Append($" - {shiftOnStart} points shifted forward at the start (Distance)\t");
        }

        public static void SendNoCornerPoints()
        {
            LoggingMessage.Append(" - No corner points\t");
        }

        public static void SendCountOfCornerPoints(int countOfCornerPoints)
        {
            LoggingMessage.Append($" - {countOfCornerPoints} corner points\t");
        }

        public static void SendLengthOfProfile(double lengthOfProfile)
        {
            LoggingMessage.Append($" - {lengthOfProfile} length of profile\t");
        }

        public static void SendDistanceOfTrace(double distanceOfTrace)
        {
            LoggingMessage.Append($" - {distanceOfTrace} distance between traces\t");
        }

        public static void SendNameOfLine(string NameOfLine)
        {
            LoggingMessage.Append(Path.GetFileNameWithoutExtension(NameOfLine) + "\t");
        }

        public static void WriteLogToFile()
        {
            using (StreamWriter streamForLogFile = File.AppendText(FileOfLogs))
            {
                streamForLogFile.WriteLine(LoggingMessage.ToString());
            }

            LoggingMessage.Clear();
        }
    }
}
