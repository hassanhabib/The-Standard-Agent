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
}
