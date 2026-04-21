using Core.Interfaces;
using Core.Models;
using System.Text.Json;

namespace Infrastructure.Exporters
{
    public class JsonExporter : IExporter
    {
        public void Export(List<Record> records, string filePath)
        {
            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
    }
}