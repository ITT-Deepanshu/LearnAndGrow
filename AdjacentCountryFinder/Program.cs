using AdjacentCountryFinder;
using System.Text.Json;

class Program
{
    static void Main()
    {
        try
        {
            string workingDirectory = Environment.CurrentDirectory;
            var jsonPath = Path.Combine(Directory.GetParent(workingDirectory).Parent.Parent.FullName, "data", "countries_detail.json");
            var json = File.ReadAllText(jsonPath);
            var countries = JsonSerializer.Deserialize<Dictionary<string, CountryDetail>>(json);
                           
            Console.Write("Enter country code (e.g. IN / US / NZ): ");
            var input = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("No input provided. Exiting.");
                return;
            }

            if (!countries.TryGetValue(input, out var country))
            {
                Console.WriteLine($"Country code '{input}' not found in data.");
                return;
            }

            Console.WriteLine($"Country: {country.Name}");
            if (country.Neighbors == null || country.Neighbors.Count == 0)
            {
                Console.WriteLine("Adjacent countries: (none)");
                return;
            }

            Console.WriteLine("Adjacent countries:");
            foreach (var neighbor in country.Neighbors)
            {
                Console.WriteLine($"- {neighbor}");
            }
            return;

        }
        catch (JsonException jsonEx)
        {
            Console.Error.WriteLine("Failed to parse JSON: " + jsonEx.Message);
            return;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unexpected error: " + ex.Message);
            return;
        }
    }
}
