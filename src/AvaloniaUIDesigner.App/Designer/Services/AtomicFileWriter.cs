using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaUIDesigner.App.Designer.Services;

public static class AtomicFileWriter
{
    public static async Task WriteAllTextAsync(string path, string content)
    {
        var targetPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(targetPath)
            ?? throw new IOException("The target path does not have a directory.");
        var fileName = Path.GetFileName(targetPath);
        var temporaryPath = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");

        Directory.CreateDirectory(directory);

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
                {
                    await writer.WriteAsync(content);
                    await writer.FlushAsync();
                }

                await stream.FlushAsync();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(targetPath))
            {
                File.Replace(temporaryPath, targetPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
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
