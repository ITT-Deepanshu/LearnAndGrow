namespace GeoLocatorApp.Validators
{
    public static class InputValidator
    {
        public static bool IsValid(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && input.Length >= 2;
        }
    }
}
