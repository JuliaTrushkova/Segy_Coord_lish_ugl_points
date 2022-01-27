
namespace Unplugged.Segy
{
    public interface ITraceHeader
    {
        byte[] TextHeader { get; set; }
        int SampleCount { get; }
        int TraceNumber { get; }
        int InlineNumber { get; }
        int CrosslineNumber { get; }
        //int SampleIntervalInMicroseconds { get; }
        float X { get; set; }
        float Y { get; set; }
    }
}
