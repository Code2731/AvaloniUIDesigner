using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaUIDesigner.App.Designer.Services;

public static class AtomicFileWriter
{
    public static Task WriteAllTextAsync(string path, string content)
        => WriteAllTextAsync(path, content, backupPath: null);

    public static async Task WriteAllTextAsync(string path, string content, string? backupPath)
    {
        var targetPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(targetPath)
            ?? throw new IOException("The target path does not have a directory.");
        var backupTargetPath = string.IsNullOrWhiteSpace(backupPath)
            ? null
            : Path.GetFullPath(backupPath);
        if (string.Equals(targetPath, backupTargetPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The backup path must differ from the target path.", nameof(backupPath));
        }

        var fileName = Path.GetFileName(targetPath);
        var temporaryPath = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");

        Directory.CreateDirectory(directory);
        if (backupTargetPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupTargetPath)!);
        }

        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.Asynchronous))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
                {
                    await writer.WriteAsync(content);
                    writer.Flush();
                }

                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(targetPath))
            {
                File.Replace(
                    temporaryPath,
                    targetPath,
                    destinationBackupFileName: backupTargetPath,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, targetPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
