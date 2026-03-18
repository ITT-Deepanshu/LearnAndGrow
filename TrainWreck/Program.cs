
using TrainWreck.Models;
using TrainWreck.Services;

namespace TrainWreck
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var paperboy = new Paperboy("Tom");

            var alice = new Customer("Alice", "Johnson", initialBalance: 50.00m);
            paperboy.CollectPayment(alice, paymentAmount: 20.00m);

            var bob = new Customer("Bob", "Smith", initialBalance: 5.00m);
            paperboy.CollectPayment(bob, paymentAmount: 20.00m);

            var carol = new Customer("Carol", "White", initialBalance: 20.00m);
            paperboy.CollectPayment(carol, paymentAmount: 20.00m);

            Console.WriteLine("\nSequential Payments for Dave");
            var dave = new Customer("Dave", "Brown", initialBalance: 35.00m);
            paperboy.CollectPayment(dave, paymentAmount: 15.00m);
            paperboy.CollectPayment(dave, paymentAmount: 15.00m);
            paperboy.CollectPayment(dave, paymentAmount: 15.00m);
        }
    }
}