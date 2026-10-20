using System.IO;
using Sight.Logging.Loggers;

namespace Sight.Logging.Tests;

[TestFixture]
public class FileRotationTests
{
    private string _directory = null!;
    private string _path = null!;
    private DateTime _now;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "sight-logging-tests-" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "app.log");
        _now = new DateTime(2024, 3, 10, 23, 59, 0);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Test]
    public void MaxFileSize_RollsTheFileBeforeExceedingIt()
    {
        var line = new string('x', 18); // 20 bytes with the line break on Windows, 19 elsewhere
        using (var logger = Create(o => o.MaxFileSize = 50))
        {
            for (var i = 0; i < 6; i++)
            {
                logger.Log(line);
                _now = _now.AddSeconds(1);
            }
        }

        var rolled = RolledFiles();
        Assert.Greater(rolled.Length, 1);
        foreach (var file in rolled.Concat(new[] { _path }))
            Assert.LessOrEqual(new FileInfo(file).Length, 50, file);

        var total = rolled.Concat(new[] { _path }).Sum(f => File.ReadAllLines(f).Length);
        Assert.AreEqual(6, total);
    }

    [Test]
    public void MaxFileSize_MessageBiggerThanTheLimit_IsStillWritten()
    {
        using (var logger = Create(o => o.MaxFileSize = 10))
        {
            logger.Log(new string('x', 100));
            logger.Log(new string('y', 100));
        }

        var all = RolledFiles().Concat(new[] { _path }).SelectMany(File.ReadAllLines).ToArray();
        Assert.AreEqual(2, all.Length);
    }

    [Test]
    public void MaxFiles_DeletesTheOldestRolledFiles()
    {
        using (var logger = Create(o =>
               {
                   o.MaxFileSize = 30;
                   o.MaxFiles = 2;
               }))
        {
            for (var i = 0; i < 10; i++)
            {
                logger.Log("message number " + i);
                _now = _now.AddSeconds(1);
            }
        }

        var rolled = RolledFiles();
        Assert.AreEqual(2, rolled.Length);
        var names = rolled.Select(Path.GetFileName).OrderBy(n => n).ToArray();
        StringAssert.Contains("message number 8", string.Join("|", File.ReadAllLines(_path).Concat(rolled.SelectMany(File.ReadAllLines))));
        Assert.IsTrue(names.All(n => n!.StartsWith("app.2024")));
    }

    [Test]
    public void MaxFiles_OnlyDeletesFilesOfTheLogger()
    {
        Directory.CreateDirectory(_directory);
        var foreign = Path.Combine(_directory, "app.notes.log");
        File.WriteAllText(foreign, "keep");

        using (var logger = Create(o =>
               {
                   o.MaxFileSize = 30;
                   o.MaxFiles = 0;
               }))
        {
            for (var i = 0; i < 5; i++)
            {
                logger.Log("message number " + i);
                _now = _now.AddSeconds(1);
            }
        }

        Assert.IsTrue(File.Exists(foreign));
        Assert.AreEqual(0, RolledFiles().Length);
    }

    [Test]
    public void Daily_RollsWhenTheDayChanges()
    {
        using (var logger = Create(o => o.RollInterval = FileRollInterval.Daily))
        {
            logger.Log("day one");
            _now = _now.AddMinutes(2);
            logger.Log("day two");
        }

        var rolled = RolledFiles();
        Assert.AreEqual(1, rolled.Length);
        Assert.AreEqual(new[] { "day one" }, File.ReadAllLines(rolled[0]));
        Assert.AreEqual(new[] { "day two" }, File.ReadAllLines(_path));
        StringAssert.StartsWith("app.20240311-000100", Path.GetFileName(rolled[0]));
    }

    [Test]
    public void Hourly_RollsWhenTheHourChanges()
    {
        using (var logger = Create(o => o.RollInterval = FileRollInterval.Hourly))
        {
            logger.Log("a");
            _now = _now.AddMinutes(30);
            logger.Log("b");
            logger.Log("c");
        }

        Assert.AreEqual(1, RolledFiles().Length);
        Assert.AreEqual(new[] { "b", "c" }, File.ReadAllLines(_path));
    }

    [Test]
    public void Daily_SameDay_DoesNotRoll()
    {
        using (var logger = Create(o => o.RollInterval = FileRollInterval.Daily))
        {
            logger.Log("a");
            _now = _now.AddSeconds(30);
            logger.Log("b");
        }

        Assert.AreEqual(0, RolledFiles().Length);
    }

    [Test]
    public void Roll_SameSecond_DoesNotOverwriteAnExistingRolledFile()
    {
        using (var logger = Create(o => o.MaxFileSize = 12))
        {
            for (var i = 0; i < 4; i++)
                logger.Log("0123456789"); // the clock never moves
        }

        var all = RolledFiles().Concat(new[] { _path }).SelectMany(File.ReadAllLines).ToArray();
        Assert.AreEqual(4, all.Length);
    }

    [Test]
    public void Encoding_IsUsedForTheFile()
    {
        using (var logger = Create(o => o.Encoding = System.Text.Encoding.Unicode))
            logger.Log("héllo");

        var bytes = File.ReadAllBytes(_path);
        Assert.AreEqual(0xFF, bytes[0]);
        Assert.AreEqual(0xFE, bytes[1]);
        StringAssert.Contains("héllo", System.Text.Encoding.Unicode.GetString(bytes));
    }

    [Test]
    public void Encoding_Default_IsUtf8WithoutBom()
    {
        using (var logger = Create(_ => { }))
            logger.Log("héllo");

        var bytes = File.ReadAllBytes(_path);
        Assert.AreNotEqual(0xEF, bytes[0]);
        StringAssert.Contains("héllo", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Test]
    public void Constructor_InvalidOptions_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileLogger(_path, new FileLoggerOptions { MaxFileSize = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileLogger(_path, new FileLoggerOptions { MaxFiles = -1 }));
    }

    [Test]
    public void BackgroundMode_Rolls()
    {
        using (var logger = Create(o =>
               {
                   o.MaxFileSize = 40;
                   o.CreateThread = true;
               }))
        {
            for (var i = 0; i < 20; i++)
                logger.Log("message number " + i);
        }

        var all = RolledFiles().Concat(new[] { _path }).SelectMany(File.ReadAllLines).ToArray();
        Assert.AreEqual(20, all.Length);
    }

    private FileLogger Create(Action<FileLoggerOptions> configure)
    {
        var options = new FileLoggerOptions { TimestampFormat = null, Clock = () => _now };
        configure(options);
        return new FileLogger(_path, options);
    }

    private string[] RolledFiles()
    {
        return Directory.GetFiles(_directory, "app.2*.log").OrderBy(f => f, StringComparer.Ordinal).ToArray();
    }
}
