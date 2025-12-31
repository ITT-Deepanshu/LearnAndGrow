using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberGuessingGame
{
    public class GameEngine
    {
        private readonly int _targetNumber;
        private int _numberOfGuesses;

        public int NumberOfGuesses => _numberOfGuesses;

        public GameEngine()
        {
            var random = new Random();
            _targetNumber = random.Next(1, 101); 
            _numberOfGuesses = 0;
        }

        public GuessResult ProcessGuess(int guess)
        {
            _numberOfGuesses++;

            if (guess < _targetNumber)
                return GuessResult.TooLow;

            if (guess > _targetNumber)
                return GuessResult.TooHigh;

            return GuessResult.Correct;
        }
    }

    public enum GuessResult
    {
        TooLow,
        TooHigh,
        Correct
    }
}
