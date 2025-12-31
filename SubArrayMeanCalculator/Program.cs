using System;

namespace SubarrayMeanCalculator
{
    class Program
    {
        static void Main()
        {
            ConsoleUI.ShowWelcome();

            try
            {
                InputData input = InputReader.ReadInput();
                long[] prefixSum = PrefixSumCalculator.BuildPrefixSum(input.Array);

                ConsoleUI.ShowResultHeader();
                QueryProcessor.ProcessQueries(
                    prefixSum,
                    input.Queries
                );
            }
            catch (Exception ex)
            {
                ConsoleUI.ShowError(ex.Message);
            }
        }
    }

    public class InputData
    {
        public long[] Array { get; set; }
        public (int Left, int Right)[] Queries { get; set; }
    }

    public static class ConsoleUI
    {
        public static void ShowWelcome()
        {
            Console.WriteLine("======================================");
            Console.WriteLine(" Subarray Mean (Floor) Calculator ");
            Console.WriteLine("======================================");
            Console.WriteLine();
        }

        public static void PromptForNQ()
        {
            Console.WriteLine("Enter number of array elements (N) and queries (Q):");
            Console.WriteLine("Format: N Q");
        }

        public static void PromptForArray(int n)
        {
            Console.WriteLine();
            Console.WriteLine($"Enter {n} space-separated integers for the array:");
        }

        public static void PromptForQuery(int index)
        {
            Console.WriteLine($"Enter query {index + 1} (L R):");
        }

        public static void ShowResultHeader()
        {
            Console.WriteLine();
            Console.WriteLine("Results (Floor of Mean for each query):");
            Console.WriteLine("--------------------------------------");
        }

        public static void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
            Console.ResetColor();
        }
    }

    public static class InputReader
    {
        public static InputData ReadInput()
        {
            ConsoleUI.PromptForNQ();
            var firstLine = ReadAndSplit(2);

            int n = ParsePositiveInt(firstLine[0], "N");
            int q = ParsePositiveInt(firstLine[1], "Q");

            ConsoleUI.PromptForArray(n);
            long[] array = ReadLongArray(n);

            var queries = ReadQueries(q, n);

            return new InputData
            {
                Array = array,
                Queries = queries
            };
        }

        private static long[] ReadLongArray(int expectedLength)
        {
            var parts = ReadAndSplit(expectedLength);
            long[] array = new long[expectedLength];

            for (int i = 0; i < expectedLength; i++)
                array[i] = long.Parse(parts[i]);

            return array;
        }

        private static (int, int)[] ReadQueries(int q, int n)
        {
            var queries = new (int, int)[q];

            for (int i = 0; i < q; i++)
            {
                ConsoleUI.PromptForQuery(i);
                var parts = ReadAndSplit(2);

                int left = int.Parse(parts[0]);
                int right = int.Parse(parts[1]);

                if (left < 1 || right > n || left > right)
                    throw new ArgumentException($"Invalid query range: {left} {right}");

                queries[i] = (left, right);
            }

            return queries;
        }

        private static string[] ReadAndSplit(int expectedCount)
        {
            var input = Console.ReadLine()?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (input == null || input.Length != expectedCount)
                throw new ArgumentException("Invalid input format.");

            return input;
        }

        private static int ParsePositiveInt(string value, string name)
        {
            int result = int.Parse(value);
            if (result <= 0)
                throw new ArgumentException($"{name} must be a positive integer.");
            return result;
        }
    }

    public static class PrefixSumCalculator
    {
        public static long[] BuildPrefixSum(long[] array)
        {
            long[] prefixSum = new long[array.Length + 1];

            for (int i = 1; i <= array.Length; i++)
                prefixSum[i] = prefixSum[i - 1] + array[i - 1];

            return prefixSum;
        }
    }

    public static class QueryProcessor
    {
        public static void ProcessQueries(
            long[] prefixSum,
            (int Left, int Right)[] queries)
        {
            foreach (var query in queries)
            {
                long sum = prefixSum[query.Right] - prefixSum[query.Left - 1];
                int count = query.Right - query.Left + 1;

                long meanFloor = sum / count;
                Console.WriteLine(meanFloor);
            }
        }
    }

}
