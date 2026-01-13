using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PowerDown.Abstractions;

/// <summary>
/// Abstraction for file system operations to enable testing
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    StreamReader OpenText(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);
    IEnumerable<string> GetFiles(string path, string searchPattern);
    IEnumerable<string> GetDirectories(string path);
    string GetFileName(string path);
    string? GetDirectoryName(string path);
    string GetFullPath(string path);
}

/// <summary>
/// Production implementation using System.IO
/// </summary>
public class SystemFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    
    public bool DirectoryExists(string path) => Directory.Exists(path);
    
    public StreamReader OpenText(string path) => File.OpenText(path);
    
    public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(path, cancellationToken);
    }
    
    public async Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllLinesAsync(path, cancellationToken);
    }
    
    public IEnumerable<string> GetFiles(string path, string searchPattern)
    {
        return Directory.GetFiles(path, searchPattern);
    }
    
    public IEnumerable<string> GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }
    
    public string GetFileName(string path) => Path.GetFileName(path);
    
    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);
    
    public string GetFullPath(string path) => Path.GetFullPath(path);
}
