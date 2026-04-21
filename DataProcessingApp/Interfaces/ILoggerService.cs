namespace Core.Interfaces
{
    public interface ILoggerService
    {
        void Log(string message);
        void Save(string path);
    }
}