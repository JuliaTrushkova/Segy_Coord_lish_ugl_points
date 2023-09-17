using System.Collections.Generic;

namespace Unplugged.Segy
{
    public interface ITrace
    {
        // Header - Заголовок трассы - первые 240 байт в блоке трассы
        // Values - значения амплитуд в трассе
        ITraceHeader Header { get; }
        IList<float> Values { get; set; }
    }
}
