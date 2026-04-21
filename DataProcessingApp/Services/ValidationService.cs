using Core.Interfaces;
using Core.Models;

namespace Application.Services
{
    public class ValidationService : IValidator
    {
        public bool Validate(Record record)
        {
            return !string.IsNullOrWhiteSpace(record.Id)
                && !string.IsNullOrWhiteSpace(record.Name)
                && record.Value >= 0;
        }
    }
}