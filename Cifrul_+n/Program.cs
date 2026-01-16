using System.Text;
using System.IO;
using System.Linq;

Console.Clear();
while (true)
{
    Console.WriteLine();
    Console.WriteLine("Cifrul \"+n\"");
    Console.WriteLine("1) Criptare");
    Console.WriteLine("2) Decriptare");
    Console.WriteLine("3) Criptanaliza (bruteforce 0..25)");
    Console.WriteLine("4) Criptanaliza (letter frequency analysis)");
    Console.WriteLine("0) Exit");
    Console.Write("Optiune: ");

    var choice = Console.ReadLine()?.Trim();
    if (choice == "0")
    {
        break;
    }

    var input = string.Empty;
    if (choice is "1" or "2" or "3")
    {
        input = ReadInputText();
    }

    switch (choice)
    {
        case "1":
            Console.Write("n (0..25): ");
            var nTextEncrypt = Console.ReadLine();
            var nEncrypt = int.TryParse(nTextEncrypt, out var parsedEncrypt) ? parsedEncrypt % 26 : 0;
            Console.WriteLine(ShiftText(input, nEncrypt));
            break;
        case "2":
            Console.Write("n (0..25): ");
            var nTextDecrypt = Console.ReadLine();
            var nDecrypt = int.TryParse(nTextDecrypt, out var parsedDecrypt) ? parsedDecrypt % 26 : 0;
            Console.WriteLine(ShiftText(input, -nDecrypt));
            break;
        case "3":
            for (var k = 0; k < 26; k++)
            {
                Console.WriteLine($"{k,2}: {ShiftText(input, -k)}");
            }
            break;
        case "4":
            var wordlistPath = ResolveTextPath("wordlist.txt");
            if (wordlistPath is null)
            {
                Console.WriteLine("Nu am gasit wordlist.txt pentru analiza.");
                break;
            }

            var analysisInput = ReadAnalysisText();
            if (!analysisInput.Ok)
            {
                break;
            }

            var encryptedText = analysisInput.Text;
            var wordlist = File.ReadAllLines(wordlistPath);
            var analysis = AnalyzeByFrequency(encryptedText, wordlist);
            var outputPath = analysisInput.SourcePath is null
                ? "decrypted.txt"
                : GetSiblingOutputPath(analysisInput.SourcePath, "decrypted.txt");
            File.WriteAllText(outputPath, analysis.DecryptedText);
            Console.WriteLine($"Most probable key: {analysis.Key}");
            Console.WriteLine($"Matched words: {analysis.MatchCount}");
            Console.WriteLine("Decrypted text:");
            Console.WriteLine(analysis.DecryptedText);
            Console.WriteLine($"Decrypted text saved to {outputPath}");
            break;
        default:
            Console.WriteLine("Optiune invalida.");
            break;
    }

    Console.WriteLine("Press any key to continue...");
    Console.ReadKey(true);
    Console.Clear();
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

static string ReadInputText()
{
    Console.Write("Sursa text (1=consola, 2=fișier): ");
    var source = Console.ReadLine()?.Trim();
    if (source == "2")
    {
        var filePath = ResolveInputFilePath();
        if (filePath is null)
        {
            Console.WriteLine("Nu am gasit fisierul ales. Se foloseste text gol.");
            return string.Empty;
        }
        return File.ReadAllText(filePath);
    }

    Console.Write("Text: ");
    return Console.ReadLine() ?? string.Empty;
}

static (string Text, string? SourcePath, bool Ok) ReadAnalysisText()
{
    Console.Write("Sursa text (1=consola, 2=fișier): ");
    var source = Console.ReadLine()?.Trim();
    if (source == "2")
    {
        var filePath = ResolveInputFilePath();
        if (filePath is null)
        {
            Console.WriteLine("Nu am gasit fisierul ales.");
            return (string.Empty, null, false);
        }
        return (File.ReadAllText(filePath), filePath, true);
    }

    Console.Write("Text: ");
    return (Console.ReadLine() ?? string.Empty, null, true);
}

static string? ResolveInputFilePath()
{
    Console.WriteLine("Alege fisier:");
    Console.WriteLine("1) encrypted.txt");
    Console.WriteLine("2) encrypted7.txt");
    Console.WriteLine("3) encrypted13.txt");
    Console.Write("Optiune: ");
    var option = Console.ReadLine()?.Trim();
    var fileName = option switch
    {
        "1" => "encrypted.txt",
        "2" => "encrypted7.txt",
        "3" => "encrypted13.txt",
        _ => null
    };

    if (fileName is null)
    {
        return null;
    }

    return File.Exists(fileName) ? fileName : null;
}

static (string DecryptedText, int Key, int MatchCount) AnalyzeByFrequency(string text, string[] dictionary)
{
    var encryptedText = text.ToUpperInvariant();
    var bestDecryption = string.Empty;
    var bestMatchCount = 0;
    var bestKey = 0;

    for (var shift = 0; shift < 26; shift++)
    {
        var decrypted = DecryptCaesar(encryptedText, shift);
        var matchCount = CountEnglishWords(decrypted, dictionary);

        if (matchCount > bestMatchCount)
        {
            bestMatchCount = matchCount;
            bestDecryption = decrypted;
            bestKey = shift;
        }
    }

    return (bestDecryption, bestKey, bestMatchCount);
}

static string DecryptCaesar(string input, int n)
{
    var sb = new StringBuilder(input.Length);
    foreach (var ch in input)
    {
        if (ch is >= 'A' and <= 'Z')
        {
            var shifted = (char)('A' + (ch - 'A' - n + 26) % 26);
            sb.Append(shifted);
        }
        else
        {
            sb.Append(ch);
        }
    }
    return sb.ToString();
}

static int CountEnglishWords(string text, string[] dictionary)
{
    var words = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    var count = 0;
    foreach (var word in words)
    {
        if (dictionary.Contains(word.ToLowerInvariant()))
        {
            count++;
        }
    }
    return count;
}

static string? ResolveTextPath(string fileName)
{
    return File.Exists(fileName) ? fileName : null;
}

static string GetSiblingOutputPath(string inputPath, string outputFileName)
{
    var dir = Path.GetDirectoryName(inputPath);
    return string.IsNullOrEmpty(dir) ? outputFileName : Path.Combine(dir, outputFileName);
}
