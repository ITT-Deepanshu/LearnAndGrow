namespace TrainWreck.Models
{
    internal class Wallet
    {
        private decimal _balance;

        public Wallet(decimal initialBalance)
        {
            if (initialBalance < 0)
                throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));

            _balance = initialBalance;
        }

        public bool HasSufficientFunds(decimal amount) => _balance >= amount;

        public void Deduct(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Deduction amount must be positive", nameof(amount));

            if (!HasSufficientFunds(amount))
                throw new InvalidOperationException("Insufficient funds in wallet");

            _balance -= amount;
        }
    }
}