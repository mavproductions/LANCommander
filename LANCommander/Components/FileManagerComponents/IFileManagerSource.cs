﻿namespace LANCommander.Components.FileManagerComponents
{
    public interface IFileManagerSource
    {
        FileManagerDirectory GetCurrentPath();
        void SetCurrentPath(FileManagerDirectory path);
        IEnumerable<FileManagerDirectory> GetDirectoryTree();
        FileManagerDirectory ExpandNode(FileManagerDirectory node);
        IEnumerable<IFileManagerEntry> GetEntries();
        string GetEntryName(IFileManagerEntry entry);
        FileManagerDirectory GetDirectory(string path);
        FileManagerFile GetFile(string path);
        FileManagerDirectory CreateDirectory(string name);
        void DeleteEntry(IFileManagerEntry entry);
    }
}
