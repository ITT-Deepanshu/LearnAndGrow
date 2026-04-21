using Core.Models;

namespace Core.Interfaces
{
    public interface IParser
    {
        Record Parse(string line);
    }
}