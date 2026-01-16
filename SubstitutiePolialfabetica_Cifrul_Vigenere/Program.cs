using System.Text;

const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

Console.WriteLine("Cifrul Vigenere (substitutie polialfabetica, n=3)");
Console.WriteLine("1) Criptare");
Console.WriteLine("2) Decriptare");
Console.Write("Optiune: ");

var choice = Console.ReadLine()?.Trim();
var input = ReadInputText();
var keys = ReadKeys();

switch (choice)
{
    case "1":
        Console.WriteLine(Transform(input, keys, true));
        break;
    case "2":
        Console.WriteLine(Transform(input, keys, false));
        break;
    default:
        Console.WriteLine("Optiune invalida.");
        break;
}

static string ReadInputText()
{
    Console.WriteLine("Sursa text:");
    Console.WriteLine("1) Consola");
    Console.WriteLine("2) Fisier (input.txt)");
    Console.Write("Optiune: ");
    var source = Console.ReadLine()?.Trim();
    if (source == "2")
    {
        const string path = "input.txt";
        if (!File.Exists(path))
        {
            Console.WriteLine("Fisier inexistent: input.txt");
            Environment.Exit(1);
        }

        return File.ReadAllText(path);
    }

    Console.Write("Text: ");
    return Console.ReadLine() ?? string.Empty;
}

static string[] ReadKeys()
{
    Console.WriteLine("Sursa chei:");
    Console.WriteLine("1) Consola");
    Console.WriteLine("2) Fisier (keys.txt)");
    Console.Write("Optiune: ");
    var source = Console.ReadLine()?.Trim();
    if (source == "1")
    {
        return ReadKeysFromConsole();
    }

    return ReadKeysFromFile();
}

static string[] ReadKeysFromConsole()
{
    var keys = new string[3];
    for (var i = 0; i < keys.Length; i++)
    {
        Console.Write($"Cheie {i + 1} (permutare A-Z): ");
        keys[i] = ReadKey();
    }
    return keys;
}

static string ReadKey()
{
    var raw = Console.ReadLine() ?? string.Empty;
    var key = new string(raw.Where(char.IsLetter).Select(char.ToUpperInvariant).ToArray());
    ValidateKey(key);
    return key;
}

static string[] ReadKeysFromFile()
{
    const string path = "keys.txt";
    if (!File.Exists(path))
    {
        Console.WriteLine("Fisier inexistent: keys.txt");
        Environment.Exit(1);
    }

    var keys = new List<string>();
    foreach (var line in File.ReadLines(path))
    {
        var key = new string(line.Where(char.IsLetter).Select(char.ToUpperInvariant).ToArray());
        if (key.Length == 0) continue;
        keys.Add(key);
    }

    if (keys.Count != 3)
    {
        Console.WriteLine("Fisierul keys.txt trebuie sa contina exact 3 chei.");
        Environment.Exit(1);
    }

    foreach (var key in keys)
    {
        ValidateKey(key);
    }

    return keys.ToArray();
}

static void ValidateKey(string key)
{
    if (key.Length != 26 || key.Distinct().Count() != 26 || key.Any(ch => ch < 'A' || ch > 'Z'))
    {
        Console.WriteLine("Cheie invalida. Trebuie 26 de litere unice A-Z.");
        Environment.Exit(1);
    }
}

static string Transform(string text, string[] keys, bool encrypt)
{
    var sb = new StringBuilder(text.Length);
    var index = 0;
    var usedKeys = encrypt ? keys : keys.Select(InvertKey).ToArray();
    foreach (var ch in text)
    {
        if (!char.IsLetter(ch))
        {
            sb.Append(ch);
            continue;
        }

        var key = usedKeys[index % usedKeys.Length];
        var upper = char.ToUpperInvariant(ch);
        var mapped = key[upper - 'A'];
        sb.Append(char.IsUpper(ch) ? mapped : char.ToLowerInvariant(mapped));
        index++;
    }
    return sb.ToString();
}

static string InvertKey(string key)
{
    var inv = new char[26];
    for (var i = 0; i < 26; i++)
    {
        inv[key[i] - 'A'] = Alphabet[i];
    }
    return new string(inv);
}
