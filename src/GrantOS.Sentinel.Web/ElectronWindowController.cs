using ElectronNET.API;

namespace GrantOS.Sentinel.Web;

/// <summary>Coordinates the single Electron window with localhost API callers.</summary>
public sealed class ElectronWindowController
{
    private BrowserWindow? _window;
    private string? _baseAddress;

    public void SetWindow(BrowserWindow window) => _window = window;

    public void SetBaseAddress(string baseAddress) => _baseAddress = baseAddress.TrimEnd('/') + "/";

    public async Task<bool> FocusAsync(string? prompt)
    {
        var window = _window;
        if (window is null)
            return false;

        if (await window.IsMinimizedAsync())
            window.Restore();
        window.Show();
        window.Focus();

        if (!string.IsNullOrWhiteSpace(prompt) && _baseAddress is not null)
        {
            var target = new Uri(new Uri(_baseAddress), $"chat?prompt={Uri.EscapeDataString(prompt)}");
            window.LoadURL(target.ToString());
        }

        return true;
    }
}
