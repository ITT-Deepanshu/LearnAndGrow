using TrainWreck.Interfaces;

namespace TrainWreck.Models
{
    public class Customer : IPayable
    {
        private readonly Wallet _wallet;

        public string FirstName { get; }
        public string LastName { get; }
        public string FullName => $"{FirstName} {LastName}";

        public Customer(string firstName, string lastName, decimal initialBalance)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name is required", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name is required", nameof(lastName));

            FirstName = firstName;
            LastName = lastName;
            _wallet = new Wallet(initialBalance);
        }

        public PaymentResult Pay(decimal amount)
        {
            if (!_wallet.HasSufficientFunds(amount))
                return PaymentResult.InsufficientFunds(amount);

            _wallet.Deduct(amount);
            return PaymentResult.Success(amount);
        }
    }
}