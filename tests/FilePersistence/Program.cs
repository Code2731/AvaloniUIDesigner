using AvaloniaUIDesigner.App.Designer.Services;

var directory = Path.Combine(Path.GetTempPath(), $"AvaloniaUIDesigner-save-{Guid.NewGuid():N}");
Directory.CreateDirectory(directory);
try
{
    var target = Path.Combine(directory, "nested", "MainView.axaml");
    var backup = target + ".bak";
    const string original = "<TextBlock Text=\"\uD55C\uAE00 \U0001F600\" />";
    await AtomicFileWriter.WriteAllTextAsync(target, original, backup);
    Assert(File.ReadAllText(target) == original, "First save must preserve Unicode text.");
    Assert(!File.Exists(backup), "First save must not invent a previous version.");
    var bytes = File.ReadAllBytes(target);
    Assert(!(bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf),
        "AXAML saves must use UTF-8 without a BOM.");

    await AtomicFileWriter.WriteAllTextAsync(target, "second", backup);
    Assert(File.ReadAllText(target) == "second" && File.ReadAllText(backup) == original,
        "Replacement must back up the exact previous version.");
    await AtomicFileWriter.WriteAllTextAsync(target, "third", backup);
    Assert(File.ReadAllText(target) == "third" && File.ReadAllText(backup) == "second",
        "Repeated saves must advance the recovery snapshot.");

    foreach (var invalidText in new[] { "invalid\uD800", "\uDC00invalid", new string('a', 8192) + "\uD800" })
    {
        var encodingFailed = false;
        try { await AtomicFileWriter.WriteAllTextAsync(target, invalidText, backup); }
        catch (System.Text.EncoderFallbackException) { encodingFailed = true; }
        Assert(encodingFailed && File.ReadAllText(target) == "third" && File.ReadAllText(backup) == "second",
            "Malformed Unicode must be rejected without changing the document or backup.");
        Assert(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories).Any(),
            "Encoding failure must clean up even partially written temporary files.");
    }

    var rejected = false;
    try { await AtomicFileWriter.WriteAllTextAsync(target, "invalid", target); }
    catch (ArgumentException) { rejected = true; }
    Assert(rejected && File.ReadAllText(target) == "third", "A self-backup must fail without touching the document.");

    var blockedBackup = Path.Combine(directory, "backup-is-directory");
    Directory.CreateDirectory(blockedBackup);
    var failed = false;
    try { await AtomicFileWriter.WriteAllTextAsync(target, "must not commit", blockedBackup); }
    catch (IOException) { failed = true; }
    catch (UnauthorizedAccessException) { failed = true; }
    Assert(failed && File.ReadAllText(target) == "third" && File.ReadAllText(backup) == "second",
        "Failed replacement must retain the document and its existing recovery snapshot.");
    Assert(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories).Any(),
        "Both successful and failed saves must clean up temporary files.");

    await AtomicFileWriter.WriteAllTextAsync(target, "recovered", backup);
    Assert(File.ReadAllText(target) == "recovered" && File.ReadAllText(backup) == "third",
        "A corrected save must succeed after a failed replacement.");
    Console.WriteLine("File persistence regression checks passed.");
}
finally
{
    Directory.Delete(directory, recursive: true);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
