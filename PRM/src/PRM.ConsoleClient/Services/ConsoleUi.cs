using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace PRM.ConsoleClient.Services;

public sealed partial class ConsoleUi
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
        while (true)
        {
            var input = PromptOptional(label);
            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (TryParseDate(input, out var date))
                return date;

            WriteError("Invalid date. Use DD-MM-YYYY.");
        }
    }

    public DateOnly PromptWeekStartOptional(string label)
    {
        while (true)
        {
            var input = PromptOptional(label);
            if (string.IsNullOrWhiteSpace(input))
                return GetLastMonday();

            if (TryParseDate(input, out var date))
                return date;

            WriteError("Invalid date. Use DD-MM-YYYY.");
        }
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

    public string PromptRequired(string label, int minLength = 1, int maxLength = 256, bool secret = false)
    {
        while (true)
        {
            var input = Prompt(label, secret: secret);
            if (string.IsNullOrWhiteSpace(input))
            {
                WriteError($"{label} is required.");
                continue;
            }

            if (input.Length < minLength)
            {
                WriteError($"{label} must be at least {minLength} characters.");
                continue;
            }

            if (input.Length > maxLength)
            {
                WriteError($"{label} must be at most {maxLength} characters.");
                continue;
            }

            return input;
        }
    }

    public string PromptEmail(string label = "Email")
    {
        while (true)
        {
            var input = Prompt(label);
            if (string.IsNullOrWhiteSpace(input))
            {
                WriteError("Email is required.");
                continue;
            }

            if (!IsValidEmail(input))
            {
                WriteError("Please enter a valid email address (e.g. user@company.com).");
                continue;
            }

            return input;
        }
    }

    public string PromptUsername(string label = "Username")
    {
        while (true)
        {
            var input = Prompt(label);
            if (string.IsNullOrWhiteSpace(input))
            {
                WriteError("Username is required.");
                continue;
            }

            if (input.Length < 3)
            {
                WriteError("Username must be at least 3 characters.");
                continue;
            }

            if (input.Length > 64)
            {
                WriteError("Username must be at most 64 characters.");
                continue;
            }

            if (!UsernameRegex().IsMatch(input))
            {
                WriteError("Username must contain only lowercase letters, digits, and .-_ characters.");
                continue;
            }

            return input;
        }
    }

    public string PromptPassword(string label = "Password")
    {
        while (true)
        {
            var input = Prompt(label, secret: true);
            var error = ValidatePassword(input);
            if (error is not null)
            {
                WriteError(error);
                continue;
            }

            return input;
        }
    }

    public string PromptConfirmPassword(string password, string label = "Confirm Password")
    {
        while (true)
        {
            var input = Prompt(label, secret: true);
            if (!string.Equals(input, password, StringComparison.Ordinal))
            {
                WriteError("Passwords do not match. Please try again.");
                continue;
            }

            return input;
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
        "GREEN" => "🟢 Green",
        "YELLOW" => "🟡 Yellow",
        "RED" => "🔴 Red",
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

    private static bool IsValidEmail(string input)
    {
        try
        {
            _ = new MailAddress(input);
            return input.Contains('@', StringComparison.Ordinal)
                && input.Contains('.', StringComparison.Ordinal)
                && !input.EndsWith('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? ValidatePassword(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Password is required.";
        if (input.Length < 8)
            return "Password must be at least 8 characters.";
        if (!input.Any(char.IsUpper))
            return "Password must contain an uppercase letter.";
        if (!input.Any(char.IsDigit))
            return "Password must contain a digit.";
        return null;
    }

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex UsernameRegex();
}
