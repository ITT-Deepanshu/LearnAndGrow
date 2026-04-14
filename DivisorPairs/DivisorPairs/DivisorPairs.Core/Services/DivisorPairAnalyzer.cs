using DivisorPairs.Core.Interfaces;

namespace DivisorPairs.Core.Services
{
    public class DivisorPairAnalyzer : IDivisorPairAnalyzer
    {
        public int GetEqualDivisorAdjacentCount(int upperLimit)
        {
            if (upperLimit <= 2)
                return 0;

            var divisorFrequency = BuildDivisorFrequency(upperLimit);

            return CountMatchingAdjacentPairs(divisorFrequency, upperLimit);
        }

        private int[] BuildDivisorFrequency(int limit)
        {
            var frequency = new int[limit + 1];

            for (int divisor = 1; divisor <= limit; divisor++)
            {
                for (int multiple = divisor; multiple <= limit; multiple += divisor)
                {
                    frequency[multiple]++;
                }
            }

            return frequency;
        }

        private int CountMatchingAdjacentPairs(int[] divisorFrequency, int limit)
        {
            int matchCount = 0;

            for (int number = 2; number < limit; number++)
            {
                if (divisorFrequency[number] == divisorFrequency[number + 1])
                {
                    matchCount++;
                }
            }

            return matchCount;
        }
    }
}