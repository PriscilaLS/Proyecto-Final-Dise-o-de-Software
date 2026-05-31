using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ClientLocal.Services.Files
{
    public class CompressionService
    {
        private static readonly HashSet<string> IgnoredFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".vs",
            ".idea",
            ".vscode",
            "bin",
            "obj",
            "node_modules",
            "packages",
            "__pycache__"
        };

        public string CreateProjectZip(string projectFolderPath)
        {
            if (!Directory.Exists(projectFolderPath))
                throw new DirectoryNotFoundException("La carpeta del proyecto no existe.");

            var zipPath = Path.Combine(Path.GetTempPath(), $"eduide_{Guid.NewGuid()}.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            var addedFiles = 0;

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

            foreach (var filePath in EnumerateProjectFiles(projectFolderPath))
            {
                var relativePath = Path.GetRelativePath(projectFolderPath, filePath)
                    .Replace(Path.DirectorySeparatorChar, '/');

                archive.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Fastest);
                addedFiles++;
            }

            if (addedFiles == 0)
                throw new InvalidOperationException("No se encontraron archivos validos para comprimir.");

            return zipPath;
        }

        private static IEnumerable<string> EnumerateProjectFiles(string folder)
        {
            var pending = new Stack<string>();
            pending.Push(folder);

            while (pending.Count > 0)
            {
                var current = pending.Pop();

                IEnumerable<string> directories = Enumerable.Empty<string>();
                IEnumerable<string> files = Enumerable.Empty<string>();

                try
                {
                    directories = Directory.EnumerateDirectories(current)
                        .Where(ShouldIncludeDirectory)
                        .ToArray();

                    files = Directory.EnumerateFiles(current)
                        .Where(ShouldIncludeFile)
                        .ToArray();
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var directory in directories)
                    pending.Push(directory);

                foreach (var file in files)
                    yield return file;
            }
        }

        private static bool ShouldIncludeDirectory(string path)
        {
            var name = Path.GetFileName(path);
            if (IgnoredFolders.Contains(name))
                return false;

            try
            {
                var attributes = File.GetAttributes(path);
                return !attributes.HasFlag(FileAttributes.ReparsePoint);
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldIncludeFile(string path)
        {
            try
            {
                var attributes = File.GetAttributes(path);
                return !attributes.HasFlag(FileAttributes.Hidden)
                    && !attributes.HasFlag(FileAttributes.System)
                    && !attributes.HasFlag(FileAttributes.ReparsePoint);
            }
            catch
            {
                return false;
            }
        }
    }
}
