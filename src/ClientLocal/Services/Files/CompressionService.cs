using System;
using System.IO;
using System.IO.Compression;

namespace ClientLocal.Services.Files
{
    public class CompressionService
    {
        public string CreateProjectZip(string projectFolderPath)
        {
            if (!Directory.Exists(projectFolderPath))
                throw new DirectoryNotFoundException("La carpeta del proyecto no existe.");

            var zipPath = Path.Combine(Path.GetTempPath(), $"eduide_{Guid.NewGuid()}.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(projectFolderPath, zipPath, CompressionLevel.Optimal, false);
            return zipPath;
        }
    }
}