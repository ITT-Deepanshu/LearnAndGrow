namespace GeoLocatorApp.Utils
{
    public static class ConsoleHelper
    {
        public static void PrintHeader()
        {
            Console.WriteLine("===================================");
            Console.WriteLine("   GEO LOCATION FINDER APP");
            Console.WriteLine("===================================");
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
            Console.ResetColor();
        }

        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
