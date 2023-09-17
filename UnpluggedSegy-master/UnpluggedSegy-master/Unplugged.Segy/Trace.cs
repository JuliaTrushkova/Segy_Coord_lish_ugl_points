using System.Collections.Generic;

namespace Unplugged.Segy
{
    class Trace : ITrace
    {
        // Header - Заголовок трассы - первые 240 байт в блоке трассы
        // Values - значения амплитуд в трассе
        // TraceInByte - запись трассы в байтах
        public ITraceHeader Header { get; set; }
        public IList<float> Values { get; set; }
        public byte[] TraceInByte { get; set; }


    }
}
