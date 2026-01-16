using System.Text;

Console.WriteLine("Cifrul Playfair");
Console.WriteLine("1) Criptare");
Console.WriteLine("2) Decriptare");
Console.Write("Optiune: ");

var choice = Console.ReadLine()?.Trim();
Console.Write("Text: ");
var input = Console.ReadLine() ?? string.Empty;
Console.Write("Cheie: ");
var key = Console.ReadLine() ?? string.Empty;

var square = BuildSquare(key);

switch (choice)
{
    case "1":
        Console.WriteLine(Encrypt(input, square));
        break;
    case "2":
        Console.WriteLine(Decrypt(input, square));
        break;
    default:
        Console.WriteLine("Optiune invalida.");
        break;
}

static char[,] BuildSquare(string key)
{
    var sb = new StringBuilder();
    foreach (var ch in key.ToUpperInvariant())
    {
        if (ch is < 'A' or > 'Z') continue;
        var letter = ch == 'J' ? 'I' : ch;
        if (!sb.ToString().Contains(letter))
        {
            sb.Append(letter);
        }
    }

    for (var c = 'A'; c <= 'Z'; c++)
    {
        if (c == 'J') continue;
        if (!sb.ToString().Contains(c))
        {
            sb.Append(c);
        }
    }

    var square = new char[5, 5];
    var idx = 0;
    for (var r = 0; r < 5; r++)
    {
        for (var col = 0; col < 5; col++)
        {
            square[r, col] = sb[idx++];
        }
    }
    return square;
}

static string Encrypt(string text, char[,] square)
{
    var prepared = PrepareText(text, true);
    var sb = new StringBuilder();
    for (var i = 0; i < prepared.Length; i += 2)
    {
        var a = prepared[i];
        var b = prepared[i + 1];
        var (ra, ca) = Find(square, a);
        var (rb, cb) = Find(square, b);

        if (ra == rb)
        {
            sb.Append(square[ra, (ca + 1) % 5]);
            sb.Append(square[rb, (cb + 1) % 5]);
        }
        else if (ca == cb)
        {
            sb.Append(square[(ra + 1) % 5, ca]);
            sb.Append(square[(rb + 1) % 5, cb]);
        }
        else
        {
            sb.Append(square[ra, cb]);
            sb.Append(square[rb, ca]);
        }
    }
    return sb.ToString();
}

static string Decrypt(string text, char[,] square)
{
    var prepared = PrepareText(text, false);
    var sb = new StringBuilder();
    for (var i = 0; i < prepared.Length; i += 2)
    {
        var a = prepared[i];
        var b = prepared[i + 1];
        var (ra, ca) = Find(square, a);
        var (rb, cb) = Find(square, b);

        if (ra == rb)
        {
            sb.Append(square[ra, (ca + 4) % 5]);
            sb.Append(square[rb, (cb + 4) % 5]);
        }
        else if (ca == cb)
        {
            sb.Append(square[(ra + 4) % 5, ca]);
            sb.Append(square[(rb + 4) % 5, cb]);
        }
        else
        {
            sb.Append(square[ra, cb]);
            sb.Append(square[rb, ca]);
        }
    }
    return sb.ToString();
}

static string PrepareText(string text, bool encrypt)
{
    var letters = text.Where(char.IsLetter)
        .Select(c => char.ToUpperInvariant(c == 'J' ? 'I' : c))
        .ToList();

    var sb = new StringBuilder();
    for (var i = 0; i < letters.Count; i++)
    {
        var a = letters[i];
        var b = (i + 1 < letters.Count) ? letters[i + 1] : 'X';

        sb.Append(a);
        if (encrypt)
        {
            if (a == b)
            {
                sb.Append('X');
            }
            else
            {
                sb.Append(b);
                i++;
            }
        }
        else
        {
            sb.Append(b);
            i++;
        }
    }

    if (sb.Length % 2 != 0)
    {
        sb.Append('X');
    }
    return sb.ToString();
}

static (int Row, int Col) Find(char[,] square, char ch)
{
    for (var r = 0; r < 5; r++)
    {
        for (var c = 0; c < 5; c++)
        {
            if (square[r, c] == ch) return (r, c);
        }
    }
    return (-1, -1);
}
