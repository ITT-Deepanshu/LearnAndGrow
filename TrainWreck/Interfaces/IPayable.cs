using TrainWreck.Models;

namespace TrainWreck.Interfaces
{
    public interface IPayable
    {
        string FullName { get; }
        PaymentResult Pay(decimal amount);
    }
}