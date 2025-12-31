using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberGuessingGame
{
    public class UserInterface
    {
        public void DisplayWelcomeMessage()
        {
            Console.WriteLine("=================================");
            Console.WriteLine("Number Guessing Game");
            Console.WriteLine("=================================");
            Console.WriteLine();
        }

        public string GetUserGuess()
        {
            Console.Write("Guess a number between 1 and 100: ");
            return Console.ReadLine() ?? string.Empty;
        }

        public void DisplayInvalidInputMessage(string errorMessage)
        {
            Console.WriteLine($"Invalid input! {errorMessage}");
            Console.WriteLine("This attempt won't be counted.");
            Console.WriteLine();
        }

        public void DisplayFeedback(GuessResult result)
        {
            switch (result)
            {
                case GuessResult.TooLow:
                    Console.WriteLine("Too low. Guess again!");
                    Console.WriteLine();
                    break;
                case GuessResult.TooHigh:
                    Console.WriteLine("Too high. Guess again!");
                    Console.WriteLine();
                    break;
            }
        }

        public void DisplaySuccessMessage(int numberOfGuesses)
        {
            Console.WriteLine();
            Console.WriteLine("=================================");
            Console.WriteLine($"Congratulations!");
            Console.WriteLine($"You guessed it in {numberOfGuesses} guesses!");
            Console.WriteLine("=================================");
        }
    }
}
