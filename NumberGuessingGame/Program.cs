namespace NumberGuessingGame
{
    public class Program
    {
        static void Main(string[] args)
        {
            var game = new GameApplication();
            game.Run();
        }
    }

    public class GameApplication
    {
        private readonly GameEngine _gameEngine;
        private readonly InputValidator _inputValidator;
        private readonly UserInterface _userInterface;

        public GameApplication()
        {
            _gameEngine = new GameEngine();
            _inputValidator = new InputValidator();
            _userInterface = new UserInterface();
        }

        public void Run()
        {
            _userInterface.DisplayWelcomeMessage();

            bool isGameWon = false;

            while (!isGameWon)
            {
                string userInput = _userInterface.GetUserGuess();

                if (!_inputValidator.IsValidGuess(userInput, out int guess))
                {
                    string errorMessage = _inputValidator.GetValidationErrorMessage();
                    _userInterface.DisplayInvalidInputMessage(errorMessage);
                    continue;
                }

                GuessResult result = _gameEngine.ProcessGuess(guess);

                if (result == GuessResult.Correct)
                {
                    _userInterface.DisplaySuccessMessage(_gameEngine.NumberOfGuesses);
                    isGameWon = true;
                }
                else
                {
                    _userInterface.DisplayFeedback(result);
                }
            }
        }
    }
}