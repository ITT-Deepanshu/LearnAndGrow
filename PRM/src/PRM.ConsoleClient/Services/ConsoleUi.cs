using System.Globalization;

namespace PRM.ConsoleClient.Services;

public sealed class ConsoleUi
{
    private const int BoxWidth = 46;

    public void ClearScreen() => Console.Clear();

    public void DrawBox(string title, params string[] subtitleLines)
    {
        var top = $"╔{new string('═', BoxWidth)}╗";
        var bottom = $"╚{new string('═', BoxWidth)}╝";
        Console.WriteLine(top);
        WriteBoxLine(Center(title, BoxWidth));
        foreach (var line in subtitleLines)
            WriteBoxLine(Center(line, BoxWidth));
        Console.WriteLine(bottom);
        Console.WriteLine();
    }

    public void DrawSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"── {title} {new string('─', Math.Max(0, BoxWidth - title.Length - 4))}");
    }

    public void DrawDivider() => Console.WriteLine(new string('─', BoxWidth + 2));

    public void WriteSuccess(string message) => Console.WriteLine($"{message} ✓");

    public void WriteWarning(string message) => Console.WriteLine($"⚠  {message}");

    public void WriteError(string message) => Console.WriteLine($"Error: {message}");

    public string Prompt(string label, bool secret = false)
    {
        Console.Write($"{label}: ");
        if (!secret)
            return Console.ReadLine()?.Trim() ?? string.Empty;

        var password = ReadSecret();
        Console.WriteLine();
        return password;
    }

    public string PromptOptional(string label, string defaultValue = "")
    {
        Console.Write($"{label}: ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }

    public int PromptInt(string label, int? min = null, int? max = null)
    {
        while (true)
        {
            var input = Prompt(label);
            if (!int.TryParse(input, out var value))
            {
                WriteError("Please enter a valid number.");
                continue;
            }

            if (min.HasValue && value < min.Value)
            {
                WriteError($"Value must be at least {min.Value}.");
                continue;
            }

            if (max.HasValue && value > max.Value)
            {
                WriteError($"Value must be at most {max.Value}.");
                continue;
            }

            return value;
        }
    }

    public long PromptLong(string label)
    {
        while (true)
        {
            var input = Prompt(label);
            if (long.TryParse(input, out var value))
                return value;
            WriteError("Please enter a valid number.");
        }
    }

    public decimal PromptDecimal(string label)
    {
        while (true)
        {
            var input = Prompt(label);
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                return value;
            WriteError("Please enter a valid number.");
        }
    }

    public DateOnly? PromptDateOptional(string label)
    {
        var input = PromptOptional(label);
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (TryParseDate(input, out var date))
            return date;

        WriteError("Invalid date. Use DD-MM-YYYY.");
        return PromptDateOptional(label);
    }

    public DateOnly PromptDate(string label)
    {
        while (true)
        {
            var input = Prompt(label);
            if (TryParseDate(input, out var date))
                return date;
            WriteError("Invalid date. Use DD-MM-YYYY.");
        }
    }

    public bool Confirm(string message)
    {
        var input = Prompt($"{message} [Y/N]").ToUpperInvariant();
        return input is "Y" or "YES";
    }

    public void Pause(string message = "Press Enter to continue...")
    {
        Console.WriteLine();
        Console.Write(message);
        Console.ReadLine();
    }

    public void PrintTable(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var rowList = rows.Select(r => r.ToList()).ToList();
        var widths = headers.Select((h, i) =>
            Math.Max(h.Length, rowList.Count == 0 ? 0 : rowList.Max(r => i < r.Count ? r[i].Length : 0))).ToArray();

        PrintRow(headers, widths);
        Console.WriteLine(new string('─', widths.Sum() + (widths.Length - 1) * 2));
        foreach (var row in rowList)
            PrintRow(row, widths);
    }

    public string FormatDate(DateOnly date) => date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    public string FormatDateTime(DateTime dateTime) =>
        dateTime.ToLocalTime().ToString("dd-MM-yyyy  HH:mm", CultureInfo.InvariantCulture);

    public string FormatNow() => FormatDateTime(DateTime.Now);

    public string HealthEmoji(string health) => health.ToUpperInvariant() switch
    {
        "AT_RISK" or "AT RISK" => "🔴 AT RISK",
        "ON_TRACK" or "ON TRACK" => "🟢 ON TRACK",
        "ATTENTION" => "🟡 ATTENTION",
        _ => health
    };

    public DateOnly GetLastMonday(DateOnly? reference = null)
    {
        var date = reference ?? DateOnly.FromDateTime(DateTime.Today);
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    public DateOnly GetPreviousCompletedWeekMonday()
    {
        var currentWeekMonday = GetLastMonday();
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (today > currentWeekMonday.AddDays(6))
            return currentWeekMonday;
        return currentWeekMonday.AddDays(-7);
    }

    public bool TryParseDateInput(string input, out DateOnly date) => TryParseDate(input, out date);

    private static void PrintRow(IReadOnlyList<string> cells, int[] widths)
    {
        for (var i = 0; i < cells.Count; i++)
        {
            if (i > 0) Console.Write("  ");
            Console.Write(cells[i].PadRight(widths[i]));
        }
        Console.WriteLine();
    }

    private static void WriteBoxLine(string content)
    {
        var padded = content.Length > BoxWidth ? content[..BoxWidth] : content.PadRight(BoxWidth);
        Console.WriteLine($"║{padded}║");
    }

    private static string Center(string text, int width)
    {
        if (text.Length >= width)
            return text[..width];
        var pad = (width - text.Length) / 2;
        return new string(' ', pad) + text + new string(' ', width - text.Length - pad);
    }

    private static string ReadSecret()
    {
        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && chars.Count > 0)
            {
                chars.RemoveAt(chars.Count - 1);
                Console.Write("\b \b");
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                chars.Add(key.KeyChar);
                Console.Write('*');
            }
        }

        return new string(chars.ToArray());
    }

    private static bool TryParseDate(string input, out DateOnly date)
    {
        if (DateOnly.TryParseExact(input, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;
        if (DateOnly.TryParseExact(input, "d-M-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;
        return DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
