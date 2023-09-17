
namespace Unplugged.Segy
{
	public interface IReadingProgress
	{
		// Интерфейс для класса 
		void ReportProgress(int progressPercentage);
		bool CancellationPending { get; }
	}
}

