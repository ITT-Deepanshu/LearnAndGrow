using Core.Interfaces;
using Core.Models;

namespace Application.Services
{
    public class DataProcessorService
    {
        private readonly IParser _parser;
        private readonly IValidator _validator;
        private readonly ITransformer _transformer;
        private readonly ILoggerService _logger;

        public DataProcessorService(
            IParser parser,
            IValidator validator,
            ITransformer transformer,
            ILoggerService logger)
        {
            _parser = parser;
            _validator = validator;
            _transformer = transformer;
            _logger = logger;
        }

        public List<Record> Process(List<string> lines)
        {
            var records = new List<Record>();

            foreach (var line in lines)
            {
                try
                {
                    var record = _parser.Parse(line);

                    if (!_validator.Validate(record))
                    {
                        _logger.Log("Invalid record skipped");
                        continue;
                    }
                    _transformer.Transform(record);
                    records.Add(record);
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error: {ex.Message}");
                }
            }

            return records;
        }
    }
}