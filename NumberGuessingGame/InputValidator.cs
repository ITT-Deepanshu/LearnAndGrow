using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberGuessingGame
{
    public class InputValidator
    {
        private const int MinimumValue = 1;
        private const int MaximumValue = 100;

        public bool IsValidGuess(string input, out int parsedValue)
        {
            parsedValue = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!int.TryParse(input, out parsedValue))
                return false;

            return parsedValue >= MinimumValue && parsedValue <= MaximumValue;
        }

        public string GetValidationErrorMessage()
        {
            return $"Please enter a valid number between {MinimumValue} and {MaximumValue}.";
        }
    }
}
