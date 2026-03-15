using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using NativeHymnsApp.Models;

namespace NativeHymnsApp.Services;

public sealed class AppStateService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string StateDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NativeHymnsApp");

    public string StateFilePath => Path.Combine(StateDirectory, "app-state.json");

    public AppStateSnapshot Load()
    {
        if (!File.Exists(StateFilePath))
        {
            return new AppStateSnapshot();
        }

        try
        {
            using var stream = File.OpenRead(StateFilePath);
            return JsonSerializer.Deserialize<AppStateSnapshot>(stream, _jsonOptions) ?? new AppStateSnapshot();
        }
        catch
        {
            return new AppStateSnapshot();
        }
    }

    public void Save(AppStateSnapshot snapshot)
    {
        Directory.CreateDirectory(StateDirectory);
        using var stream = File.Create(StateFilePath);
        JsonSerializer.Serialize(stream, snapshot, _jsonOptions);
    }
}
