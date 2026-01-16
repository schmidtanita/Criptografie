using System.Text;

const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
const string EnglishFreqOrder = "ETAOINSHRDLCUMWFGYPBVKJXQZ";

Console.WriteLine("Substitutie monoalfabetica");
Console.WriteLine("1) Criptare");
Console.WriteLine("2) Decriptare");
Console.WriteLine("3) Criptanaliza (frecventa litere)");
Console.Write("Optiune: ");

var choice = Console.ReadLine()?.Trim();
Console.Write("Text: ");
var input = Console.ReadLine() ?? string.Empty;

switch (choice)
{
    case "1":
        Console.Write("Cheie (permutare A-Z): ");
        var keyEnc = ReadKey();
        Console.WriteLine(Transform(input, keyEnc));
        break;
    case "2":
        Console.Write("Cheie (permutare A-Z): ");
        var keyDec = ReadKey();
        var invKey = InvertKey(keyDec);
        Console.WriteLine(Transform(input, invKey));
        break;
    case "3":
        var guessedKey = GuessKeyByFrequency(input);
        Console.WriteLine($"Cheie estimata (A-Z ->): {guessedKey}");
        Console.WriteLine("Text estimat:");
        Console.WriteLine(Transform(input, InvertKey(guessedKey)));
        break;
    default:
        Console.WriteLine("Optiune invalida.");
        break;
}

static string ReadKey()
{
    var raw = Console.ReadLine() ?? string.Empty;
    var key = new string(raw.Where(char.IsLetter).Select(char.ToUpperInvariant).ToArray());
    if (key.Length != 26 || key.Distinct().Count() != 26 || key.Any(ch => ch < 'A' || ch > 'Z'))
    {
        Console.WriteLine("Cheie invalida. Trebuie 26 de litere unice A-Z.");
        Environment.Exit(1);
    }
    return key;
}

static string Transform(string text, string key)
{
    var sb = new StringBuilder(text.Length);
    foreach (var ch in text)
    {
        if (!char.IsLetter(ch))
        {
            sb.Append(ch);
            continue;
        }

        var upper = char.ToUpperInvariant(ch);
        var mapped = key[upper - 'A'];
        sb.Append(char.IsUpper(ch) ? mapped : char.ToLowerInvariant(mapped));
    }
    return sb.ToString();
}

static string InvertKey(string key)
{
    var inv = new char[26];
    for (var i = 0; i < 26; i++)
    {
        inv[key[i] - 'A'] = (char)('A' + i);
    }
    return new string(inv);
}

static string GuessKeyByFrequency(string cipherText)
{
    var counts = new int[26];
    foreach (var ch in cipherText)
    {
        if (!char.IsLetter(ch)) continue;
        counts[char.ToUpperInvariant(ch) - 'A']++;
    }

    var orderedCipher = Alphabet
        .OrderByDescending(ch => counts[ch - 'A'])
        .ThenBy(ch => ch)
        .ToArray();

    var key = new char[26];
    for (var i = 0; i < 26; i++)
    {
        var plain = EnglishFreqOrder[i];
        var cipher = orderedCipher[i];
        key[plain - 'A'] = cipher;
    }
    return new string(key);
}
