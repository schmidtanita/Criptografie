using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

const int KeySize = 8;
var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var inputPath = Path.Combine(projectDir, "input.txt");
var encryptedPath = Path.Combine(projectDir, "encrypted.txt");
var decryptedPath = Path.Combine(projectDir, "decrypted.txt");
var keyPath = Path.Combine(projectDir, "key.txt");
var rng = new Random();

Console.WriteLine("Merkle-Hellman Knapsack PK encryption");
Console.WriteLine($"Key size: {KeySize} bits.");

KeyPair? keyPair = null;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1) Genereaza chei (key.txt)");
    Console.WriteLine("2) Criptare (input.txt + key.txt -> encrypted.txt)");
    Console.WriteLine("3) Decriptare (encrypted.txt -> decrypted.txt)");
    Console.WriteLine("5) Iesire");
    Console.Write("Optiune: ");

    var choice = Console.ReadLine()?.Trim();
    switch (choice)
    {
        case "1":
            keyPair = GenerateKeyPair(KeySize, rng);
            PrintKeyPair(keyPair);
            SaveKeyPair(keyPair);
            Console.WriteLine("Cheile au fost salvate in key.txt.");
            break;
        case "2":
            if (keyPair == null)
            {
                keyPair = GenerateKeyPair(KeySize, rng);
                Console.WriteLine("Chei generate automat.");
                PrintKeyPair(keyPair);
                SaveKeyPair(keyPair);
                Console.WriteLine("Cheile au fost salvate in key.txt.");
            }
            EncryptFlow(keyPair);
            break;
        case "3":
            if (keyPair == null)
            {
                Console.WriteLine("Genereaza cheile mai intai.");
                break;
            }
            DecryptFlow(keyPair);
            break;
        case "5":
            return;
        default:
            Console.WriteLine("Optiune invalida.");
            break;
    }

}

void EncryptFlow(KeyPair keyPair)
{
    if (!File.Exists(inputPath))
    {
        Console.WriteLine("Fisier inexistent: input.txt");
        return;
    }

    var input = File.ReadAllText(inputPath);
    var data = Encoding.UTF8.GetBytes(input);
    var cipher = Encrypt(data, keyPair.PublicKey);
    var cipherText = string.Join(" ", cipher);
    File.WriteAllText(encryptedPath, cipherText);
    Console.WriteLine("Ciphertext (salvat in encrypted.txt):");
    Console.WriteLine(cipherText);
}

void DecryptFlow(KeyPair keyPair)
{
    if (!TryReadCipherValuesFromFile(encryptedPath, out var cipher))
    {
        Console.WriteLine("Lista vida sau invalida in encrypted.txt.");
        return;
    }

    if (!TryDecryptAndWrite(cipher, keyPair))
    {
        Console.WriteLine("Eroare: Ciphertext invalid pentru cheia curenta.");
    }
}

bool TryDecryptAndWrite(List<int> cipher, KeyPair keyPair)
{
    try
    {
        var data = Decrypt(cipher, keyPair);
        var text = Encoding.UTF8.GetString(data);
        File.WriteAllText(decryptedPath, text);
        Console.WriteLine("Text (salvat in decrypted.txt):");
        Console.WriteLine(text);
        return true;
    }
    catch
    {
        File.WriteAllText(decryptedPath, "Error");
        return false;
    }
}

static bool TryReadCipherValuesFromFile(string path, out List<int> values)
{
    values = new List<int>();
    if (!File.Exists(path))
    {
        return false;
    }

    var text = File.ReadAllText(path);
    return TryParseCipherValues(text, out values);
}

static bool TryParseCipherValues(string text, out List<int> values)
{
    values = new List<int>();
    var parts = text.Split(new[] { ' ', ',', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var part in parts)
    {
        if (!int.TryParse(part, out var value))
        {
            return false;
        }
        values.Add(value);
    }
    return values.Count > 0;
}

static List<int> Encrypt(byte[] data, int[] publicKey)
{
    if (publicKey.Length != KeySize)
    {
        throw new InvalidOperationException("Dimensiunea cheii trebuie sa fie 8.");
    }

    var output = new List<int>(data.Length);
    foreach (var value in data)
    {
        var sum = 0;
        for (var i = 0; i < publicKey.Length; i++)
        {
            var bit = (value >> (publicKey.Length - 1 - i)) & 1;
            if (bit == 1)
            {
                sum += publicKey[i];
            }
        }
        output.Add(sum);
    }
    return output;
}

static byte[] Decrypt(IReadOnlyList<int> ciphertext, KeyPair keyPair)
{
    if (keyPair.PrivateKey.Length != KeySize)
    {
        throw new InvalidOperationException("Dimensiunea cheii trebuie sa fie 8.");
    }

    var output = new byte[ciphertext.Count];
    for (var i = 0; i < ciphertext.Count; i++)
    {
        output[i] = DecryptBlock(ciphertext[i], keyPair);
    }
    return output;
}

static byte DecryptBlock(int cipher, KeyPair keyPair)
{
    var s = (cipher * keyPair.MultiplierInverse) % keyPair.Modulus;
    var bits = new int[keyPair.PrivateKey.Length];
    for (var i = keyPair.PrivateKey.Length - 1; i >= 0; i--)
    {
        if (s >= keyPair.PrivateKey[i])
        {
            bits[i] = 1;
            s -= keyPair.PrivateKey[i];
        }
    }

    if (s != 0)
    {
        throw new InvalidOperationException("Ciphertext invalid pentru cheia curenta.");
    }

    int value = 0;
    for (var i = 0; i < bits.Length; i++)
    {
        value = (value << 1) | bits[i];
    }
    return (byte)value;
}

static KeyPair GenerateKeyPair(int length, Random rng)
{
    var w = new int[length];
    var sum = 0;
    for (var i = 0; i < length; i++)
    {
        var next = sum + rng.Next(1, 10);
        w[i] = next;
        sum += next;
    }

    var q = sum + rng.Next(1, sum + 1);
    int r;
    do
    {
        r = rng.Next(2, q);
    } while (Gcd(r, q) != 1);

    var b = w.Select(value => (r * value) % q).ToArray();
    var rInv = ModInverseSimple(r, q);
    return new KeyPair(w, b, q, r, rInv);
}

static void PrintKeyPair(KeyPair keyPair)
{
    Console.WriteLine("Cheie privata w: " + string.Join(" ", keyPair.PrivateKey));
    Console.WriteLine("q: " + keyPair.Modulus);
    Console.WriteLine("r: " + keyPair.Multiplier);
    Console.WriteLine("Cheie publica b: " + string.Join(" ", keyPair.PublicKey));
}

void SaveKeyPair(KeyPair keyPair)
{
    var lines = new[]
    {
        "w: " + string.Join(" ", keyPair.PrivateKey),
        "q: " + keyPair.Modulus,
        "r: " + keyPair.Multiplier
    };
    File.WriteAllLines(keyPath, lines);
}

static int ModInverseSimple(int a, int mod)
{
    for (var x = 1; x < mod; x++)
    {
        if ((a * x) % mod == 1)
        {
            return x;
        }
    }
    throw new InvalidOperationException("Nu exista invers modular.");
}

static int Gcd(int a, int b)
{
    a = Math.Abs(a);
    b = Math.Abs(b);
    while (b != 0)
    {
        var temp = a % b;
        a = b;
        b = temp;
    }
    return a;
}

class KeyPair
{
    public KeyPair(int[] privateKey, int[] publicKey, int modulus, int multiplier, int multiplierInverse)
    {
        PrivateKey = privateKey;
        PublicKey = publicKey;
        Modulus = modulus;
        Multiplier = multiplier;
        MultiplierInverse = multiplierInverse;
    }

    public int[] PrivateKey { get; }
    public int[] PublicKey { get; }
    public int Modulus { get; }
    public int Multiplier { get; }
    public int MultiplierInverse { get; }
}
