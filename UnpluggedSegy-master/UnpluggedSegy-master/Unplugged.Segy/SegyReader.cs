using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unplugged.IbmBits;
using Unplugged.Segy;

namespace Unplugged.Segy
{
    /// <summary>
    /// Responsible for reading SEGY files given a path or a Stream
    // BitConverter по умлочанию “little-endian”

    /// 1. Три перегруженных метода для чтения файла (первые два постепенно ссылаются на третий с большим количеством аргументов)
    /// ISegyFile Read(string path, IReadingProgress progress = null)     //если на вход подается только путь и прогресс, то создается новый поток с эти путем, файл записывается в память и запускается второй метод
    /// ISegyFile Read(Stream stream, IReadingProgress progress = null)     //если на вход подается только поток и прогресс, то вызывается третий метод с кол-вом трасс равным int.Maxvalue
    /// ISegyFile Read(Stream stream, int traceCount, IReadingProgress progress = null)     //Сначала записывает весь файл в переменную BinaryReader, 
    /// затем считывает заголовок методом ReadFileHeader(reader), после этого считывет нужное количество трасс в память, 
    /// используя для каждой трассы метод ReadTrace(reader, fileHeader.SampleFormat, fileHeader.IsLittleEndian). int.Maxvalue - все трассы

    // 2. IFileHeader ReadFileHeader(BinaryReader reader)         // (вызывается из ISegyFile Read) - 
    // в переменную типа string записывается текстовый заголовок файла с помощью метода ReadTextHeader(BinaryReader reader) (первые 3200 байт)
    // затем создается переменная типа FileHeader и в нее сразу записывается бинарный заголовок файла с помощью метода ReadBinaryHeader (след. 400 байт)
    // также в ее свойтво text записывается наша переменная типа string

    /// 3. Три перегруженных метода для чтения заголовка файла (первые два постепенно ссылаются на третий с большим количеством аргументов)
    /// string ReadTextHeader(string path)     //если на вход подается только путь, то создается новый поток с этим путем и запускается второй метод
    /// string ReadTextHeader(Stream stream)     //если на вход подается только поток, то создается новый BinaryReader с этим потоком и вызывается третий метод
    /// string ReadTextHeader(BinaryReader reader)    //(вызывается из ReadFileHeader) переменная textHeaderLength - длина заголовка (обычно 3200 байт)
    /// в переменную байт записывается байт в кол-ве textHeaderLength (3200) из основного потока с начала файла с помощью стандартного метода reader.ReadBytes(textHeaderLength)
    /// Затем конвертируем в string: Если EBCDIC=false (отсутствует) или первый символ равен char 'C', используем простой конвертер ASCIIEncoding.Default.GetString(bytes)
    /// Если наоборот, конвертируем в string методом IbmConverter.ToString(bytes);
    /// И если можно, то вставляем новую строку методом InsertNewLines. Если нет, то возвращаем как есть

    // 4. IFileHeader ReadBinaryHeader(BinaryReader reader)    //(вызывается из ReadFileHeader как и п.3)
    // Считывает из основного потока байты в размере _binaryHeaderSize (400 байт) - бинарный заголовок файла - простым методом reader.ReadBytes(_binaryHeaderSize)
    // Затем проверяет какой порядок байтов использован в файле: считывает значения с 25 и 26 байта (там записан формат чисел в файле). 
    // Если 26 байт равен 0, то это Little-endian и FormatCode считывается с 25 байта. Если 26 байт не равен 0, то это big-endian и формат чисел FormatCode считывается с 26 байта.
    // 
    // ????Куда записывает текстовый заголовок не понятно????? - добавила запись в свойство TextInByte

    /// 
    /// 5. ITrace ReadTrace(BinaryReader reader, FormatCode sampleFormat, bool isLittleEndian) // (вызывается из ISegyFile Read)
    /// Считывает из основного потока байты в размере _traceHeaderSize (240 байт) - бинарный заголовок трассы - методом ITraceHeader ReadTraceHeader(BinaryReader reader, bool isLittleEndian)
    /// Затем считывает значения амплитуд для трассы в коллекцию List<float> методом IList<float> ReadTrace(BinaryReader reader, FormatCode sampleFormat, int sampleCount, bool isLittleEndian)
    /// И вовзращает новый экземпляр класса ITrace
     
    // 6. Два перегруженных метода для чтения заголовка файла (первый ссылается на второй с большим количеством аргументов)
    // ITraceHeader ReadTraceHeader(BinaryReader reader)   //если на вход подается только reader, то запускается второй метод с этим же reader и isLittleEndian = false
    // ITraceHeader ReadTraceHeader(BinaryReader reader, bool isLittleEndian) // (вызывается из ITrace ReadTrace)
    // Создает новую переменную traceheader. В свойство byte[] TextHeader записывает заголовок в размере _traceHeaderSize (240 байт)
    // Из этого массива достает другие свойства traceheader: CrosslineNumber=TraceNumber, InlineNumber, X, Y, SampleCount - можно задавать еще добавляя в класс ITraceHeader и TraceHeader
    // Номера байтов, соответствующих параметру, берет из свойств и полей класса SegyReader (есть конструктор по умолчанию)
    // Если параметр содержится в 4 байтах (смотреть в спецификации к SEGY), то считывает методом int ToInt32(byte[] bytes, int index, bool isLittleEndian) из текущего класса SegyReader
    // Если параметр содержится в 2 байтах (смотреть в спецификации к SEGY), то считывает методом int ToInt16(byte[] bytes, int index, bool isLittleEndian) из текущего класса SegyReader
    
    /// 7. IList<float> ReadTrace(BinaryReader reader, FormatCode sampleFormat, int sampleCount, bool isLittleEndian) //  (вызывается из ITrace ReadTrace)
    /// В зависимости от формата данных FormatCode считывает значения амплитуд с трассы в коллекцию IList<float>
    /// 







    /// string InsertNewLines(string text)
    
    // int ToInt16(byte[] bytes, int index, bool isLittleEndian) //(вызывается из ITraceHeader ReadTraceHeader)
    // Если байты в формате little-endian, то преобразование происходит через стандартный метод BitConverter.ToInt16(bytes, index)
    // Если не little-endian, то возвращает преобразованное число типа short из 2 байт методом IbmConverter.ToInt16(value)
    // из namespace Unplugged.IbmBits класс IbmConverter. 
    // Метод переворачивает массив byte[] и получает число short через стандартный метод BitConverter.ToInt16(value2, 0);
    
    /// int ToInt32(byte[] bytes, int index, bool isLittleEndian) //(вызывается из ITraceHeader ReadTraceHeader)
    /// Если байты в формате little-endian, то преобразование происходит через стандартный метод BitConverter.ToInt32(bytes, index)
    /// Если не little-endian, то возвращает преобразованное число типа short из 4 байт методом IbmConverter.ToInt32(value)
    /// из namespace Unplugged.IbmBits класс IbmConverter. 
    /// Метод переворачивает массив byte[] и получает число int через стандартный метод BitConverter.ToInt32(value2, 0);
     
    // float ReadSignedByte(BinaryReader reader) (вызывается из IList<float> ReadTrace)
    // Считывает один байт. Если байт меньше 128, то возвращает байт. Если больше 128, то вычитает 256 и возвращает (тк signed)
    
    /// float ReadReversedSingle(BinaryReader reader) (вызывается из IList<float> ReadTrace)
    /// Этот метод считывает 4 байта в массив byte[] b, переворачивает и подает на вход стандартному методу BitConverter.ToSingle(b, 0);
    
    // byte[] ReverseBytesCoord(byte[] ish, int loc)
    /// 

    /// </summary>
    public class SegyReader
    {
        public ISegyOptions Options { get; set; }
        public int InlineNumberLocation { get; set; }
        public int CrosslineNumberLocation { get; set; }
        public int XLocation { get; set; }
        public int YLocation { get; set; }

        public int ShiftLocation { get; set; }

        public int SampleLocation { get; set; }

        private const int _binaryHeaderSize = 400;
        private const int _traceHeaderSize = 240;
        private const int _sampleFormatIndex = 24;
        private const int _sampleCountIndex = 114;


        public SegyReader()
        {
            Options = new SegyOptions();
            XLocation = 73;
            YLocation = 77;
            InlineNumberLocation = 189;
            CrosslineNumberLocation = 193;
            ShiftLocation = 104;
            SampleLocation = 117;
        }

        #region From the Top: Methods that start reading from the beginning of the file

        /// <summary>
        /// Given a file path, reads entire SEGY file into memory
        /// </summary>
        public virtual ISegyFile Read(string path, IReadingProgress progress = null)
        {
            using (var stream = File.OpenRead(path))
                return Read(stream, progress);
        }

        /// <summary>
        /// Given stream, reads entire SEGY file into memory.
        /// Assumes the stream is at the start of the file.
        /// </summary>
        public virtual ISegyFile Read(Stream stream, IReadingProgress progress = null)
        {
            return Read(stream, int.MaxValue, progress);
        }

        /// <summary>
        /// Given stream and traceCount, reads the requested number
        /// of traces into memory. The given traceCount may exceed
        /// the number of traces in the file; 
        /// in that case all the traces in the file are read.
        /// Assumes the stream is at the start of the file.
        /// </summary>
        public virtual ISegyFile Read(Stream stream, int traceCount, IReadingProgress progress = null)
        {
            using (var reader = new BinaryReader(stream))
            {
                var fileHeader = ReadFileHeader(reader);
                var traces = new List<ITrace>();
                for (int i = 0; i < traceCount; i++)
                {
                    if (progress != null)
                    {
                        // TODO: Check if stream.Length breaks when streaming from web
                        int percentage = (int)(100 * stream.Position / stream.Length);
                        progress.ReportProgress(percentage);
                        if (progress.CancellationPending)
                            break;
                    }
                    var trace = ReadTrace(reader, fileHeader.SampleFormat, fileHeader.IsLittleEndian);
                    if (trace == null)
                        break;
                    traces.Add(trace);
                }
                return new SegyFile { Header = fileHeader, Traces = traces };
            }
        }

        /// <summary>
        /// Given a BinaryReader, reads the SEGY File Header into memory.
        /// Asummes the BinaryReader is at the start of the file.
        /// </summary>
        public virtual IFileHeader ReadFileHeader(BinaryReader reader)
        {
            var text = ReadTextHeader(reader);

            FileHeader header = ReadBinaryHeader(reader) as FileHeader;
            header.Text = text;
            return header;
        }

        /// <summary>
        /// Given a file path reads the text header from the beginning
        /// of the SEGY file.
        /// </summary>
        public virtual string ReadTextHeader(string path)
        {
            using (var stream = File.OpenRead(path))
                return ReadTextHeader(stream);
        }

        /// <summary>
        /// Given a stream reads the text header.
        /// Assumes the stream is at the start of the file.
        /// </summary>
        public virtual string ReadTextHeader(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
                return ReadTextHeader(reader);
        }

        /// <summary>
        /// Given a BinaryReader reads the text header.
        /// Assumes the BinaryReader is at the start of the file.
        /// </summary>
        public virtual string ReadTextHeader(BinaryReader reader)
        {
            var textHeaderLength = Options.TextHeaderColumnCount * Options.TextHeaderRowCount;

            var bytes = reader.ReadBytes(textHeaderLength);

            string text = (bytes[0] == 'C') || Options.IsEbcdic == false ?
                ASCIIEncoding.Default.GetString(bytes) :
                IbmConverter.ToString(bytes);
            return Options.TextHeaderInsertNewLines ? InsertNewLines(text) : text;
        }

        #endregion

        #region Already in progress: Methods that start reading from the current location in the stream

        /// <summary>
        /// Given a BinaryReader, reads the binary header.
        /// Assumes that the binary header is the next item to be read.
        /// </summary>
        public virtual IFileHeader ReadBinaryHeader(BinaryReader reader)
        {
            var binaryHeader = reader.ReadBytes(_binaryHeaderSize);

            var byte0 = binaryHeader[_sampleFormatIndex];
            var byte1 = binaryHeader[_sampleFormatIndex + 1];

            bool isLittleEndian = byte1 == 0;
            var sampleFormat = isLittleEndian ?
                    (FormatCode)byte0 :
                    (FormatCode)byte1;
            return new FileHeader { TextInByte = binaryHeader, SampleFormat = sampleFormat, IsLittleEndian = isLittleEndian };
        }

        /// <summary>
        /// Reads the trace (header and sample values).
        /// Assumes that the trace header is the next item to be read.
        /// </summary>
        public virtual ITrace ReadTrace(BinaryReader reader, FormatCode sampleFormat, bool isLittleEndian)
        {
            var header = ReadTraceHeader(reader, isLittleEndian);
            if (header == null)
                return null;
            var values = ReadTrace(reader, sampleFormat, header.SampleCount, isLittleEndian);
            return new Trace { Header = header, Values = values };
        }

        /// <summary>
        /// Given a BinaryReader, reads the trace header.
        /// Assumes that the trace header is the next item to be read.
        /// Assumes that the byte order is Big Endian.
        /// </summary>
        public virtual ITraceHeader ReadTraceHeader(BinaryReader reader)
        {
            return ReadTraceHeader(reader, false);
        }

        /// <summary>
        /// Given a BinaryReader, reads the trace header.
        /// Assumes that the trace header is the next item to be read.
        /// </summary>
        public virtual ITraceHeader ReadTraceHeader(BinaryReader reader, bool isLittleEndian)
        {
            var traceHeader = new TraceHeader();
            var headerBytes = reader.ReadBytes(_traceHeaderSize);
            traceHeader.TextHeader = headerBytes;
            if (headerBytes.Length < _traceHeaderSize)
                return null;

            if (headerBytes.Length >= CrosslineNumberLocation + 3)
                traceHeader.CrosslineNumber = traceHeader.TraceNumber =
                    ToInt32(headerBytes, CrosslineNumberLocation - 1, isLittleEndian);

            if (headerBytes.Length >= InlineNumberLocation + 3)
                traceHeader.InlineNumber = ToInt32(headerBytes, InlineNumberLocation - 1, isLittleEndian);

            if (headerBytes.Length >= XLocation + 3)
                traceHeader.X = ToSingle(headerBytes, XLocation - 1, isLittleEndian);
            if (headerBytes.Length >= YLocation + 3)
                traceHeader.Y = ToSingle(headerBytes, YLocation - 1, isLittleEndian);

            if (headerBytes.Length >= _sampleCountIndex + 2)
                traceHeader.SampleCount = ToInt16(headerBytes, _sampleCountIndex, isLittleEndian);

            if (headerBytes.Length >= ShiftLocation + 2)
                traceHeader.Shift = ToInt16(headerBytes, ShiftLocation - 1, isLittleEndian);

            if (headerBytes.Length >= SampleLocation + 2)
                traceHeader.Sample = ToInt16(headerBytes, SampleLocation - 1, isLittleEndian);

            return traceHeader;
        }

        /// <summary>
        /// Assuming the trace header has been read, reads the array of sample values
        /// </summary>
        public virtual IList<float> ReadTrace(BinaryReader reader, FormatCode sampleFormat, int sampleCount, bool isLittleEndian)
        {
            var trace = new float[sampleCount];
            try
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    switch (sampleFormat)
                    {
                        // IbmFloatingPoint4: Вызывается метод ReadSingleIbm() из сборки Unplugged.IbmBits класс BinaryReaderExtensionMethods
                        // Считывает 4 байта методом ReadBytes(reader, 4)
                        // Возвращает преобразованное число типа float из этих 4 байт методом IbmConverter.ToSingle(value)
                        // из namespace Unplugged.IbmBits класс IbmConverter
                        case FormatCode.IbmFloatingPoint4:
                            trace[i] = reader.ReadSingleIbm();
                            break;
                        // IeeeFloatingPoint4: Если формат байтов little-endian, то вызывается стандартный метод ReadSingle класса BinaryReader
                        // если не little-endian, то вызывается метод float ReadReversedSingle(BinaryReader reader) из текущего класса SegyReader
                        // Этот метод считывает 4 байта в массив, переворачивает и подает на вход стандартному методу BitConverter.ToSingle(b, 0);
                        case FormatCode.IeeeFloatingPoint4:
                            trace[i] = isLittleEndian ?
                                reader.ReadSingle() :
                                ReadReversedSingle(reader);
                            break;

                        // TwosComplementInteger1: вызывается метод float ReadSignedByte(BinaryReader reader) из текущего класса SegyReader
                        // Считывает один байт. Если байт меньше 128, то возвращает байт. Если больше 128, то вычитает 256 и возвращает (тк signed)
                        case FormatCode.TwosComplementInteger1:
                            trace[i] = ReadSignedByte(reader);
                            break;

                        //TwosComplementInteger2: Если формат байтов little-endian, то вызывается стандартный метод ReadInt16 класса BinaryReader
                        // если не little-endian, то вызывается метод short ReadInt16BigEndian(this BinaryReader reader) из класса BinaryReaderExtensionMethods из namespace Unplugged.IbmBits
                        // Считывает 2 байта методом ReadBytes(reader, 2)
                        // Возвращает преобразованное число типа short из этих 2 байт методом IbmConverter.ToInt16(value)
                        // из namespace Unplugged.IbmBits класс IbmConverter
                        // Переворачивает массив byte[] и получает число short через стандартный метод BitConverter.ToInt16(value2, 0);
                        case FormatCode.TwosComplementInteger2:
                            trace[i] = isLittleEndian ?
                                reader.ReadInt16() :
                                reader.ReadInt16BigEndian();
                            break;

                        //TwosComplementInteger4: Если формат байтов little-endian, то вызывается стандартный метод ReadInt32 класса BinaryReader
                        // если не little-endian, то вызывается метод short ReadInt32BigEndian(this BinaryReader reader) из класса BinaryReaderExtensionMethods из namespace Unplugged.IbmBits
                        // Считывает 4 байта методом ReadBytes(reader, 4)
                        // Возвращает преобразованное число типа short из этих 4 байт методом IbmConverter.ToInt32(value)
                        // из namespace Unplugged.IbmBits класс IbmConverter
                        // Переворачивает массив byte[] и получает число int через стандартный метод BitConverter.ToInt32(value2, 0);
                        case FormatCode.TwosComplementInteger4:
                            trace[i] = isLittleEndian ?
                                reader.ReadInt32() :
                                reader.ReadInt32BigEndian();
                            break;
                        default:
                            throw new NotSupportedException(
                                String.Format("Unsupported sample format: {0}. Send an email to dev@segy.net to request support for this format.", sampleFormat));
                    }
                }
            }
            catch (EndOfStreamException) { /* Encountered end of stream before end of trace. Leave remaining trace samples as zero */ }
            
            return trace;
        }

        #endregion

        #region Behind the Scenes        

        private string InsertNewLines(string text)
        {
            var rows = Options.TextHeaderRowCount;
            var cols = Options.TextHeaderColumnCount;
            var result = new StringBuilder(text.Length + rows);
            for (int i = 0; i < 1 + text.Length / cols; i++)
            {
                var line = new string(text.Skip(cols * i).Take(cols).ToArray());
                result.AppendLine(line);
            }
            return result.ToString();
        }

        private static int ToInt16(byte[] bytes, int index, bool isLittleEndian)
        {
            return isLittleEndian ?
                BitConverter.ToInt16(bytes, index) :
                IbmConverter.ToInt16(bytes, index);
        }

        private static int ToInt32(byte[] bytes, int index, bool isLittleEndian)
        {
            return isLittleEndian ?
                BitConverter.ToInt32(bytes, index) :
                IbmConverter.ToInt32(bytes, index);
        }

        private static float ToSingle(byte[] bytes, int index, bool isLittleEndian)
        {
            float result;
            if (isLittleEndian) { result = BitConverter.ToSingle(bytes, index); }
            else
            {
                byte[] number = new byte[] { bytes[index + 3], bytes[index + 2], bytes[index + 1], bytes[index + 0] };
                result = BitConverter.ToSingle(number, 0);
            }
            return result;
        }

        private static float ReadSignedByte(BinaryReader reader)
        {
            byte b = reader.ReadByte();
            return b < 128 ? b : b - 256;
        }

        private static float ReadReversedSingle(BinaryReader reader)
        {
            var b = reader.ReadBytes(4).Reverse().ToArray();
            return BitConverter.ToSingle(b, 0);
        }

        public virtual byte[] ReverseBytesCoord(byte[] ish, int loc)
        {
            byte[] b_ret = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                b_ret[i] = ish[loc + 3 - i];
            }
            return b_ret;
        }


        #endregion
    }
}
