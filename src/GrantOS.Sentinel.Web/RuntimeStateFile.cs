namespace GrantOS.Sentinel.Web;

/// <summary>Publishes the dynamic Electron web URL for the local terminal launcher.</summary>
public static class RuntimeStateFile
{
    public static string PathName => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GrantOS.Sentinel",
        "runtime-url");

    public static async Task WriteAsync(string address)
    {
        var directory = Path.GetDirectoryName(PathName)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(PathName, address.TrimEnd('/'));
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(PathName))
                File.Delete(PathName);
        }
        catch (IOException)
        {
            // A stale URL is harmless: the launcher's health check rejects it.
        }
    }
}
