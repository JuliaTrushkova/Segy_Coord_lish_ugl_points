
namespace Unplugged.Segy
{
    public interface ISegyOptions
    {
        //Заполнен заголвок EBCDIC или нет
        bool? IsEbcdic { get; }
        //bool? IsLittleEndian { get; }

        //Количество столбцов и строк в тексовом заголовке
        int TextHeaderColumnCount { get; }
        int TextHeaderRowCount { get; }

        //Можно ли добавлять новые строки
        bool TextHeaderInsertNewLines { get; }

        //int BinaryHeaderLength { get; }
        //int BinaryHeaderLocationForSampleFormat { get; }

        //int TraceHeaderLength { get; }
        //int TraceHeaderLocationForInlineNumber { get; }
        //int TraceHeaderLocationForCrosslineNumber { get; }
        //int TraceHeaderLocationForSampleCount { get; }
    }
}
