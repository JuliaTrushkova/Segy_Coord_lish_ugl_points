using System.Collections.Generic;

namespace Unplugged.Segy
{
    class SegyFile : ISegyFile
    {
        //Header - заголовок файла (первые 3600 байтов)
        //Traces - коллекция трасс
        //FileInByte - файл segy в байтовом формате (3200 байт - текстовый заголовок, 400 байт - бинарный заголовок.
        //Далее последовательно данные трасс: 240 байт - заголовок трассы, N байт - амплитуды в каждом отсчете
        //(количество байт задается длинной трассы)

        public IFileHeader Header { get; set; }
        public IList<ITrace> Traces { get; set; }
        public byte[] FileInByte { get; set; }
    }
}
