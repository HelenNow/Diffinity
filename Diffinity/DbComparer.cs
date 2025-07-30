﻿using DbComparer.HtmlHelper;
using DbComparer.ProcHelper;
using DbComparer.TableHelper;
using DbComparer.ViewHelper;
using Microsoft.IdentityModel.Tokens;

namespace DbComparer;
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
public record DbServer(string name, string connectionString);
public class DbComparer : DbObjectHandler
{
    public static string CompareProcs(DbServer sourceServer, DbServer destinationServer, string outputFolder, ComparerAction makeChange, DbObjectFilter filter)
    {
        /// <summary>
        /// Compares stored procedures between source and destination databases.
        /// Generates HTML reports for differences and optionally applies updates to the destination.
        /// </summary>

        // Step 1 - Setup folder structure for reports
        Directory.CreateDirectory(outputFolder);
        string proceduresFolderPath = Path.Combine(outputFolder, "Procedures");
        Directory.CreateDirectory(proceduresFolderPath);

        // Step 2 - Retrieve procedure names from the source server
        //List<string> procedures = new() { "temporary.GenerateCustomerOrderReport","joelle.rePopulateCommandBags","ccc.spCreateConcierge","temporary.test1", "temporary.test2", "adminapp.spAvgRequestsCompletedPerHourPerConcierge" };
        List<string> procedures = ProcedureFetcher.GetProcedureNames(sourceServer.connectionString);

        List<dbObjectResult> results = new();

        Serilog.Log.Information("Procs:");

        // Step 3 - Loop over each procedure and compare
        foreach (var proc in procedures)
        {
            string[] parts = proc.Split('.');
            string schema = parts.Length > 1 ? parts[0] : "dbo"; 
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(proc);
            string schemaFolder = Path.Combine(proceduresFolderPath, safeSchema);

            // Step 4 - Fetch definitions from both servers
            (string sourceBody, string destinationBody) = ProcedureFetcher.GetProcedureBody(sourceServer.connectionString, destinationServer.connectionString, proc);
            bool areEqual = AreBodiesEqual(sourceBody, destinationBody);
            string change = areEqual ? "No changes" : "Changes detected";
            Serilog.Log.Information($"{proc}: {change}");

            // Step 5 - Setup filenames and paths
            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string differencesFile = $"{safeName}_differences.html";
            string newFile = $"{safeName}_New.html";
            string returnPage = Path.Combine("..", "index.html");

            bool isDestinationEmpty = string.IsNullOrWhiteSpace(destinationBody);
            bool isVisible = false;

            // Step 6 - Write HTML reports if needed
            if ((areEqual && filter == DbObjectFilter.ShowUnchanged) || !areEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                string sourcePath = Path.Combine(schemaFolder, sourceFile);
                string destinationPath = Path.Combine(schemaFolder, destinationFile);
                HtmlReportWriter.WriteBodyHtml(sourcePath, $"{sourceServer.name}", sourceBody, returnPage);
                HtmlReportWriter.WriteBodyHtml(destinationPath, $"{destinationServer.name}", destinationBody, returnPage);

              if (!isDestinationEmpty)
                {
                    string differencesPath = Path.Combine(schemaFolder, differencesFile);
                    HtmlReportWriter.DifferencesWriter(differencesPath, sourceServer.name, destinationServer.name, sourceBody, destinationBody, "Differences", proc, returnPage);
                }
                isVisible = true;
            }

            // Step 7 - Apply changes to destination if instructed
            string destinationNewBody = destinationBody;
            bool wasAltered = false;

            if (!areEqual && makeChange == ComparerAction.ApplyChanges)
            {
                AlterDbObject(destinationServer.connectionString, sourceBody,destinationBody);
                (_, destinationNewBody) = ProcedureFetcher.GetProcedureBody(sourceServer.connectionString, destinationServer.connectionString, proc);
                string newPath = Path.Combine(schemaFolder, newFile);
                HtmlReportWriter.WriteBodyHtml(newPath, $"New {destinationServer.name}", destinationNewBody, returnPage);
                wasAltered = true;
            }

            // Step 8 - Store result entry for summary
            results.Add(new dbObjectResult
            {
                Type = "Proc",
                Name = proc,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = areEqual,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                DifferencesFile = Path.Combine(safeSchema, differencesFile),
                NewFile = wasAltered ? Path.Combine(safeSchema, newFile) : null
            });
        }

        // Step 9 - Generate summary report
        HtmlReportWriter.WriteSummaryReport(sourceServer, destinationServer, Path.Combine(proceduresFolderPath, "index.html"), results, filter);
        return ("Procedures/index.html");
    }
    public static string CompareViews(DbServer sourceServer, DbServer destinationServer, string outputFolder, ComparerAction makeChange, DbObjectFilter filter)
    {
        /// <summary>
        /// Compares SQL views between source and destination databases.
        /// Generates HTML reports for differences and optionally applies updates to the destination.
        /// </summary>

        // Step 1 - Setup folder structure for reports
        Directory.CreateDirectory(outputFolder);
        string viewsFolderPath = Path.Combine(outputFolder, "Views");
        Directory.CreateDirectory(viewsFolderPath);

        // Step 2 - Retrieve view names from the source server
        //List<string> views = new() { "joelle.ConciergeAppAddons","ccc.vwCopyEdits", "ccc.vwRequests ", "[core].[vwUtcRequests2]" };
        List<string> views = ViewFetcher.GetViewsNames(sourceServer.connectionString);

        List<dbObjectResult> results = new();

        Serilog.Log.Information("Views:");

        // Step 3 - Loop over each view and compare
        foreach (var view in views)
        {
            string[] parts = view.Split('.');
            string schema = parts.Length > 1 ? parts[0] : "dbo"; 
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(view);
            string schemaFolder = Path.Combine(viewsFolderPath, safeSchema);

            // Step 4 - Fetch definitions from both servers
            (string sourceBody, string destinationBody) = ViewFetcher.GetViewBody(sourceServer.connectionString, destinationServer.connectionString, view);
            bool areEqual = AreBodiesEqual(sourceBody, destinationBody);
            string change = areEqual ? "No changes" : "Changes detected";
            Serilog.Log.Information($"{view}: {change}");

            // Step 5 - Setup filenames and paths
            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string differencesFile = $"{safeName}_differences.html";
            string newFile = $"{safeName}_New.html";
            string returnPage = Path.Combine("..", "index.html");

            bool isDestinationEmpty =string.IsNullOrEmpty(destinationBody);
            bool isVisible = false;

            // Step 6 - Write HTML reports if needed
            if ((areEqual && filter == DbObjectFilter.ShowUnchanged) || !areEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                string sourcePath = Path.Combine(schemaFolder, sourceFile);
                string destinationPath = Path.Combine(schemaFolder, destinationFile);
                HtmlReportWriter.WriteBodyHtml(sourcePath, $"{sourceServer.name}", sourceBody, returnPage);
                HtmlReportWriter.WriteBodyHtml(destinationPath, $"{destinationServer.name}", destinationBody, returnPage);
                
                if (!isDestinationEmpty)
                {
                    string differencesPath = Path.Combine(schemaFolder, differencesFile);
                    HtmlReportWriter.DifferencesWriter(differencesPath, sourceServer.name, destinationServer.name, sourceBody, destinationBody, "Differences", view, returnPage);
                }
                isVisible = true;
            }

            // Step 7 - Apply changes if instructed
            string destinationNewBody = destinationBody;
            bool wasAltered = false;

            if (!areEqual && makeChange == ComparerAction.ApplyChanges)
            {
                AlterDbObject(destinationServer.connectionString, sourceBody, destinationBody);
                (_, destinationNewBody) = ViewFetcher.GetViewBody(sourceServer.connectionString, destinationServer.connectionString, view);
                string newPath = Path.Combine(schemaFolder, newFile);
                HtmlReportWriter.WriteBodyHtml(newPath, $"New {destinationServer.name}", destinationNewBody, returnPage);
                wasAltered = true;
            }

            // Step 8 - Store result entry for summary
            results.Add(new dbObjectResult
            {
                Type = "View",
                Name = view,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = areEqual,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                DifferencesFile = Path.Combine(safeSchema, differencesFile),
                NewFile = wasAltered ? Path.Combine(safeSchema, newFile) : null
            });
        }

        // Step 9 - Generate summary report
        HtmlReportWriter.WriteSummaryReport(sourceServer, destinationServer, Path.Combine(viewsFolderPath, "index.html"), results, filter);
        return ("Views/index.html");
    }
    public static string CompareTables(DbServer sourceServer, DbServer destinationServer, string outputFolder, ComparerAction makeChange, DbObjectFilter filter)
    {
        /// <summary>
        /// Compares table column definitions between source and destination databases.
        /// Generates HTML reports for schema differences and optionally applies updates to the destination.
        /// </summary>

        // Step 1 - Setup folder structure for reports
        Directory.CreateDirectory(outputFolder);
        string tablesFolderPath = Path.Combine(outputFolder, "Tables");
        Directory.CreateDirectory(tablesFolderPath);

        // Step 2 - Retrieve table names from the source server
        //List<string> tables = new() {"dbo.App","dbo.Client"};
        List<string> tables = TableFetcher.GetTablesNames(sourceServer.connectionString);

        List<dbObjectResult> results = new();
        bool areEqual = false;

        Serilog.Log.Information("Tables:");

        // Step 3 - Loop over each table and compare
        foreach (var table in tables)
        {
            string[] parts = table.Split('.');
            string schema = parts.Length > 1 ? parts[0] : "dbo";
            string safeSchema = MakeSafe(schema);
            string safeName = MakeSafe(table);
            string schemaFolder = Path.Combine(tablesFolderPath, safeSchema);

            // Step 4 - Fetch table column info
            List<string> allDifferences= new List<string>();
            (List<tableDto> sourceInfo, List<tableDto> destinationInfo) = TableFetcher.GetTableInfo(sourceServer.connectionString, destinationServer.connectionString, table);
            bool isDestinationEmpty = destinationInfo.IsNullOrEmpty();

            // Step 5 - Compare each column
            for (int i = 0; i < sourceInfo.Count; i++)
            {
                var tableDto = sourceInfo[i];
                (areEqual, List<string> differences) = TableComparerAndUpdater.ComparerAndUpdater(destinationServer.connectionString, sourceInfo[i], destinationInfo[i], table, makeChange);
                if (!areEqual)
                {
                    allDifferences.AddRange(differences);
                    Serilog.Log.Information($"{table}: Changes detected");
                }
            }
            if (areEqual)
            {
                Serilog.Log.Information($"{table}: No Changes");
            }

            // Step 6 - Setup filenames and paths
            string sourceFile = $"{safeName}_{sourceServer.name}.html";
            string destinationFile = $"{safeName}_{destinationServer.name}.html";
            string newFile = $"{safeName}_New.html";
            string returnPage = Path.Combine("..", "index.html");

            bool isVisible = false;

            // Step 7 - Write HTML reports if needed
            if ((areEqual && filter == DbObjectFilter.ShowUnchanged) || !areEqual)
            {
                Directory.CreateDirectory(schemaFolder);
                string sourcePath = Path.Combine(schemaFolder, sourceFile);
                string destinationPath = Path.Combine(schemaFolder, destinationFile);
                HtmlReportWriter.WriteBodyHtml(sourcePath, $"{sourceServer.name} Table", HtmlReportWriter.PrintTableInfo(sourceInfo,allDifferences), returnPage);
                HtmlReportWriter.WriteBodyHtml(destinationPath, $"{destinationServer.name} Table", HtmlReportWriter.PrintTableInfo(destinationInfo,allDifferences), returnPage);
                isVisible = true;
            }

            // Step 8 - Refresh table definition and write new version
            List<tableDto> destinationNewInfo = destinationInfo;
            bool wasAltered = false;

            if (makeChange == ComparerAction.ApplyChanges)
            {
                (_, destinationNewInfo) = TableFetcher.GetTableInfo(sourceServer.connectionString, destinationServer.connectionString, table);
                string newPath = Path.Combine(schemaFolder, newFile);
                HtmlReportWriter.WriteBodyHtml(newPath, $"New {destinationServer.name} Table", HtmlReportWriter.PrintTableInfo(destinationNewInfo,null), returnPage);
                wasAltered = true;
            }

            // Step 9 - Store result entry for summary
            results.Add(new dbObjectResult
            {
                Type = "Table",
                Name = table,
                IsDestinationEmpty = isDestinationEmpty,
                IsEqual = areEqual,
                SourceFile = isVisible ? Path.Combine(safeSchema, sourceFile) : null,
                DestinationFile = isVisible ? Path.Combine(safeSchema, destinationFile) : null,
                NewFile = wasAltered ? Path.Combine(safeSchema, newFile) : null
            });
        }

        // Step 10 - Generate summary report
        HtmlReportWriter.WriteSummaryReport(sourceServer, destinationServer, Path.Combine(tablesFolderPath, "index.html"), results, filter);
        return ("Tables/index.html");
    }
    private static string MakeSafe(string name)
    {
        // Helper method to sanitize file names
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}

