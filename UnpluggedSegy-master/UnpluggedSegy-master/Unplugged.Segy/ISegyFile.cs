using System.Collections.Generic;

namespace Unplugged.Segy
{
    public interface ISegyFile
    {
        //Каждый segy файл содержит заголовок файла, набор трасс и запись файла в байтах
        IFileHeader Header { get; }
        IList<ITrace> Traces { get; }
        byte[] FileInByte { get; set; }

       


    }
}
