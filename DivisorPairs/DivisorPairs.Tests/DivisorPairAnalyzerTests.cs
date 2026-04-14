using DivisorPairs.Core.Services;

namespace DivisorPairs.Tests
{
    public class DivisorPairAnalyzerTests
    {
        private readonly DivisorPairAnalyzer _analyzer;

        public DivisorPairAnalyzerTests()
        {
            _analyzer = new DivisorPairAnalyzer();
        }

        [Fact]
        public void Should_Return_Correct_Count_For_K_15()
        {
            int result = _analyzer.GetEqualDivisorAdjacentCount(15);

            Assert.Equal(2, result);
        }

        [Theory]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(4, 1)]
        [InlineData(10, 1)]
        public void Should_Handle_Small_Inputs(int input, int expected)
        {
            int result = _analyzer.GetEqualDivisorAdjacentCount(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Should_Return_Zero_For_Negative_Input()
        {
            int result = _analyzer.GetEqualDivisorAdjacentCount(-5);

            Assert.Equal(0, result);
        }

        [Fact]
        public void Should_Work_For_Large_Input()
        {
            int result = _analyzer.GetEqualDivisorAdjacentCount(1000);

            Assert.True(result >= 0);
        }

        [Fact]
        public void Should_Not_Throw_Exception_For_Zero()
        {
            int result = _analyzer.GetEqualDivisorAdjacentCount(0);

            Assert.Equal(0, result);
        }
    }
}