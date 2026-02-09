using Diffinity.HtmlHelper;
using Diffinity.ProcHelper;
using Diffinity.TableHelper;
using Diffinity.UdtHelper;
using Diffinity.ViewHelper;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static Diffinity.DbComparer;
using Diffinity.FunctionHelper;

namespace Diffinity;
public enum ComparerAction
{
    ApplyChanges,
    DoNotApplyChanges
}
public enum DbObjectFilter
{
    ShowUnchanged,
    HideUnchanged
}
public enum Run
{
    Proc,
    View,
    Table,
    Udt,
    Function,
    ProcView,
    ProcTable,
    ViewTable,
    All
}
public record DbServer(string name, string connectionString);
public class DbComparer : DbObjectHandler
{

    static readonly string _outputFolder = @"Diffinity-output";

    static DbComparer()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
    public static string Compare(DbServer sourceServer, DbServer destinationServer, int threadCount = 4, ILogger? logger = null, string? outputFolder = null, ComparerAction? makeChange = ComparerAction.DoNotApplyChanges, DbObjectFilter? filter = DbObjectFilter.HideUnchanged, Run? run = Run.All)
    {
        /// <summary>
        /// Executes comparison of database object types based on the specified Run option and returns the corresponding summary report.
        /// </summary>
        if (outputFolder == null) { outputFolder = _outputFolder; }
        if (logger == null) { logger = Log.Logger; }

        var ignoredObjects = DiffIgnoreLoader.LoadIgnoredObjects();
        var objectTags = DiffTagsLoader.LoadObjectTags();
        var tagColors = DiffTagColorsLoader.LoadTagColors();
        summaryReportDto ignoredReport = !ignoredObjects.Any() ? new summaryReportDto() : HtmlReportWriter.WriteIgnoredReport(outputFolder, ignoredObjects, run.Value);
        summaryReportDto ProcReport;
        summaryReportDto ViewReport;
        summaryReportDto TableReport;
        summaryReportDto UdtReport;

        var sw = new Stopwatch();

        switch (run)
        {
            case Run.Proc:
                {
                    sw.Start();
                    ProcReport = CompareProcs(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(ProcReport.fullPath, ProcReport.html.Replace("{procsCount}", ProcReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{procsCount}", ProcReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, procIndexPath: ProcReport.path, procCount: ProcReport.diffsCount, procsCountText: ProcReport.count);
                }
            case Run.View:
                {
                    sw.Start();
                    ViewReport = CompareViews(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(ViewReport.fullPath, ViewReport.html.Replace("{viewsCount}", ViewReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{viewsCount}", ViewReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, viewIndexPath: ViewReport.path, viewCount: ViewReport.diffsCount, viewsCountText: ViewReport.count);
                }
            case Run.Table:
                {
                    sw.Start();
                    TableReport = CompareTables(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(TableReport.fullPath, TableReport.html.Replace("{tablesCount}", TableReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{tablesCount}", TableReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, tableIndexPath: TableReport.path, tableCount: TableReport.diffsCount, tablesCountText: TableReport.count);
                }
            case Run.Udt:
                {
                    sw.Start();
                    UdtReport = CompareUdts(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(UdtReport.fullPath, UdtReport.html.Replace("{udtsCount}", UdtReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{udtsCount}", UdtReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, udtIndexPath: UdtReport.path, udtCount: UdtReport.diffsCount, udtsCountText: UdtReport.count);
                }
            case Run.Function:
                {
                    sw.Start();
                    var FunctionReport = CompareFunctions(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(FunctionReport.fullPath, FunctionReport.html.Replace("{functionsCount}", FunctionReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{functionsCount}", FunctionReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, functionIndexPath: FunctionReport.path, functionCount: FunctionReport.diffsCount, functionsCountText: FunctionReport.count);
                }
            case Run.ProcView:
                {
                    sw.Start();
                    ProcReport = CompareProcs(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    ViewReport = CompareViews(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(ProcReport.fullPath, ProcReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count));
                    File.WriteAllText(ViewReport.fullPath, ViewReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, procIndexPath: ProcReport.path, viewIndexPath: ViewReport.path, procCount: ProcReport.diffsCount, viewCount: ViewReport.diffsCount, procsCountText: ProcReport.count, viewsCountText: ViewReport.count);
                }
            case Run.ProcTable:
                {
                    sw.Start();
                    ProcReport = CompareProcs(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    TableReport = CompareTables(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(ProcReport.fullPath, ProcReport.html.Replace("{procsCount}", ProcReport.count).Replace("{tablesCount}", TableReport.count));
                    File.WriteAllText(TableReport.fullPath, TableReport.html.Replace("{procsCount}", ProcReport.count).Replace("{tablesCount}", TableReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{procsCount}", ProcReport.count).Replace("{tablesCount}", TableReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, procIndexPath: ProcReport.path, tableIndexPath: TableReport.path, procCount: ProcReport.diffsCount, tableCount: TableReport.diffsCount, procsCountText: ProcReport.count, tablesCountText: TableReport.count);
                }
            case Run.ViewTable:
                {
                    sw.Start();
                    ViewReport = CompareViews(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    TableReport = CompareTables(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(ViewReport.fullPath, ViewReport.html.Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count));
                    File.WriteAllText(TableReport.fullPath, TableReport.html.Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, viewIndexPath: ViewReport.path, tableIndexPath: TableReport.path, viewCount: ViewReport.diffsCount, tableCount: TableReport.diffsCount, viewsCountText: ViewReport.count, tablesCountText: TableReport.count);
                }
            case Run.All:
                {
                    sw.Start();
                    ProcReport = CompareProcs(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    ViewReport = CompareViews(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    TableReport = CompareTables(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    UdtReport = CompareUdts(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    var FunctionReport = CompareFunctions(sourceServer, destinationServer, outputFolder, makeChange.Value, filter.Value, run.Value, ignoredObjects, objectTags, tagColors, threadCount);
                    File.WriteAllText(ProcReport.fullPath, ProcReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count).Replace("{udtsCount}", UdtReport.count).Replace("{functionsCount}", FunctionReport.count));
                    File.WriteAllText(ViewReport.fullPath, ViewReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count).Replace("{udtsCount}", UdtReport.count).Replace("{functionsCount}", FunctionReport.count));
                    File.WriteAllText(TableReport.fullPath, TableReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count).Replace("{udtsCount}", UdtReport.count).Replace("{functionsCount}", FunctionReport.count));
                    File.WriteAllText(UdtReport.fullPath, UdtReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count).Replace("{udtsCount}", UdtReport.count).Replace("{functionsCount}", FunctionReport.count));
                    File.WriteAllText(FunctionReport.fullPath, FunctionReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count).Replace("{udtsCount}", UdtReport.count).Replace("{functionsCount}", FunctionReport.count));
                    if (ignoredObjects.Any()) File.WriteAllText(ignoredReport.fullPath, ignoredReport.html.Replace("{procsCount}", ProcReport.count).Replace("{viewsCount}", ViewReport.count).Replace("{tablesCount}", TableReport.count).Replace("{udtsCount}", UdtReport.count).Replace("{functionsCount}", FunctionReport.count));
                    sw.Stop();
                    return HtmlReportWriter.WriteIndexSummary(sourceServer, destinationServer, outputFolder, sw.ElapsedMilliseconds, ignoredReport.path, procIndexPath: ProcReport.path, viewIndexPath: ViewReport.path, tableIndexPath: TableReport.path, udtIndexPath: UdtReport.path, functionIndexPath: FunctionReport.path, procCount: ProcReport.diffsCount, viewCount: ViewReport.diffsCount, tableCount: TableReport.diffsCount, udtCount: UdtReport.diffsCount, functionCount: FunctionReport.diffsCount, procsCountText: ProcReport.count, viewsCountText: ViewReport.count, tablesCountText: TableReport.count, udtsCountText: UdtReport.count, functionsCountText: FunctionReport.count);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(run), run, "Invalid Run option");
        }

    }
    public static summaryReportDto CompareProcs(DbServer sourceServer, DbServer destinationServer, string outputFolder, ComparerAction makeChange, DbObjectFilter filter, Run run, HashSet<string> ignoredObjects, Dictionary<string, List<string>> objectTags, Dictionary<string, string> tagColors, int threadCount)
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = threadCount };
        /// <summary>
        /// Compares stored procedures between source and destination databases.
        /// Generates HTML reports for differences and optionally applies updates to the destination.
        /// </summary>

        // Step 1 - Setup folder structure for reports
        Directory.CreateDirectory(outputFolder);
        string proceduresFolderPath = Path.Combine(outputFolder, "Procedures");
        Directory.CreateDirectory(proceduresFolderPath);

        //Step 2 - Check if ignored is empty
        bool isIgnoredEmpty = !ignoredObjects.Any() ? true : false;
        string ignoredCount = ignoredObjects.Count.ToString();

        // Step 3 - Retrieve procedure names from the source server
        List<(string schema, string name)> procedures = ProcedureFetcher.GetProcedureNames(sourceServer.connectionString).OrderBy(p => p.schema).ThenBy(p => p.name).ToList();

        List<dbObjectResult> results = new();

        Serilog.Log.Information("Procs:");

        // Step 4 - Loop over each procedure and compare
        Parallel.ForEach(procedures, parallelOptions, procTuple =>
        {
            string schema = procTuple.schema;
            string proc = procTuple.name;
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(proc);

            if (ignoredObjects.Any(ignore =>
            {
                var parts = ignore.Split('.');
                var safeParts = parts
                    .Select(part => part == "*" ? "*" : MakeSafe(part));
                var safeIgnore = string.Join(".", safeParts);

                if (ignore.EndsWith(".*"))
                    return safeIgnore == safeSchema + ".*";

                return safeIgnore == safeSchema + "." + safeName;
            }))
            {
                Log.Information($"{schema}.{proc}: Ignored");
                return;
            }

            string schemaFolder = Path.Combine(proceduresFolderPath, safeSchema);

            // Step 5 - Fetch definitions from both servers
            (string sourceBody, string destinationBody) = ProcedureFetcher.GetProcedureBody(sourceServer.connectionString, destinationServer.connectionString, schema, proc);
            var marker = @"--\s*diffinity\s*:\s*client(?:\s*-\s*|\s+)specific\b";
            bool isTenantSpecific =
                (!string.IsNullOrWhiteSpace(sourceBody) &&
                 Regex.IsMatch(sourceBody, marker, RegexOptions.IgnoreCase | RegexOptions.Multiline))||(!string.IsNullOrWhiteSpace(destinationBody) &&
                 Regex.IsMatch(destinationBody, marker, RegexOptions.IgnoreCase | RegexOptions.Multiline));
            bool areEqual = AreBodiesEqual(sourceBody, destinationBody);
            string change = areEqual ? "No changes" : "Changes detected";
            Serilog.Log.Information($"{schema}.{proc}: {change}");

            // Step 6 - Setup filenames and paths
            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string differencesFile = $"{safeName}_differences.html";
            string newFile = $"{safeName}_New.html";
            string returnPage = Path.Combine("..", "index.html");

            bool isDestinationEmpty = string.IsNullOrWhiteSpace(destinationBody);
            bool isVisible = false;
            bool isDifferencesVisible = false;

            // Step 7 - Write HTML reports if needed
            if ((areEqual && filter == DbObjectFilter.ShowUnchanged) || !areEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                string sourcePath = Path.Combine(schemaFolder, sourceFile);
                string destinationPath = Path.Combine(schemaFolder, destinationFile);
                HtmlReportWriter.WriteBodyHtml(sourcePath, $"{sourceServer.name}", sourceBody, returnPage);
                HtmlReportWriter.WriteBodyHtml(destinationPath, $"{destinationServer.name}", destinationBody, returnPage);

                if (!isDestinationEmpty && !areEqual)
                {
                    string differencesPath = Path.Combine(schemaFolder, differencesFile);
                    HtmlReportWriter.DifferencesWriter(differencesPath, sourceServer.name, destinationServer.name, sourceBody, destinationBody, "Differences", $"{schema}.{proc}", returnPage);
                    isDifferencesVisible = true;
                }
                isVisible = true;
            }

            // Step 8 - Apply changes to destination if instructed
            string destinationNewBody = destinationBody;
            bool wasAltered = false;

            if (!areEqual && makeChange == ComparerAction.ApplyChanges)
            {
                AlterDbObject(destinationServer.connectionString, sourceBody, destinationBody);
                (_, destinationNewBody) = ProcedureFetcher.GetProcedureBody(sourceServer.connectionString, destinationServer.connectionString, schema, proc);
                string newPath = Path.Combine(schemaFolder, newFile);
                HtmlReportWriter.WriteBodyHtml(newPath, $"New {destinationServer.name}", destinationNewBody, returnPage);
                wasAltered = true;
            }

            // Step 9 - Store result entry for summary
            var fullName = $"{schema}.{proc}";
            var tags = objectTags.GetValueOrDefault(fullName, new List<string>());
            results.Add(new dbObjectResult
            {
                Type = "Proc",
                Name = safeName,
                schema = safeSchema,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = areEqual,
                SourceBody = sourceBody,
                DestinationBody = isDestinationEmpty? null: destinationBody,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                DifferencesFile = isDifferencesVisible ? Path.Combine(safeSchema, differencesFile) : null,
                NewFile = wasAltered ? Path.Combine(safeSchema, newFile) : null,
                IsTenantSpecific = isTenantSpecific,
                Tags = tags
            });
        });

        // Step 10 - Generate summary report
        (string procReportHtml, string procCount) = HtmlReportWriter.WriteSummaryReport(sourceServer, destinationServer, Path.Combine(proceduresFolderPath, "index.html"), results, filter, run, isIgnoredEmpty, ignoredCount, tagColors);
        int procDiffsCount = filter == DbObjectFilter.HideUnchanged ? results.Count(r => (r.IsDestinationEmpty) || (!r.IsDestinationEmpty && !r.IsEqual)) : results.Count(); 
        return new summaryReportDto
        {
            path = "Procedures/index.html",
            fullPath = Path.Combine(proceduresFolderPath, "index.html"),
            html = procReportHtml,
            count = procCount,
            diffsCount = procDiffsCount

        };
    }
    public static summaryReportDto CompareViews(DbServer sourceServer, DbServer destinationServer, string outputFolder, ComparerAction makeChange, DbObjectFilter filter, Run run, HashSet<string> ignoredObjects, Dictionary<string, List<string>> objectTags, Dictionary<string, string> tagColors, int threadCount)
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = threadCount };
        /// <summary>
        /// Compares SQL views between source and destination databases.
        /// Generates HTML reports for differences and optionally applies updates to the destination.
        /// </summary>

        // Step 1 - Setup folder structure for reports
        Directory.CreateDirectory(outputFolder);
        string viewsFolderPath = Path.Combine(outputFolder, "Views");
        Directory.CreateDirectory(viewsFolderPath);

        //Step 2 - Check if ignored is empty
        bool isIgnoredEmpty = !ignoredObjects.Any() ? true : false;
        string ignoredCount = ignoredObjects.Count.ToString();

        // Step 3 - Retrieve view names from the source server
        List<(string schema, string name)> views = ViewFetcher.GetViewsNames(sourceServer.connectionString).OrderBy(v => v.schema).ThenBy(v => v.name).ToList();

        List<dbObjectResult> results = new();

        Serilog.Log.Information("Views:");

        // Step 4 - Loop over each view and compare
        Parallel.ForEach(views, parallelOptions, viewTuple =>
        {
            string schema = viewTuple.schema;
            string view = viewTuple.name;
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(view);
            if (ignoredObjects.Any(ignore =>
            {
                var parts = ignore.Split('.');
                var safeParts = parts
                    .Select(part => part == "*" ? "*" : MakeSafe(part));
                var safeIgnore = string.Join(".", safeParts);

                if (ignore.EndsWith(".*"))
                    return safeIgnore == safeSchema + ".*";

                return safeIgnore == safeSchema + "." + safeName;
            }))
            {
                Log.Information($"{schema}.{view}: Ignored");
                return;
            }

            string schemaFolder = Path.Combine(viewsFolderPath, safeSchema);

            // Step 5 - Fetch definitions from both servers
            (string sourceBody, string destinationBody) = ViewFetcher.GetViewBody(sourceServer.connectionString, destinationServer.connectionString, schema, view);
            var marker = @"--\s*diffinity\s*:\s*client(?:\s*-\s*|\s+)specific\b";
            bool isTenantSpecific =
                (!string.IsNullOrWhiteSpace(sourceBody) &&
                 Regex.IsMatch(sourceBody, marker, RegexOptions.IgnoreCase | RegexOptions.Multiline)) || (!string.IsNullOrWhiteSpace(destinationBody) &&
                 Regex.IsMatch(destinationBody, marker, RegexOptions.IgnoreCase | RegexOptions.Multiline));
            bool areEqual = AreBodiesEqual(sourceBody, destinationBody);
            string change = areEqual ? "No changes" : "Changes detected";
            Serilog.Log.Information($"{schema}.{view}: {change}");

            // Step 6 - Setup filenames and paths
            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string differencesFile = $"{safeName}_differences.html";
            string newFile = $"{safeName}_New.html";
            string returnPage = Path.Combine("..", "index.html");

            bool isDestinationEmpty = string.IsNullOrEmpty(destinationBody);
            bool isVisible = false;
            bool isDifferencesVisible = false;

            // Step 7 - Write HTML reports if needed
            if ((areEqual && filter == DbObjectFilter.ShowUnchanged) || !areEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                string sourcePath = Path.Combine(schemaFolder, sourceFile);
                string destinationPath = Path.Combine(schemaFolder, destinationFile);
                HtmlReportWriter.WriteBodyHtml(sourcePath, $"{sourceServer.name}", sourceBody, returnPage);
                HtmlReportWriter.WriteBodyHtml(destinationPath, $"{destinationServer.name}", destinationBody, returnPage);
                if (!isDestinationEmpty && !areEqual)
                {
                    string differencesPath = Path.Combine(schemaFolder, differencesFile);
                    HtmlReportWriter.DifferencesWriter(differencesPath, sourceServer.name, destinationServer.name, sourceBody, destinationBody, "Differences", $"{schema}.{view}", returnPage); isDifferencesVisible = true;
                }
                isVisible = true;
            }

            // Step 8 - Apply changes if instructed
            string destinationNewBody = destinationBody;
            bool wasAltered = false;

            if (!areEqual && makeChange == ComparerAction.ApplyChanges)
            {
                AlterDbObject(destinationServer.connectionString, sourceBody, destinationBody);
                (_, destinationNewBody) = ViewFetcher.GetViewBody(sourceServer.connectionString, destinationServer.connectionString, schema, view);
                string newPath = Path.Combine(schemaFolder, newFile);
                HtmlReportWriter.WriteBodyHtml(newPath, $"New {destinationServer.name}", destinationNewBody, returnPage);
                wasAltered = true;
            }

            // Step 9 - Store result entry for summary
            var fullName = $"{schema}.{view}";
            var tags = objectTags.GetValueOrDefault(fullName, new List<string>());
            results.Add(new dbObjectResult
            {
                Type = "View",
                Name = safeName,
                schema = safeSchema,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = areEqual,
                SourceBody = sourceBody,
                DestinationBody = isDestinationEmpty ? null : destinationBody,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                DifferencesFile = isDifferencesVisible ? Path.Combine(safeSchema, differencesFile) : null,
                NewFile = wasAltered ? Path.Combine(safeSchema, newFile) : null,
                IsTenantSpecific = isTenantSpecific,
                Tags = tags
            });
        });

        // Step 10 - Generate summary report
        (string viewReportHtml, string viewCount) = HtmlReportWriter.WriteSummaryReport(sourceServer, destinationServer, Path.Combine(viewsFolderPath, "index.html"), results, filter, run, isIgnoredEmpty, ignoredCount, tagColors);
        int viewDiffsCount = filter == DbObjectFilter.HideUnchanged ? results.Count(r => (r.IsDestinationEmpty) || (!r.IsDestinationEmpty && !r.IsEqual)) : results.Count(); // changed
        return new summaryReportDto
        {
            path = "Views/index.html",
            fullPath = Path.Combine(viewsFolderPath, "index.html"),
            html = viewReportHtml,
            count = viewCount,
            diffsCount = viewDiffsCount
        };
    }
    public static summaryReportDto CompareTables(DbServer sourceServer, DbServer destinationServer, string outputFolder, ComparerAction makeChange, DbObjectFilter filter, Run run, HashSet<string> ignoredObjects, Dictionary<string, List<string>> objectTags, Dictionary<string, string> tagColors, int threadCount)
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = threadCount };
        /// <summary>
        /// Compares table column definitions between source and destination databases.
        /// Generates HTML reports for schema differences and optionally applies updates to the destination.
        /// </summary>

        object resultsLock = new object();
        // Step 1 - Setup folder structure for reports
        Directory.CreateDirectory(outputFolder);
        string tablesFolderPath = Path.Combine(outputFolder, "Tables");
        Directory.CreateDirectory(tablesFolderPath);

        //Step 2 - Check if ignored is empty
        bool isIgnoredEmpty = !ignoredObjects.Any() ? true : false;
        string ignoredCount = ignoredObjects.Count().ToString();

        // Step 3 - Retrieve table names from the source server
        List<(string schema, string name)> tables = TableFetcher.GetTablesNames(sourceServer.connectionString).OrderBy(t => t.schema).ThenBy(t => t.name).ToList();

        List<dbObjectResult> results = new();

        Serilog.Log.Information("Tables:");

        // Step 4 - Loop over each table and compare
        Parallel.ForEach(tables, parallelOptions, tableTuple =>
        {
            bool isTableEqual = true;
            string schema = tableTuple.schema;
            string table = tableTuple.name;
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(table);

            if (ignoredObjects.Any(ignore =>
            {
                var parts = ignore.Split('.');
                var safeParts = parts.Select(part => part == "*" ? "*" : MakeSafe(part));
                var safeIgnore = string.Join(".", safeParts);
                if (ignore.EndsWith(".*")) return safeIgnore == safeSchema + ".*";
                return safeIgnore == safeSchema + "." + safeName;
            }))
            {
                Log.Information($"{schema}.{table}: Ignored");
                return;
            }
            string schemaFolder = Path.Combine(tablesFolderPath, safeSchema);

            // Step 5 - Fetch table column info
            List<string> allDifferences = new List<string>();
            (List<tableDto> sourceInfo, List<tableDto> destinationInfo,
             List<ForeignKeyDto> sourceFKs, List<ForeignKeyDto> destFKs) =
                TableFetcher.GetTableInfo(sourceServer.connectionString, destinationServer.connectionString, schema, table);

            bool isDestinationEmpty = destinationInfo.IsNullOrEmpty();
            int sourceColumnCount = sourceInfo.Count;
            int destinationColumnCount = destinationInfo.Count;
            int minCount = Math.Min(sourceColumnCount, destinationColumnCount);
            // Step 6 - Compare each column
            for (int i = 0; i < minCount; i++)
            {
                if (isDestinationEmpty)
                {
                    Serilog.Log.Information($"{table}: Changes detected");
                    allDifferences.Add(table);
                    isTableEqual = false; 
                    continue;
                }

                (bool isColEqual, List<string> differences) = TableComparerAndUpdater.ComparerAndUpdater(destinationServer.connectionString, sourceInfo[i], destinationInfo[i], table, makeChange);
                if (!isColEqual)
                {
                    isTableEqual = false;
                    allDifferences.AddRange(differences);
                    Serilog.Log.Information($"{schema}.{table}: Changes detected");
                }
            }
            // Handle extra columns in source
            if (sourceColumnCount > destinationColumnCount)
            {
                for (int i = destinationColumnCount; i < sourceColumnCount; i++)
                {
                    allDifferences.Add(sourceInfo[i].columnName);
                }
                isTableEqual = false;
            }
            // Handle extra columns in destination
            if (destinationColumnCount > sourceColumnCount)
            {
                for (int i = sourceColumnCount; i < destinationColumnCount; i++)
                {
                    allDifferences.Add(destinationInfo[i].columnName);
                }
                isTableEqual = false;
            }

            if (isTableEqual && !isDestinationEmpty)
            {
                Serilog.Log.Information($"{schema}.{table}: No Changes");
            }

            // Step 7 - Setup filenames and paths
            string differencesFile = $"{safeName}_differences.html";
            bool isDifferencesVisible = false;

            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string newFile = $"{safeName}_New.html";
            string returnPage = Path.Combine("..", "index.html");

            bool isVisible = false;

            // Step 8 - Write HTML reports if needed
            if ((isTableEqual && filter == DbObjectFilter.ShowUnchanged) || !isTableEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                string sourcePath = Path.Combine(schemaFolder, sourceFile);
                string destinationPath = Path.Combine(schemaFolder, destinationFile);

                // destination empty: use CREATE script
                // both exist: use ALTER script
                string sourceTableScript;
                string destTableScript;

                if (isDestinationEmpty)
                {
                    sourceTableScript = HtmlReportWriter.CreateTableScript(schema, table, sourceInfo, sourceFKs);
                    destTableScript = null;
                }
                else if (isTableEqual)
                {
                    sourceTableScript = HtmlReportWriter.CreateTableScript(schema, table, sourceInfo, sourceFKs);
                    destTableScript = HtmlReportWriter.CreateTableScript(schema, table, destinationInfo, destFKs);
                }
                else
                {
                    // Changed table - use ALTER scripts
                    sourceTableScript = HtmlReportWriter.CreateAlterTableScript(schema, table, destinationInfo, sourceInfo, destFKs, sourceFKs);
                    destTableScript = HtmlReportWriter.CreateAlterTableScript(schema, table, sourceInfo, destinationInfo, sourceFKs, destFKs);
                }

                HtmlReportWriter.WriteBodyHtml(sourcePath, $"{sourceServer.name} Table", HtmlReportWriter.PrintTableInfo(sourceInfo, allDifferences), returnPage, sourceTableScript);
                HtmlReportWriter.WriteBodyHtml(destinationPath, $"{destinationServer.name} Table", HtmlReportWriter.PrintTableInfo(destinationInfo, allDifferences), returnPage, destTableScript);

                if (!isDestinationEmpty && !isTableEqual)
                {
                    string differencesPath = Path.Combine(schemaFolder, differencesFile);
                    HtmlReportWriter.TableDifferencesWriter(
                        differencesPath,
                        sourceServer.name,
                        destinationServer.name,
                        sourceInfo,
                        destinationInfo,
                        allDifferences,
                        "Differences",
                        $"{schema}.{table}",
                        returnPage,
                        sourceFKs,
                        destFKs
                    );
                    isDifferencesVisible = true;
                }

                isVisible = true;
            }

            // Step 9 - Refresh table definition and write new version
            List<tableDto> destinationNewInfo = destinationInfo;
            bool wasAltered = false;

            if (makeChange == ComparerAction.ApplyChanges && !isTableEqual)
            {
                (_, destinationNewInfo, _, var destinationNewFKs) = TableFetcher.GetTableInfo(sourceServer.connectionString, destinationServer.connectionString, schema, table);
                string newPath = Path.Combine(schemaFolder, newFile);
                var newTableScript = HtmlReportWriter.CreateTableScript(schema, table, destinationNewInfo, destFKs);
                HtmlReportWriter.WriteBodyHtml(newPath, $"New {destinationServer.name} Table", HtmlReportWriter.PrintTableInfo(destinationNewInfo, null), returnPage, newTableScript);
                wasAltered = true;
            }

            // Step 10 - Store result entry for summary
            var fullName = $"{schema}.{table}";
            var tags = objectTags.GetValueOrDefault(fullName, new List<string>());
            var resultItem = new dbObjectResult
            {
                Type = "Table",
                Name = table,
                schema = schema,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = isTableEqual, 
                SourceTableInfo = sourceInfo,
                DestinationTableInfo = destinationInfo,
                SourceForeignKeys = sourceFKs,
                DestinationForeignKeys = destFKs,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                DifferencesFile = isDifferencesVisible ? Path.Combine(safeSchema, differencesFile) : null,
                NewFile = wasAltered ? Path.Combine(safeSchema, newFile) : null,
                Tags = tags
            };

            lock (resultsLock)
            {
                results.Add(resultItem);
            }
        });

        // Step 11 - Generate summary report
        (string tableHtmlReport, string tablesCount) = HtmlReportWriter.WriteSummaryReport(sourceServer, destinationServer, Path.Combine(tablesFolderPath, "index.html"), results, filter, run, isIgnoredEmpty, ignoredCount, tagColors);
        int tableDiffsCount = filter == DbObjectFilter.HideUnchanged ? results.Count(r => (r.IsDestinationEmpty) || (!r.IsDestinationEmpty && !r.IsEqual)) : results.Count();
        return new summaryReportDto
        {
            path = "Tables/index.html",
            fullPath = Path.Combine(tablesFolderPath, "index.html"),
            html = tableHtmlReport,
            count = tablesCount,
            diffsCount = tableDiffsCount
        };
    }
    public static summaryReportDto CompareUdts(DbServer sourceServer,DbServer destinationServer, string outputFolder,ComparerAction makeChange,DbObjectFilter filter,Run run,HashSet<string> ignoredObjects, Dictionary<string, List<string>> objectTags, Dictionary<string, string> tagColors, int threadCount)
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = threadCount };

        // 1) Folders
        Directory.CreateDirectory(outputFolder);
        string udtsFolderPath = Path.Combine(outputFolder, "UDTs");
        Directory.CreateDirectory(udtsFolderPath);

        // 2) Ignored meta
        bool isIgnoredEmpty = !ignoredObjects.Any();
        string ignoredCount = ignoredObjects.Count.ToString();

        // 3) Pull UDT names from source
        var udts = UdtFetcher.GetUdtNames(sourceServer.connectionString).OrderBy(u => u.schema).ThenBy(u => u.name).ToList();
        var results = new List<dbObjectResult>();

        Serilog.Log.Information("UDTs:");

        // 4) Compare bodies (scripted CREATE TYPE definitions)
        Parallel.ForEach(udts, parallelOptions, udt =>
        {
            string schema = udt.schema;
            string name = udt.name;
            string full = $"{schema}.{name}";
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(name);

            if (ignoredObjects.Any(ignore =>
            {
                var parts = ignore.Split('.');
                var safeParts = parts
                    .Select(part => part == "*" ? "*" : MakeSafe(part));
                var safeIgnore = string.Join(".", safeParts);

                if (ignore.EndsWith(".*"))
                    return safeIgnore == safeSchema + ".*";

                return safeIgnore == safeSchema + "." + safeName;
            }))
            {
                Log.Information($"{schema}.{name}: Ignored");
                return;
            }

            string schemaFolder = Path.Combine(udtsFolderPath, safeSchema);

            // 5) Script UDTs on both servers
            (string sourceBody, string destBody) = UdtFetcher.GetUdtBody(sourceServer.connectionString, destinationServer.connectionString, schema, name);
            bool areEqual = AreBodiesEqual(sourceBody, destBody);
            bool isDestinationEmpty = string.IsNullOrWhiteSpace(destBody);
            string change = areEqual ? "No changes" : "Changes detected";
            Serilog.Log.Information($"{full}: {change}");

            // 6) Files
            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string differencesFile = $"{safeName}_differences.html";
            string returnPage = Path.Combine("..", "index.html");

            bool isVisible = false, isDifferencesVisible = false;

            // 7) Output HTML
            if ((areEqual && filter == DbObjectFilter.ShowUnchanged) || !areEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                HtmlReportWriter.WriteBodyHtml(Path.Combine(schemaFolder, sourceFile), $"{sourceServer.name}", sourceBody, returnPage);
                HtmlReportWriter.WriteBodyHtml(Path.Combine(schemaFolder, destinationFile), $"{destinationServer.name}", destBody, returnPage);

                if (!isDestinationEmpty && !areEqual)
                {
                    HtmlReportWriter.DifferencesWriter(
                        Path.Combine(schemaFolder, differencesFile),
                        sourceServer.name, destinationServer.name,
                        sourceBody, destBody,
                        "Differences", $"{schema}.{name}", returnPage);
                    isDifferencesVisible = true;
                }
                isVisible = true;
            }
            // 8) Attempt apply changes if requested (DROP TYPE + CREATE TYPE guarded by dependency check)
            // Prepare variables for potential update


            // 9) Summary row
            var fullName = $"{schema}.{name}";
            var tags = objectTags.GetValueOrDefault(fullName, new List<string>());
            results.Add(new dbObjectResult
            {
                Type = "UDT",
                Name = name,
                schema = schema,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = areEqual,
                SourceBody = sourceBody,
                DestinationBody = isDestinationEmpty ? null : destBody,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                DifferencesFile = isDifferencesVisible ? Path.Combine(safeSchema, differencesFile) : null,
                NewFile = null,
                Tags = tags
            });
        });

        // 10) Summary page
        (string udtHtml, string udtCount) = HtmlReportWriter.WriteSummaryReport(
            sourceServer, destinationServer, Path.Combine(udtsFolderPath, "index.html"),
            results, filter, run, isIgnoredEmpty, ignoredCount, tagColors);

        int udtDiffsCount = filter == DbObjectFilter.HideUnchanged ? results.Count(r => (r.IsDestinationEmpty) || (!r.IsDestinationEmpty && !r.IsEqual)) : results.Count(); 

        return new summaryReportDto
        {
            path = "UDTs/index.html",
            fullPath = Path.Combine(udtsFolderPath, "index.html"),
            html = udtHtml,
            count = udtCount,
            diffsCount = udtDiffsCount
        };
    }
    private static summaryReportDto CompareFunctions(DbServer sourceServer, DbServer destinationServer, string outputFolder, ComparerAction makeChange, DbObjectFilter filter, Run run, HashSet<string> ignoredObjects, Dictionary<string, List<string>> objectTags, Dictionary<string, string> tagColors, int threadCount)
    {
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = threadCount };

        // 1) Folders
        Directory.CreateDirectory(outputFolder);
        string functionsFolderPath = Path.Combine(outputFolder, "Functions");
        Directory.CreateDirectory(functionsFolderPath);

        // 2) Ignored meta
        bool isIgnoredEmpty = !ignoredObjects.Any();
        string ignoredCount = ignoredObjects.Count.ToString();

        // 3) Pull function names from source
        var functions = FunctionFetcher.GetFunctionNames(sourceServer.connectionString)
            .OrderBy(f => f.schema).ThenBy(f => f.name).ToList();
        var results = new List<dbObjectResult>();

        Serilog.Log.Information("Functions:");

        // 4) Compare bodies
        Parallel.ForEach(functions, parallelOptions, function =>
        {
            string schema = function.schema;
            string name = function.name;
            string full = $"{schema}.{name}";
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(name);

            if (ignoredObjects.Any(ignore =>
            {
                var parts = ignore.Split('.');
                var safeParts = parts.Select(part => part == "*" ? "*" : MakeSafe(part));
                var safeIgnore = string.Join(".", safeParts);

                if (ignore.EndsWith(".*"))
                    return safeIgnore == safeSchema + ".*";

                return safeIgnore == safeSchema + "." + safeName;
            }))
            {
                Log.Information($"{schema}.{name}: Ignored");
                return;
            }

            string schemaFolder = Path.Combine(functionsFolderPath, safeSchema);

            // 5) Get function bodies from both servers
            (string sourceBody, string destBody) = FunctionFetcher.GetFunctionBody(
                sourceServer.connectionString, destinationServer.connectionString, schema, name);

            var marker = @"--\s*diffinity\s*:\s*client(?:\s*-\s*|\s+)specific\b";
            bool isTenantSpecific =
                (!string.IsNullOrWhiteSpace(sourceBody) &&
                 Regex.IsMatch(sourceBody, marker, RegexOptions.IgnoreCase | RegexOptions.Multiline)) ||
                (!string.IsNullOrWhiteSpace(destBody) &&
                 Regex.IsMatch(destBody, marker, RegexOptions.IgnoreCase | RegexOptions.Multiline));

            bool areEqual = AreBodiesEqual(sourceBody, destBody);
            bool isDestinationEmpty = string.IsNullOrWhiteSpace(destBody);
            string change = areEqual ? "No changes" : "Changes detected";
            Serilog.Log.Information($"{full}: {change}");

            // 6) Files
            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string differencesFile = $"{safeName}_differences.html";
            string newFile = $"{safeName}_new.sql";
            string returnPage = Path.Combine("..", "index.html");

            bool isVisible = false, isDifferencesVisible = false;

            // 7) Output HTML
            if ((areEqual && filter == DbObjectFilter.ShowUnchanged) || !areEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                HtmlReportWriter.WriteBodyHtml(Path.Combine(schemaFolder, sourceFile),
                    $"{sourceServer.name}", sourceBody, returnPage);
                HtmlReportWriter.WriteBodyHtml(Path.Combine(schemaFolder, destinationFile),
                    $"{destinationServer.name}", destBody, returnPage);

                if (!isDestinationEmpty && !areEqual)
                {
                    HtmlReportWriter.DifferencesWriter(
                        Path.Combine(schemaFolder, differencesFile),
                        sourceServer.name, destinationServer.name,
                        sourceBody, destBody,
                        "Differences", $"{schema}.{name}", returnPage);
                    isDifferencesVisible = true;
                }
                isVisible = true;
            }

            // 8) Apply changes if requested
            string? newFilePath = null;
            if (makeChange == ComparerAction.ApplyChanges && !areEqual)
            {
                AlterDbObject(destinationServer.connectionString, sourceBody, destBody);
                Directory.CreateDirectory(schemaFolder);
                File.WriteAllText(Path.Combine(schemaFolder, newFile), sourceBody);
                newFilePath = Path.Combine(safeSchema, newFile);
            }

            // 9) Summary row
            var fullName = $"{schema}.{name}";
            var tags = objectTags.GetValueOrDefault(fullName, new List<string>());
            results.Add(new dbObjectResult
            {
                Type = "FUNCTION",
                Name = name,
                schema = schema,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = areEqual,
                SourceBody = sourceBody,
                DestinationBody = isDestinationEmpty ? null : destBody,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                DifferencesFile = isDifferencesVisible ? Path.Combine(safeSchema, differencesFile) : null,
                NewFile = newFilePath,
                IsTenantSpecific = isTenantSpecific,
                Tags = tags
            });
        });

        // 10) Summary page
        (string functionHtml, string functionCount) = HtmlReportWriter.WriteSummaryReport(
            sourceServer, destinationServer, Path.Combine(functionsFolderPath, "index.html"),
            results, filter, run, isIgnoredEmpty, ignoredCount, tagColors);

        int functionDiffsCount = filter == DbObjectFilter.HideUnchanged
            ? results.Count(r => r.IsDestinationEmpty || (!r.IsDestinationEmpty && !r.IsEqual))
            : results.Count();

        return new summaryReportDto
        {
            path = "Functions/index.html",
            fullPath = Path.Combine(functionsFolderPath, "index.html"),
            html = functionHtml,
            count = functionCount,
            diffsCount = functionDiffsCount
        };
    }
    private static string MakeSafe(string name)
    {
        // Helper method to sanitize file names
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        name = '[' + name + ']';
        return name;
    }
    public class summaryReportDto
    {
        public string path { get; set; }
        public string fullPath { get; set; }
        public string html { get; set; }
        public string count { get; set; }
        public int diffsCount { get; set; }

    }
}

