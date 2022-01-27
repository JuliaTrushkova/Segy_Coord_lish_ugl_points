
namespace Unplugged.Segy
{
    class TraceHeader : ITraceHeader
    {
        public byte[] TextHeader { get; set; }        
        public int SampleCount { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int TraceNumber { get; set; }
        public int InlineNumber { get; set; }
        public int CrosslineNumber { get; set; }
    }
}
