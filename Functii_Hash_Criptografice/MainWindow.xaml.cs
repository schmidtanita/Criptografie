using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Functii_Hash_Criptografice;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _hashCts;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select a file to hash",
            Filter = "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            FilePathTextBox.Text = dialog.FileName;
            HashOutputTextBox.Text = string.Empty;
            StatusTextBlock.Text = "Ready.";
        }
    }

    private async void ComputeButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = FilePathTextBox.Text;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            StatusTextBlock.Text = "Select a valid file first.";
            return;
        }

        var selectedItem = AlgorithmComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
        var algorithmName = selectedItem?.Content?.ToString() ?? "SHA256";

        _hashCts?.Cancel();
        _hashCts = new CancellationTokenSource();

        SetBusy(true, $"Computing {algorithmName}...");
        try
        {
            using var algorithm = CreateAlgorithm(algorithmName);
            var hex = await ComputeHashHexAsync(filePath, algorithm, _hashCts.Token);
            HashOutputTextBox.Text = hex;
            StatusTextBlock.Text = "Done.";
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Canceled.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
        }
        finally
        {
            SetBusy(false, StatusTextBlock.Text);
        }
    }

    private static async Task<string> ComputeHashHexAsync(string filePath, HashAlgorithm algorithm, CancellationToken token)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 8192,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var hash = await algorithm.ComputeHashAsync(stream, token);
        return Convert.ToHexString(hash);
    }

    private static HashAlgorithm CreateAlgorithm(string name)
    {
        return name switch
        {
            "MD5" => MD5.Create(),
            "RIPEMD160" => CreateRipemd160(),
            "SHA1" => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            "SHA3-256" => SHA3_256.Create(),
            "SHA3-384" => SHA3_384.Create(),
            "SHA3-512" => SHA3_512.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            _ => SHA256.Create()
        };
    }

    private void SetBusy(bool isBusy, string status)
    {
        HashProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        ComputeButton.IsEnabled = !isBusy;
        BrowseButton.IsEnabled = !isBusy;
        AlgorithmComboBox.IsEnabled = !isBusy;
        StatusTextBlock.Text = status;
    }

    private static HashAlgorithm CreateRipemd160()
    {
        var type = Type.GetType(
            "System.Security.Cryptography.RIPEMD160, System.Security.Cryptography.Algorithms");
        var createMethod = type?.GetMethod("Create", Type.EmptyTypes);
        var algorithm = createMethod?.Invoke(null, null) as HashAlgorithm;
        if (algorithm == null)
        {
            throw new NotSupportedException("RIPEMD160 is not available on this platform/runtime.");
        }

        return algorithm;
    }
}
