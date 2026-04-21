using Core.Interfaces;
using System.Text;

namespace Infrastructure.Logging
{
    public class FileLogger : ILoggerService
    {
        private readonly StringBuilder _logs = new();

        public void Log(string message)
        {
            _logs.AppendLine($"{DateTime.Now}: {message}");
        }

        public void Save(string path)
        {
            File.WriteAllText(path, _logs.ToString());
        }
    }
}