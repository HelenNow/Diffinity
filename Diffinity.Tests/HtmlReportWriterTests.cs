using Diffinity;
using Diffinity.HtmlHelper;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using static Diffinity.DbComparer;
using static Diffinity.HtmlHelper.HtmlReportWriter;

public class HtmlReportWriterTests
{
    private readonly string _tempFolder;

    public HtmlReportWriterTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
    }

    [Fact]
    public void WriteIndexSummary_CreatesFile()
    {
        string path = WriteIndexSummary(
            "Server=Src;Database=SrcDb;",
            "Server=Dest;Database=DestDb;",
            _tempFolder,
            ignoredIndexPath: "Ignored/index.html",
            procIndexPath: "Procedures/index.html",
            viewIndexPath: "Views/index.html",
            tableIndexPath: "Tables/index.html"
        );

        Assert.True(File.Exists(path));
        string content = File.ReadAllText(path);
        Assert.Contains("Database Comparison Summary", content);
        Assert.Contains("Procedures", content);
        Assert.Contains("Views", content);
        Assert.Contains("Tables", content);
        Assert.Contains("Ignored", content);
    }

    [Fact]
    public void WriteBodyHtml_CreatesFileWithTitle()
    {
        string filePath = Path.Combine(_tempFolder, "body.html");
        string title = "TestProcedure";
        string body = "SELECT 1";
        WriteBodyHtml(filePath, title, body, "return.html");

        Assert.True(File.Exists(filePath));
        string content = File.ReadAllText(filePath);
        Assert.Contains(title, content);
        Assert.Contains(body, content);
        Assert.Contains("Return to Summary", content);
    }

    [Fact]
    public void WriteIgnoredReport_CreatesFileWithIgnoredObjects()
    {
        var ignored = new HashSet<string> { "Obj1", "Obj2" };
        var report = HtmlReportWriter.WriteIgnoredReport(_tempFolder, ignored, Run.Proc);

        Assert.True(File.Exists(report.fullPath));
        string html = File.ReadAllText(report.fullPath);
        Assert.Contains("Ignored Objects", html);
        Assert.Contains("Obj1", html);
        Assert.Contains("Obj2", html);
    }
}
