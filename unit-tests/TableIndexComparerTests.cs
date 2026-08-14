using Diffinity.HtmlHelper;
using Diffinity.TableHelper;

public class TableIndexComparerTests
{
    [Fact]
    public void AreEqual_ReturnsFalse_WhenIndexDefinitionChanges()
    {
        var source = new List<IndexDto>
        {
            new() { IndexName = "IX_Test", IndexType = "NONCLUSTERED", IsUnique = false, KeyColumns = "[Name] ASC" }
        };

        var destination = new List<IndexDto>
        {
            new() { IndexName = "IX_Test", IndexType = "NONCLUSTERED", IsUnique = true, KeyColumns = "[Name] ASC" }
        };

        Assert.False(TableIndexComparer.AreEqual(source, destination));
    }

    [Fact]
    public void GetDifferenceMarkers_FlagsMissingAndChangedIndexes()
    {
        var source = new List<IndexDto>
        {
            new() { IndexName = "IX_OnlyInSource", IndexType = "NONCLUSTERED", KeyColumns = "[ColA] ASC" },
            new() { IndexName = "IX_Changed", IndexType = "NONCLUSTERED", KeyColumns = "[ColB] ASC" }
        };

        var destination = new List<IndexDto>
        {
            new() { IndexName = "IX_Changed", IndexType = "NONCLUSTERED", KeyColumns = "[ColB] DESC" },
            new() { IndexName = "IX_OnlyInDest", IndexType = "NONCLUSTERED", KeyColumns = "[ColC] ASC" }
        };

        var markers = TableIndexComparer.GetDifferenceMarkers(source, destination);

        Assert.Contains(TableIndexComparer.ToMarker("IX_OnlyInSource"), markers);
        Assert.Contains(TableIndexComparer.ToMarker("IX_Changed"), markers);
        Assert.Contains(TableIndexComparer.ToMarker("IX_OnlyInDest"), markers);
    }

    [Fact]
    public void CreateAlterTableScript_IncludesDropAndCreateIndexStatements()
    {
        var sourceColumns = new List<tableDto>
        {
            new() { columnName = "Id", columnType = "int", isNullable = "NO", isPrimaryKey = "YES" }
        };

        var targetColumns = new List<tableDto>
        {
            new() { columnName = "Id", columnType = "int", isNullable = "NO", isPrimaryKey = "YES" }
        };

        var sourceIndexes = new List<IndexDto>
        {
            new() { IndexName = "IX_Old", IndexType = "NONCLUSTERED", KeyColumns = "[Id] ASC" }
        };

        var targetIndexes = new List<IndexDto>
        {
            new() { IndexName = "IX_New", IndexType = "NONCLUSTERED", KeyColumns = "[Id] ASC", IncludedColumns = "[Name]" }
        };

        var script = HtmlReportWriter.CreateAlterTableScript("dbo", "MyTable", sourceColumns, targetColumns, null, null, sourceIndexes, targetIndexes);

        Assert.Contains("DROP INDEX [IX_Old] ON [dbo].[MyTable];", script);
        Assert.Contains("CREATE NONCLUSTERED INDEX [IX_New] ON [dbo].[MyTable] ([Id] ASC) INCLUDE ([Name]);", script);
    }

    [Fact]
    public void PrintTableInfo_ShowsIndexCopyButtonAndUsesCreateStatement()
    {
        var columns = new List<tableDto>
        {
            new() { columnName = "Id", columnType = "int", isNullable = "NO", isPrimaryKey = "YES" }
        };

        var indexes = new List<IndexDto>
        {
            new() { IndexName = "IX_CopyMe", IndexType = "NONCLUSTERED", KeyColumns = "[Id] ASC" }
        };

        var html = HtmlReportWriter.PrintTableInfo("dbo", "MyTable", columns, new List<string>(), indexes);

        Assert.Contains("IX_CopyMe", html);
        Assert.Contains("copy-btn-small", html);
        Assert.Contains("CREATE NONCLUSTERED INDEX [IX_CopyMe] ON [dbo].[MyTable] ([Id] ASC);", html);
    }
}
