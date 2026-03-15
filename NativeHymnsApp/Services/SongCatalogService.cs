using System.Text.Json;
using System.IO;
using NativeHymnsApp.Models;

namespace NativeHymnsApp.Services;

public sealed class SongCatalogService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SongCatalogService(string dataDirectory)
    {
        DataDirectory = dataDirectory;
    }

    public string DataDirectory { get; }

    public SongCatalogLoadResult LoadCatalog()
    {
        if (!Directory.Exists(DataDirectory))
        {
            throw new DirectoryNotFoundException($"Data directory was not found: {DataDirectory}");
        }

        var files = Directory.GetFiles(DataDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName)
            .ToList();

        if (files.Count == 0)
        {
            throw new FileNotFoundException($"No structured hymn files were found in {DataDirectory}");
        }

        var songs = new List<SongDocument>();
        foreach (var file in files)
        {
            using var stream = File.OpenRead(file);
            var document = JsonSerializer.Deserialize<StructuredHymnFile>(stream, _jsonOptions);

            if (document?.Hymns is null)
            {
                continue;
            }

            foreach (var hymn in document.Hymns.OrderBy(item => item.Number))
            {
                songs.Add(MapHymn(hymn, Path.GetFileName(file)));
            }
        }

        return new SongCatalogLoadResult
        {
            DataDirectory = DataDirectory,
            Files = files.Select(Path.GetFileName).Where(name => !string.IsNullOrWhiteSpace(name)).Cast<string>().ToList(),
            Songs = songs
                .OrderBy(song => song.HymnNumber ?? int.MaxValue)
                .ThenBy(song => song.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList()
        };
    }

    private static SongDocument MapHymn(StructuredHymn hymn, string sourceFile)
    {
        return new SongDocument
        {
            Id = $"hymn-{hymn.Number:0000}",
            Kind = SongKind.Hymn,
            HymnNumber = hymn.Number,
            Title = hymn.Title.Trim(),
            SourceFile = sourceFile,
            Slides = hymn.Verses
                .OrderBy(verse => verse.Number)
                .Select(verse => new SlideSection
                {
                    Order = verse.Number,
                    Heading = $"Verse {verse.Number}",
                    Text = verse.Text.Trim()
                })
                .ToList()
        };
    }
}
