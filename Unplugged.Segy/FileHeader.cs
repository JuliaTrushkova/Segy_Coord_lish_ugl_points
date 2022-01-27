
namespace Unplugged.Segy
{
    class FileHeader : IFileHeader
    {
        public byte[] TextInByte { get; set; }
        public string Text { get; set; }
        public FormatCode SampleFormat { get; set; }
        public bool IsLittleEndian { get; set; }
    }
}
