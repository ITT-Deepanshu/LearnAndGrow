using Core.Interfaces;
using Core.Models;

namespace Application.Services
{
    public class ParserService : IParser
    {
        public Record Parse(string line)
        {
            var parts = line.Split(',');

            if (parts.Length < 3)
                throw new Exception("Invalid format");

            if (!double.TryParse(parts[2], out double value))
                throw new Exception("Invalid numeric value");

            DateTime? date = null;
            if (parts.Length > 3 && DateTime.TryParse(parts[3], out DateTime parsedDate))
                date = parsedDate;

            return new Record
            {
                Id = parts[0].Trim(),
                Name = parts[1].Trim(),
                Value = value,
                Date = date
            };
        }
    }
}