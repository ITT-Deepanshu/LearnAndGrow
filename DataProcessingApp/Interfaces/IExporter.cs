using Core.Models;

namespace Core.Interfaces
{
    public interface IExporter
    {
        void Export(List<Record> records, string filePath);
    }
}