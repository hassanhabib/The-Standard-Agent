// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Files;

public interface IFileBroker
{
    bool DirectoryExists(string path);

    IReadOnlyList<string> SelectFiles(string path, string searchPattern, SearchOption searchOption);

    ValueTask<string> ReadFileAsync(string path);

    bool FileExists(string path);

    ValueTask<IReadOnlyList<string>> ReadAllLinesAsync(string path);

    void CreateDirectory(string path);

    ValueTask AppendAllLinesAsync(string path, IEnumerable<string> lines);
}
