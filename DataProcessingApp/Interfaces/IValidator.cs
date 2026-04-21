using Core.Models;

namespace Core.Interfaces
{
    public interface IValidator
    {
        bool Validate(Record record);
    }
}