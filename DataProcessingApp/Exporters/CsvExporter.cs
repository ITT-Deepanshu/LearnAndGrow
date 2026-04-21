using Core.Interfaces;
using Core.Models;

namespace Infrastructure.Exporters
{
    public class CsvExporter : IExporter
    {
        public void Export(List<Record> records, string filePath)
        {
            var lines = new List<string>
            {
                "Id,Name,Value,Date,DoubledValue,SquaredValue"
            };

            lines.AddRange(records.Select(r =>
                $"{r.Id},{r.Name},{r.Value},{r.Date},{r.DoubledValue},{r.SquaredValue}"
            ));

            File.WriteAllLines(filePath, lines);
        }
    }
}