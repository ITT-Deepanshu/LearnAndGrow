using Core.Interfaces;
using Core.Models;
using System.Xml.Serialization;

namespace Infrastructure.Exporters
{
    public class XmlExporter : IExporter
    {
        public void Export(List<Record> records, string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<Record>));

            using var writer = new StreamWriter(filePath);
            serializer.Serialize(writer, records);
        }
    }
}