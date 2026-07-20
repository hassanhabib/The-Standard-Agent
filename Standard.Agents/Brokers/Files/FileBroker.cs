// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Files;

public sealed class FileBroker : IFileBroker
{
    public bool DirectoryExists(string path) =>
        Directory.Exists(path);

    public IReadOnlyList<string> SelectFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption).ToList();

    public async ValueTask<string> ReadFileAsync(string path) =>
        await File.ReadAllTextAsync(path);

    public bool FileExists(string path) =>
        File.Exists(path);

    public async ValueTask<IReadOnlyList<string>> ReadAllLinesAsync(string path) =>
        await File.ReadAllLinesAsync(path);

    public void CreateDirectory(string path) =>
        Directory.CreateDirectory(path);

    public async ValueTask AppendAllLinesAsync(string path, IEnumerable<string> lines) =>
        await File.AppendAllLinesAsync(path, lines);
}
