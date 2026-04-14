
using DivisorPairs.Core.Interfaces;
using DivisorPairs.Core.Services;

namespace DivisorPairs.App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IDivisorPairAnalyzer analyzer = new DivisorPairAnalyzer();

            int testCases = ReadInteger("Enter number of test cases:");

            for (int i = 0; i < testCases; i++)
            {
                int input = ReadInteger("Enter value of Upper Limit:");

                int result = analyzer.GetEqualDivisorAdjacentCount(input);

                Console.WriteLine(result);
            }
        }

        private static int ReadInteger(string message)
        {
            Console.WriteLine(message);
            return int.Parse(Console.ReadLine() ?? "0");
        }
    }
}