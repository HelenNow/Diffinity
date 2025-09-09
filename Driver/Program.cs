using Diffinity;
using System.Diagnostics;

internal class Program
{
    static void Main(string[] args)
    {
        var MyDbV1 = new DbServer("Corewell", Environment.GetEnvironmentVariable("localDb"));
        var MyDbV2 = new DbServer("CMH", Environment.GetEnvironmentVariable("localDb1"));
        var sw = Stopwatch.StartNew();
        string IndexPage = DbComparer.Compare(MyDbV1, MyDbV2,run: Run.Proc);
        sw.Stop();
        Console.WriteLine($"Comparison completed in {sw.Elapsed.TotalSeconds} seconds.");
        #region Optional:
        // You can optionally pass any of the following parameters:
        // logger: your custom ILogger instance
        // outputFolder: path to save the results (string)
        // makeChange: whether to apply changes (ComparerAction.ApplyChanges,ComparerAction.DoNotApplyChanges)
        // filter: filter rules (DbObjectFilter.ShowUnchanged,DbObjectFilter.HideUnchanged)
        // run: execute comparison on specific dbObject(Run.Proc,Run.View,Run.Table,Run.ProcView,Run.ProcTable,Run.ViewTable,Run.All)
        //
        // Example:
        // string IndexPage = DbComparer.Compare(MyDbV1, MyDbV2, logger: myLogger, outputFolder: "customPath", makeChange: true);
        #endregion
        var psi = new ProcessStartInfo
        {
            FileName = IndexPage,
            UseShellExecute = true
        };
        Process.Start(psi);
    }
}
