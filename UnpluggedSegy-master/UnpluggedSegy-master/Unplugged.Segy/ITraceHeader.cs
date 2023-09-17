
namespace Unplugged.Segy
{
    public interface ITraceHeader
    {
        // TextHeader - заголовок трассы в байтах (240 байт)
        // 1–4 - TraceNumber - Trace sequence number within line — Numbers continue to increase if the same line continues across multiple SEG-Y files. 
        // 5–8 - Trace sequence number within SEG-Y file — Each file starts with trace sequence one.
        // 17–20 - Energy source point number — Used when more than one record occurs at the same effective surface location.
        // It is recommended that the new entry defined in Trace Header bytes 197–202 be used for shotpoint number.
        // 21–24 - Ensemble number (i.e. CDP, CMP, CRP)
        // 25–28 - Trace number within the ensemble — Each ensemble starts with trace number one.
        // 71–72 - Scalar to be applied to all coordinates specified in Standard Trace Header bytes 73–88 and to bytes Trace Header 181–188 to give the real value.
        // Scalar = 1, ±10, ±100, ±1000, or ±10,000. If positive, scalar is used as a multiplier; if negative, scalar is used as divisor.
        // A value of zero is assumed to be a scalar value of 1.
        // 73–76 - Source coordinate – X
        // 77–80 - Source coordinate – Y.
        // 81–84 - Group coordinate – X.
        // 85–88 - Group coordinate – Y
        // 103–104 - Total static applied in milliseconds. (Zero if no static has been applied)
        // 105–106 - в эти байты Petrel записывает примененный shift к segy. - Lag time A — Time in milliseconds between end of 240-byte trace identification header and time break.
        // The value is positive if time break occurs after the end of header; negative if time break occurs before the end of header.
        // Time break is defined as the initiation pulse that may be recorded on an auxiliary trace or as otherwise specified by the recording system.
        // 107–108 - Lag Time B — Time in milliseconds between time break and the initiation time of the energy source.May be positive or negative.
        // 109–110 - Delay recording time — Time in milliseconds between initiation time of energy source and the time when recording of data samples begins.
        // In SEG-Y rev 0 this entry was intended for deep-water work if data recording did not start at zero time. The entry can be negative to accommodate
        // negative start times (i.e.data recorded before time zero, presumably as a result of static application to the data trace).
        // If a non-zero value(negative or positive) is recorded in this entry, a comment to that effect should appear in the Textual File Header
        // 115–116 - SampleCount - Number of samples in this trace.
        // 117–118 - Sample interval for this trace
        // 181–184 - X coordinate of ensemble (CDP) position of this trace (scalar in Standard Trace Header bytes 71–72 applies).
        // 185–188 - Y coordinate of ensemble (CDP) position of this trace (scalar in Standard Trace Header bytes 71–72 applies).
        // 189–192 - For 3-D poststack data, this field should be used for the in-line number.
        // If one in-line per SEG-Y file is being recorded, this value should be the same for all traces in the file and the same value will be recorded
        // in bytes 3205–3208 of the Binary File Header.
        // 193–196 - For 3-D poststack data, this field should be used for the cross-line number. This will typically be the same value as the ensemble(CDP)
        // number in Standard Trace Header bytes 21–24, but this does not have to be the case.        // 197–200 - Shotpoint number
        // 205–210 - Transduction Constant

        byte[] TextHeader { get; set; }
        int SampleCount { get; }
        int TraceNumber { get; }
        int InlineNumber { get; }
        int CrosslineNumber { get; }
        //int SampleIntervalInMicroseconds { get; }
        float X { get; set; }
        float Y { get; set; }
        int Shift { get; set; }

        int Sample { get; }
    }
}
