using Diffinity;
using System.Diagnostics;

internal class Program
{
    static void Main(string[] args)
    {
        var localdb1   = new DbServer("localdb1", Environment.GetEnvironmentVariable("db1Cs"));
        var localdb2 = new DbServer("localdb2", Environment.GetEnvironmentVariable("db2Cs"));
        
        string IndexPage = DbComparer.Compare(localdb1, localdb2);
        #region Optional
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
