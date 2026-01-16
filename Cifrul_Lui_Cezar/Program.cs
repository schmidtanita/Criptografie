using System.Text;

const int Shift = 3;

Console.WriteLine("Cifrul lui Cezar (+3)");
Console.WriteLine("1) Criptare");
Console.WriteLine("2) Decriptare");
Console.WriteLine("3) Criptanaliza");
Console.Write("Optiune: ");

var choice = Console.ReadLine()?.Trim();
Console.Write("Text: ");
var input = Console.ReadLine() ?? string.Empty;

switch (choice)
{
    case "1":
        Console.WriteLine(ShiftText(input, Shift));
        break;
    case "2":
        Console.WriteLine(ShiftText(input, -Shift));
        break;
    case "3":
        for (var n = 0; n < 26; n++)
        {
            Console.WriteLine($"{n,2}: {ShiftText(input, -n)}");
        }
        break;
    default:
        Console.WriteLine("Optiune invalida.");
        break;
}

static string ShiftText(string text, int shift)
{
    var sb = new StringBuilder(text.Length);
    foreach (var ch in text)
    {
        if (ch is >= 'A' and <= 'Z')
        {
            sb.Append((char)('A' + (ch - 'A' + shift + 26) % 26));
        }
        else if (ch is >= 'a' and <= 'z')
        {
            sb.Append((char)('a' + (ch - 'a' + shift + 26) % 26));
        }
        else
        {
            sb.Append(ch);
        }
    }
    return sb.ToString();
}
