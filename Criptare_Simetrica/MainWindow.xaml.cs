using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace Criptare_Simetrica;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AlgorithmCombo.ItemsSource = new[] { "AES", "DES", "TripleDES", "Rijndael", "RC2" };
        ModeCombo.ItemsSource = Enum.GetNames(typeof(CipherMode));
        PaddingCombo.ItemsSource = Enum.GetNames(typeof(PaddingMode));

        AlgorithmCombo.SelectedIndex = 0;
        ModeCombo.SelectedItem = nameof(CipherMode.CBC);
        PaddingCombo.SelectedItem = nameof(PaddingMode.PKCS7);
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();
        if (dialog.ShowDialog() == true)
        {
            InputFileTextBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(OutputFileTextBox.Text))
            {
                OutputFileTextBox.Text = dialog.FileName + ".enc";
            }
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog();
        if (dialog.ShowDialog() == true)
        {
            OutputFileTextBox.Text = dialog.FileName;
        }
    }

    private void GenerateKey_Click(object sender, RoutedEventArgs e)
    {
        using var algorithm = CreateAlgorithm();
        var keySize = GetMaxKeySize(algorithm);
        var bytes = RandomNumberGenerator.GetBytes(keySize / 8);
        KeyTextBox.Text = Convert.ToBase64String(bytes);
    }

    private void GenerateIV_Click(object sender, RoutedEventArgs e)
    {
        using var algorithm = CreateAlgorithm();
        var ivSize = algorithm.BlockSize / 8;
        var bytes = RandomNumberGenerator.GetBytes(ivSize);
        IVTextBox.Text = Convert.ToBase64String(bytes);
    }

    private async void Encrypt_Click(object sender, RoutedEventArgs e)
    {
        await RunCryptoAsync(encrypt: true);
    }

    private async void Decrypt_Click(object sender, RoutedEventArgs e)
    {
        await RunCryptoAsync(encrypt: false);
    }

    private async Task RunCryptoAsync(bool encrypt)
    {
        var inputFile = InputFileTextBox.Text.Trim();
        var outputFile = OutputFileTextBox.Text.Trim();
        if (!File.Exists(inputFile))
        {
            StatusTextBlock.Text = "Fisier input invalid.";
            return;
        }
        if (string.IsNullOrWhiteSpace(outputFile))
        {
            StatusTextBlock.Text = "Fisier output invalid.";
            return;
        }

        StatusTextBlock.Text = "Se proceseaza...";
        try
        {
            await Task.Run(() =>
            {
                using var algorithm = CreateAlgorithm();
                ApplySettings(algorithm);
                var keyBytes = ResolveKey(algorithm);
                var ivBytes = ResolveIV(algorithm);

                algorithm.Key = keyBytes;
                if (algorithm.Mode != CipherMode.ECB)
                {
                    algorithm.IV = ivBytes;
                }

                if (encrypt)
                {
                    EncryptFile(inputFile, outputFile, algorithm);
                }
                else
                {
                    DecryptFile(inputFile, outputFile, algorithm);
                }
            });
            StatusTextBlock.Text = encrypt ? "Criptare finalizata." : "Decriptare finalizata.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Eroare: " + ex.Message;
        }
    }

    private SymmetricAlgorithm CreateAlgorithm()
    {
        return AlgorithmCombo.SelectedItem?.ToString() switch
        {
            "DES" => DES.Create(),
            "TripleDES" => TripleDES.Create(),
            "Rijndael" => Rijndael.Create(),
            "RC2" => RC2.Create(),
            _ => Aes.Create(),
        };
    }

    private void ApplySettings(SymmetricAlgorithm algorithm)
    {
        algorithm.Mode = Enum.Parse<CipherMode>(ModeCombo.SelectedItem?.ToString() ?? nameof(CipherMode.CBC));
        algorithm.Padding = Enum.Parse<PaddingMode>(PaddingCombo.SelectedItem?.ToString() ?? nameof(PaddingMode.PKCS7));
    }

    private byte[] ResolveKey(SymmetricAlgorithm algorithm)
    {
        var raw = KeyTextBox.Text.Trim();
        byte[] key;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (!TryFromBase64(raw, out key))
            {
                key = Encoding.UTF8.GetBytes(raw);
            }
        }
        else
        {
            key = RandomNumberGenerator.GetBytes(GetMaxKeySize(algorithm) / 8);
            KeyTextBox.Dispatcher.Invoke(() => KeyTextBox.Text = Convert.ToBase64String(key));
        }

        var size = GetMaxKeySize(algorithm) / 8;
        return DeriveFixedLength(key, size);
    }

    private byte[] ResolveIV(SymmetricAlgorithm algorithm)
    {
        var raw = IVTextBox.Text.Trim();
        var size = algorithm.BlockSize / 8;
        if (algorithm.Mode == CipherMode.ECB)
        {
            return new byte[size];
        }
        byte[] iv;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (!TryFromBase64(raw, out iv))
            {
                iv = Encoding.UTF8.GetBytes(raw);
            }
        }
        else
        {
            iv = RandomNumberGenerator.GetBytes(size);
            IVTextBox.Dispatcher.Invoke(() => IVTextBox.Text = Convert.ToBase64String(iv));
        }
        return DeriveFixedLength(iv, size);
    }

    private static bool TryFromBase64(string input, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(input);
            return true;
        }
        catch
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    private static int GetMaxKeySize(SymmetricAlgorithm algorithm)
    {
        var ks = algorithm.LegalKeySizes[^1];
        return ks.MaxSize;
    }

    private static byte[] DeriveFixedLength(byte[] input, int length)
    {
        if (input.Length == length) return input;
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(input);
        if (hash.Length == length) return hash;
        var output = new byte[length];
        Array.Copy(hash, output, length);
        return output;
    }

    private static void EncryptFile(string inputPath, string outputPath, SymmetricAlgorithm algorithm)
    {
        using var input = File.OpenRead(inputPath);
        using var output = File.Create(outputPath);
        using var crypto = new CryptoStream(output, algorithm.CreateEncryptor(), CryptoStreamMode.Write);
        input.CopyTo(crypto);
    }

    private static void DecryptFile(string inputPath, string outputPath, SymmetricAlgorithm algorithm)
    {
        using var input = File.OpenRead(inputPath);
        using var output = File.Create(outputPath);
        using var crypto = new CryptoStream(input, algorithm.CreateDecryptor(), CryptoStreamMode.Read);
        crypto.CopyTo(output);
    }
}
