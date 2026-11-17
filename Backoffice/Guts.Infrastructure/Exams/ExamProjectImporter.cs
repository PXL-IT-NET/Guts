using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Dtos.Exam;
using Guts.Application.Services.Exam;
using Guts.Common;
using Guts.Domain.ExamAggregate;

namespace Guts.Infrastructure.Exams;

internal class ExamProjectImporter : IExamProjectImporter
{
    public const long MaxArchiveBytes = 10 * 1024 * 1024;
    public const long MaxSingleFileBytes = 1024 * 1024;
    public const int MaxFileCount = 200;

    private static readonly string[] IgnoredDirectories = ["bin", "obj", ".vs", ".git", "node_modules"];
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".slnx", ".json", ".xml", ".config", ".md", ".txt", ".props", ".targets", ".razor", ".cshtml"
    };

    public async Task<IReadOnlyList<ImportedProjectFileDto>> ExtractFromZipAsync(
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        Contracts.Require(zipStream != null, "Zip archive stream cannot be null.");

        using var memoryStream = new MemoryStream();
        await zipStream.CopyToAsync(memoryStream, cancellationToken);
        Contracts.Require(memoryStream.Length > 0, "Zip archive cannot be empty.");
        Contracts.Require(memoryStream.Length <= MaxArchiveBytes, $"Zip archive exceeds maximum allowed size of {MaxArchiveBytes / (1024 * 1024)} MB.");

        memoryStream.Position = 0;
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);

        var results = new List<ImportedProjectFileDto>();
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ShouldSkipZipEntry(entry))
            {
                continue;
            }

            Contracts.Require(!entry.FullName.Contains("..", StringComparison.Ordinal), $"Archive entry '{entry.FullName}' contains disallowed path traversal.");
            Contracts.Require(entry.Length <= MaxSingleFileBytes, $"Archive entry '{entry.FullName}' exceeds maximum allowed file size.");
            Contracts.Require(results.Count < MaxFileCount, $"Archive exceeds maximum allowed file count of {MaxFileCount}.");

            var normalizedPath = new ExamFilePath(entry.FullName);
            await using var entryStream = await entry.OpenAsync(cancellationToken);
            using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string content = await reader.ReadToEndAsync(cancellationToken);

            results.Add(new ImportedProjectFileDto
            {
                RelativePath = normalizedPath.Value,
                Content = content,
                SuggestedRole = SuggestRole(normalizedPath.Value)
            });
        }

        Contracts.Require(results.Count > 0, "Zip archive did not contain any supported source or project files.");
        return results;
    }

    public async Task<IReadOnlyList<ImportedProjectFileDto>> CloneFromGitAsync(
        string repositoryUrl,
        string branchOrRef = null,
        CancellationToken cancellationToken = default)
    {
        var uri = ValidateGitUri(repositoryUrl);
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"guts-exam-import-{Guid.NewGuid():N}");

        try
        {
            await RunGitCloneAsync(uri, branchOrRef, tempDirectory, cancellationToken);
            return await ReadDirectoryFilesAsync(tempDirectory, cancellationToken);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static bool ShouldSkipZipEntry(ZipArchiveEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith('/'))
        {
            return true;
        }

        if (entry.FullName.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (HasIgnoredDirectorySegment(entry.FullName))
        {
            return true;
        }

        return !AllowedExtensions.Contains(Path.GetExtension(entry.Name));
    }

    private static bool HasIgnoredDirectorySegment(string relativePath)
    {
        var segments = relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment =>
            IgnoredDirectories.Any(ignored => string.Equals(segment, ignored, StringComparison.OrdinalIgnoreCase)));
    }

    private static ExamFileRole SuggestRole(string relativePath)
    {
        string fileName = Path.GetFileName(relativePath);
        string extension = Path.GetExtension(fileName);

        if (!string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase))
        {
            return ExamFileRole.ReadOnly;
        }

        if (relativePath.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            return ExamFileRole.PublicTest;
        }

        return ExamFileRole.Editable;
    }

    private static Uri ValidateGitUri(string repositoryUrl)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(repositoryUrl), "Git repository URL cannot be empty.");
        Contracts.Require(
            Uri.TryCreate(repositoryUrl.Trim(), UriKind.Absolute, out var uri) && uri != null,
            "Git repository URL is invalid.");
        Contracts.Require(
            string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase),
            "Only HTTPS Git repository URLs are permitted.");
        Contracts.Require(!IsDisallowedHost(uri.Host), "Git repository host is not permitted.");
        return uri;
    }

    private static bool IsDisallowedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host) ||
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var ipAddress))
        {
            return false;
        }

        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        byte[] bytes = ipAddress.GetAddressBytes();
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254);
        }

        return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal;
    }

    private static async Task RunGitCloneAsync(
        Uri repositoryUri,
        string branchOrRef,
        string targetDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("clone");
        startInfo.ArgumentList.Add("--depth");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("--single-branch");
        if (!string.IsNullOrWhiteSpace(branchOrRef))
        {
            startInfo.ArgumentList.Add("--branch");
            startInfo.ArgumentList.Add(branchOrRef.Trim());
        }

        startInfo.ArgumentList.Add(repositoryUri.AbsoluteUri);
        startInfo.ArgumentList.Add(targetDirectory);

        using var process = Process.Start(startInfo);
        Contracts.Require(process != null, "Unable to start git process.");
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);
            Contracts.Require(false, $"Git clone failed: {error.Trim()}");
        }
    }

    private static async Task<IReadOnlyList<ImportedProjectFileDto>> ReadDirectoryFilesAsync(
        string rootDirectory,
        CancellationToken cancellationToken)
    {
        var results = new List<ImportedProjectFileDto>();
        foreach (string fullPath in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(rootDirectory, fullPath).Replace('\\', '/');
            if (HasIgnoredDirectorySegment(relativePath) || !AllowedExtensions.Contains(Path.GetExtension(fullPath)))
            {
                continue;
            }

            var fileInfo = new FileInfo(fullPath);
            Contracts.Require(fileInfo.Length <= MaxSingleFileBytes, $"File '{relativePath}' exceeds maximum allowed file size.");
            Contracts.Require(results.Count < MaxFileCount, $"Repository exceeds maximum allowed file count of {MaxFileCount}.");

            var examPath = new ExamFilePath(relativePath);
            string content = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken);
            results.Add(new ImportedProjectFileDto
            {
                RelativePath = examPath.Value,
                Content = content,
                SuggestedRole = SuggestRole(examPath.Value)
            });
        }

        Contracts.Require(results.Count > 0, "Repository did not contain any supported source or project files.");
        return results;
    }
}
