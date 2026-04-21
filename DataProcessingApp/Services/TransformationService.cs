using Core.Interfaces;
using Core.Models;

namespace Application.Services
{
    public class TransformationService : ITransformer
    {
        public void Transform(Record record)
        {
            record.Name = record.Name.ToUpper();
        }
    }
}