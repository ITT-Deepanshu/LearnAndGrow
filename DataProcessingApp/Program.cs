using Application.Services;
using Core.Interfaces;
using Infrastructure.Exporters;
using Infrastructure.Logging;

class Program
{
    static void Main()
    {
        try
        {
            IParser parser = new ParserService();
            IValidator validator = new ValidationService();
            ITransformer transformer = new TransformationService();
            ILoggerService logger = new FileLogger();

            var processor = new DataProcessorService(parser, validator, transformer, logger);

            var inputPath = "input.csv";
            var lines = File.ReadAllLines(inputPath).ToList();

            var records = processor.Process(lines);

            Console.WriteLine($"Processed Records: {records.Count}");

            IExporter jsonExporter = new JsonExporter();
            IExporter xmlExporter = new XmlExporter();
            IExporter csvExporter = new CsvExporter();

            jsonExporter.Export(records, "output.json");
            xmlExporter.Export(records, "output.xml");
            csvExporter.Export(records, "output.csv");

            logger.Save("logs.txt");

            Console.WriteLine("Processing completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal Error: {ex.Message}");
        }
    }
}