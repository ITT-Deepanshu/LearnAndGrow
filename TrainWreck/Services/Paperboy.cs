using TrainWreck.Interfaces;
using TrainWreck.Models;

namespace TrainWreck.Services
{
    public class Paperboy
    {
        private readonly string _name;

        public Paperboy(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Paperboy name is required.", nameof(name));

            _name = name;
        }

        public void CollectPayment(IPayable customer, decimal paymentAmount)
        {
            if (paymentAmount <= 0)
                throw new ArgumentException("Payment amount must be positive.", nameof(paymentAmount));

            Console.WriteLine($"\n{_name} -> Requesting {paymentAmount:C} from {customer.FullName}");

            PaymentResult result = customer.Pay(paymentAmount);

            Console.WriteLine(result.IsSuccess
                ? $"{result.Message}"
                : $"{result.Message}");
        }
    }
}