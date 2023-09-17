
namespace Unplugged.Segy
{
    public interface IFileHeader
    {
        // Любой segy файл должен содержать текстовый заголовок, информацию о формате записи и типе написания байтовых чисел
        // Первые 3200 байт - это заголовок в EBCDIC/Ascii формате. Информация о файле (как записывался, обрабатывался).
        // Может быть пустым. Содержит 40 строк
        // Следующие 400 байт - бинарный заголовок файла. В нем содержатся параметры для всего файла.
        //
        // 3205-3208 - Line number
        //
        // 3213–3214 - Number of data traces per ensemble. Mandatory for prestack data.
        //
        // 3217–3218 - Sample interval. Microseconds (µs) for time data, Hertz (Hz) for frequency data, meters(m) or feet(ft) for depth data.
        //
        // 3221–3222 - Number of samples per data trace. 
        //
        // 3225–3226 - Data sample format code. (1 = 4-byte IBM floating-point, 2 = 4-byte, two's complement integer, 3 = 2-byte, two's complement integer,
        // 4 = 4-byte fixed-point with gain(obsolete), 5 = 4-byte IEEE floating - point, 6 = 8-byte IEEE floating - point, 7 = 3-byte two’s complement integer,
        // 8 = 1-byte, two's complement integer, 9 = 8-byte, two's complement integer, 10 = 4-byte, unsigned integer, 11 = 2-byte, unsigned integer,
        // 12 = 8-byte, unsigned integer, 15 = 3-byte, unsigned integer, 16 = 1-byte, unsigned integer
        //
        // 3227–3228 - Ensemble fold — The expected number of data traces per trace ensemble (e.g.the CMP fold).
        //
        // 3229–3230 - Trace sorting code (i.e. type of ensemble): –1 = Other (should be explained in a user Extended Textual File Header stanza), 0 = Unknown,
        // 1 = As recorded(no sorting), 2 = CDP ensemble, 3 = Single fold continuous profile, 4 = Horizontally stacked, 5 = Common source point, 6 = Common receiver point,
        // 7 = Common offset point, 8 = Common mid-point, 9 = Common conversion point
        //
        // 3297–3300 - The integer constant 1690906010 (0102030416). This is used to detect the byte ordering to expect for this SEG-Y file (big-endian, little-endian).
        //
        // 3501 - for SEG-Y Revision 2.0, as defined in this document, this will be recorded as 0216.
        // This field is mandatory for all versions of SEG-Y, although a value of zero indicates “traditional” SEG-Y conforming to the 1975 standard.
        //
        // 3503–3504 - Fixed length trace flag. A value of one indicates that all traces in this SEG-Y
        // file are guaranteed to have the same sample interval, number of trace header blocks and trace samples.
        // A value of zero indicates that the length of the traces in the file may vary and the number of samples in bytes 115–116 of the Standard SEG-Y Trace Header
        // and, if SEGY 2.0, bytes 137–140 of SEG-Y Trace Header Extension 1 must be examined to determine the actual length of each trace.


        string Text { get; }
        FormatCode SampleFormat { get; }
        bool IsLittleEndian { get; }
        byte[] TextInByte { get; }
    }
}
