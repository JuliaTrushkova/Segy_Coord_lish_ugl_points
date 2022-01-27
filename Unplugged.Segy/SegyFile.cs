using System.Collections.Generic;

namespace Unplugged.Segy
{
    class SegyFile : ISegyFile
    {
        public IFileHeader Header { get; set; }
        public IList<ITrace> Traces { get; set; }
        public byte[] FileInByte { get; set; }
    }
}
