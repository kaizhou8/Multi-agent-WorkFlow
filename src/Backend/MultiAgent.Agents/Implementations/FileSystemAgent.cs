using Microsoft.Extensions.Logging;
using MultiAgent.Shared.Models.Agent;
using System.Text.Json;
namespace MultiAgent.Agents.Implementations;
/// <summary>
/// File system agent implementation
/// Provides file and directory operations
/// </summary>
public class FileSystemAgent : BaseAgent
{
    private readonly string _workingDirectory;
    private readonly HashSet<string> _allowedExtensions;
    private readonly long _maxFileSize;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="workingDirectory">Working directory</param>
    public FileSystemAgent(ILogger<BaseAgent> logger, string? workingDirectory = null)
        : base("filesystem-agent", "File System Agent", logger)
    {
        _workingDirectory = workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _allowedExtensions = new HashSet<string> { ".txt", ".json", ".xml", ".csv", ".md", ".log" };
        _maxFileSize = 10 * 1024 * 1024; // 10MB
        // Configure capabilities
        Capabilities = new AgentCapabilities
        {
            SupportedOperations = new List<string>
            {
                "read_file",
                "write_file",
                "delete_file",
                "list_directory",
                "create_directory",
                "delete_directory",
                "get_file_info",
                "search_files"
            },
            MaxConcurrentTasks = 3,
            SupportsAsyncExecution = true,
            RequiresAuthentication = false,
            ResourceRequirements = new Dictionary<string, object>
            {
                ["WorkingDirectory"] = _workingDirectory,
                ["AllowedExtensions"] = _allowedExtensions.ToList(),
                ["MaxFileSize"] = _maxFileSize
            }
        };
    }
    /// <summary>
    /// Execute command implementation
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <returns>Execution result</returns>
    protected override async Task<AgentExecutionResult> ExecuteCommandAsync(AgentCommand command)
    {
        var result = new AgentExecutionResult
        {
            AgentId = Id,
            CommandId = command.Id,
            StartTime = DateTime.UtcNow
        };
        try
        {
            switch (command.Type.ToLowerInvariant())
            {
                case "read_file":
                    result = await ReadFileAsync(command, result);
                    break;
                case "write_file":
                    result = await WriteFileAsync(command, result);
                    break;
                case "delete_file":
                    result = await DeleteFileAsync(command, result);
                    break;
                case "list_directory":
                    result = await ListDirectoryAsync(command, result);
                    break;
                case "create_directory":
                    result = await CreateDirectoryAsync(command, result);
                    break;
                case "delete_directory":
                    result = await DeleteDirectoryAsync(command, result);
                    break;
                case "get_file_info":
                    result = await GetFileInfoAsync(command, result);
                    break;
                case "search_files":
                    result = await SearchFilesAsync(command, result);
                    break;
                default:
                    result.Success = false;
                    result.ErrorMessage = $"Unsupported operation: {command.Type}";
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Logs.Add($"Error: {ex.Message}");
        }
        return result;
    }
    /// <summary>
    /// Read file content
    /// </summary>
    private async Task<AgentExecutionResult> ReadFileAsync(AgentCommand command, AgentExecutionResult result)
    {
        if (!command.Parameters.TryGetValue("file_path", out var filePathObj) || filePathObj is not string filePath)
        {
            result.Success = false;
            result.ErrorMessage = "file_path parameter is required";
            return result;
        }
        var fullPath = GetSafePath(filePath);
        if (fullPath == null)
        {
            result.Success = false;
            result.ErrorMessage = "Invalid file path or path outside working directory";
            return result;
        }
        if (!File.Exists(fullPath))
        {
            result.Success = false;
            result.ErrorMessage = "File not found";
            return result;
        }
        if (!IsAllowedExtension(fullPath))
        {
            result.Success = false;
            result.ErrorMessage = "File extension not allowed";
            return result;
        }
        var content = await File.ReadAllTextAsync(fullPath);
        result.Success = true;
        result.OutputData["content"] = content;
        result.OutputData["file_path"] = fullPath;
        result.OutputData["file_size"] = new FileInfo(fullPath).Length;
        result.Logs.Add($"Successfully read file: {fullPath}");
        return result;
    }
    /// <summary>
    /// Write file content
    /// </summary>
    private async Task<AgentExecutionResult> WriteFileAsync(AgentCommand command, AgentExecutionResult result)
    {
        if (!command.Parameters.TryGetValue("file_path", out var filePathObj) || filePathObj is not string filePath)
        {
            result.Success = false;
            result.ErrorMessage = "file_path parameter is required";
            return result;
        }
        if (!command.Parameters.TryGetValue("content", out var contentObj) || contentObj is not string content)
        {
            result.Success = false;
            result.ErrorMessage = "content parameter is required";
            return result;
        }
        var fullPath = GetSafePath(filePath);
        if (fullPath == null)
        {
            result.Success = false;
            result.ErrorMessage = "Invalid file path or path outside working directory";
            return result;
        }
        if (!IsAllowedExtension(fullPath))
        {
            result.Success = false;
            result.ErrorMessage = "File extension not allowed";
            return result;
        }
        if (content.Length > _maxFileSize)
        {
            result.Success = false;
            result.ErrorMessage = $"Content size exceeds maximum allowed size of {_maxFileSize} bytes";
            return result;
        }
        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.WriteAllTextAsync(fullPath, content);
        result.Success = true;
        result.OutputData["file_path"] = fullPath;
        result.OutputData["bytes_written"] = content.Length;
        result.Logs.Add($"Successfully wrote file: {fullPath}");
        return result;
    }
    /// <summary>
    /// Delete file
    /// </summary>
    private Task<AgentExecutionResult> DeleteFileAsync(AgentCommand command, AgentExecutionResult result)
    {
        if (!command.Parameters.TryGetValue("file_path", out var filePathObj) || filePathObj is not string filePath)
        {
            result.Success = false;
            result.ErrorMessage = "file_path parameter is required";
            return Task.FromResult(result);
        }
        var fullPath = GetSafePath(filePath);
        if (fullPath == null)
        {
            result.Success = false;
            result.ErrorMessage = "Invalid file path or path outside working directory";
            return Task.FromResult(result);
        }
        if (!File.Exists(fullPath))
        {
            result.Success = false;
            result.ErrorMessage = "File not found";
            return Task.FromResult(result);
        }
        File.Delete(fullPath);
        result.Success = true;
        result.OutputData["deleted_file"] = fullPath;
        result.Logs.Add($"Successfully deleted file: {fullPath}");
        return Task.FromResult(result);
    }
    /// <summary>
    /// List directory contents
    /// </summary>
    private Task<AgentExecutionResult> ListDirectoryAsync(AgentCommand command, AgentExecutionResult result)
    {
        var directoryPath = _workingDirectory;
        if (command.Parameters.TryGetValue("directory_path", out var dirPathObj) && dirPathObj is string dirPath)
        {
            var fullPath = GetSafePath(dirPath);
            if (fullPath != null)
            {
                directoryPath = fullPath;
            }
        }
        if (!Directory.Exists(directoryPath))
        {
            result.Success = false;
            result.ErrorMessage = "Directory not found";
            return Task.FromResult(result);
        }
        var files = Directory.GetFiles(directoryPath)
            .Select(f => new
            {
                Name = Path.GetFileName(f),
                FullPath = f,
                Size = new FileInfo(f).Length,
                LastModified = File.GetLastWriteTime(f),
                Extension = Path.GetExtension(f)
            }).ToList();
        var directories = Directory.GetDirectories(directoryPath)
            .Select(d => new
            {
                Name = Path.GetFileName(d),
                FullPath = d,
                LastModified = Directory.GetLastWriteTime(d)
            }).ToList();
        result.Success = true;
        result.OutputData["directory_path"] = directoryPath;
        result.OutputData["files"] = files;
        result.OutputData["directories"] = directories;
        result.OutputData["total_files"] = files.Count;
        result.OutputData["total_directories"] = directories.Count;
        result.Logs.Add($"Successfully listed directory: {directoryPath}");
        return Task.FromResult(result);
    }
    /// <summary>
    /// Create directory
    /// </summary>
    private Task<AgentExecutionResult> CreateDirectoryAsync(AgentCommand command, AgentExecutionResult result)
    {
        if (!command.Parameters.TryGetValue("directory_path", out var dirPathObj) || dirPathObj is not string dirPath)
        {
            result.Success = false;
            result.ErrorMessage = "directory_path parameter is required";
            return Task.FromResult(result);
        }
        var fullPath = GetSafePath(dirPath);
        if (fullPath == null)
        {
            result.Success = false;
            result.ErrorMessage = "Invalid directory path or path outside working directory";
            return Task.FromResult(result);
        }
        if (Directory.Exists(fullPath))
        {
            result.Success = true;
            result.OutputData["directory_path"] = fullPath;
            result.OutputData["already_exists"] = true;
            result.Logs.Add($"Directory already exists: {fullPath}");
            return Task.FromResult(result);
        }
        Directory.CreateDirectory(fullPath);
        result.Success = true;
        result.OutputData["directory_path"] = fullPath;
        result.OutputData["created"] = true;
        result.Logs.Add($"Successfully created directory: {fullPath}");
        return Task.FromResult(result);
    }
    /// <summary>
    /// Delete directory
    /// </summary>
    private Task<AgentExecutionResult> DeleteDirectoryAsync(AgentCommand command, AgentExecutionResult result)
    {
        if (!command.Parameters.TryGetValue("directory_path", out var dirPathObj) || dirPathObj is not string dirPath)
        {
            result.Success = false;
            result.ErrorMessage = "directory_path parameter is required";
            return Task.FromResult(result);
        }
        var fullPath = GetSafePath(dirPath);
        if (fullPath == null)
        {
            result.Success = false;
            result.ErrorMessage = "Invalid directory path or path outside working directory";
            return Task.FromResult(result);
        }
        if (!Directory.Exists(fullPath))
        {
            result.Success = false;
            result.ErrorMessage = "Directory not found";
            return Task.FromResult(result);
        }
        var recursive = command.Parameters.TryGetValue("recursive", out var recursiveObj) &&
                       recursiveObj is bool recursiveValue && recursiveValue;
        Directory.Delete(fullPath, recursive);
        result.Success = true;
        result.OutputData["deleted_directory"] = fullPath;
        result.OutputData["recursive"] = recursive;
        result.Logs.Add($"Successfully deleted directory: {fullPath}");
        return Task.FromResult(result);
    }
    /// <summary>
    /// Get file information
    /// </summary>
    private Task<AgentExecutionResult> GetFileInfoAsync(AgentCommand command, AgentExecutionResult result)
    {
        if (!command.Parameters.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            result.Success = false;
            result.ErrorMessage = "path parameter is required";
            return Task.FromResult(result);
        }
        var fullPath = GetSafePath(path);
        if (fullPath == null)
        {
            result.Success = false;
            result.ErrorMessage = "Invalid path or path outside working directory";
            return Task.FromResult(result);
        }
        if (File.Exists(fullPath))
        {
            var fileInfo = new FileInfo(fullPath);
            result.Success = true;
            result.OutputData["type"] = "file";
            result.OutputData["path"] = fullPath;
            result.OutputData["name"] = fileInfo.Name;
            result.OutputData["size"] = fileInfo.Length;
            result.OutputData["extension"] = fileInfo.Extension;
            result.OutputData["created"] = fileInfo.CreationTime;
            result.OutputData["modified"] = fileInfo.LastWriteTime;
            result.OutputData["accessed"] = fileInfo.LastAccessTime;
        }
        else if (Directory.Exists(fullPath))
        {
            var dirInfo = new DirectoryInfo(fullPath);
            result.Success = true;
            result.OutputData["type"] = "directory";
            result.OutputData["path"] = fullPath;
            result.OutputData["name"] = dirInfo.Name;
            result.OutputData["created"] = dirInfo.CreationTime;
            result.OutputData["modified"] = dirInfo.LastWriteTime;
            result.OutputData["accessed"] = dirInfo.LastAccessTime;
        }
        else
        {
            result.Success = false;
            result.ErrorMessage = "Path not found";
        }
        return Task.FromResult(result);
    }
    /// <summary>
    /// Search files
    /// </summary>
    private Task<AgentExecutionResult> SearchFilesAsync(AgentCommand command, AgentExecutionResult result)
    {
        var searchDirectory = _workingDirectory;
        if (command.Parameters.TryGetValue("directory_path", out var dirPathObj) && dirPathObj is string dirPath)
        {
            var fullPath = GetSafePath(dirPath);
            if (fullPath != null && Directory.Exists(fullPath))
            {
                searchDirectory = fullPath;
            }
        }
        var pattern = "*";
        if (command.Parameters.TryGetValue("pattern", out var patternObj) && patternObj is string patternValue)
        {
            pattern = patternValue;
        }
        var recursive = command.Parameters.TryGetValue("recursive", out var recursiveObj) &&
                       recursiveObj is bool recursiveValue && recursiveValue;
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var foundFiles = Directory.GetFiles(searchDirectory, pattern, searchOption)
            .Where(f => IsAllowedExtension(f))
            .Select(f => new
            {
                Name = Path.GetFileName(f),
                FullPath = f,
                Size = new FileInfo(f).Length,
                LastModified = File.GetLastWriteTime(f),
                Extension = Path.GetExtension(f)
            }).ToList();
        result.Success = true;
        result.OutputData["search_directory"] = searchDirectory;
        result.OutputData["pattern"] = pattern;
        result.OutputData["recursive"] = recursive;
        result.OutputData["files"] = foundFiles;
        result.OutputData["total_found"] = foundFiles.Count;
        result.Logs.Add($"Found {foundFiles.Count} files matching pattern '{pattern}' in {searchDirectory}");
        return Task.FromResult(result);
    }
    /// <summary>
    /// Get safe path within working directory
    /// </summary>
    private string? GetSafePath(string relativePath)
    {
        try
        {
            var fullPath = Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(_workingDirectory, relativePath);
            var normalizedPath = Path.GetFullPath(fullPath);
            var normalizedWorkingDir = Path.GetFullPath(_workingDirectory);
            // Ensure path is within working directory
            if (!normalizedPath.StartsWith(normalizedWorkingDir, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return normalizedPath;
        }
        catch
        {
            return null;
        }
    }
    /// <summary>
    /// Check if file extension is allowed
    /// </summary>
    private bool IsAllowedExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }
}

