namespace Diffinity;

using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

internal class Program
{
    static void Main(string[] args)
    {

       var db1 = new DbServer("DB1", Environment.GetEnvironmentVariable("localDb1"));
        var db2 = new DbServer("DB2", Environment.GetEnvironmentVariable("localDb"));
        var tester = new CompareTableTester();
        tester.Test(db1, db2);
        //        f();
        //        return;
        //        var CMH = new DbServer("CMH", Environment.GetEnvironmentVariable("cmhCs"));

        //        string IndexPage = DbComparer.Compare(DEV002, CMH);
        //        #region Optional
        //        // You can optionally pass any of the following parameters:
        //        // logger: your custom ILogger instance
        //        // outputFolder: path to save the results (string)
        //        // makeChange: whether to apply changes (ComparerAction.ApplyChanges,ComparerAction.DoNotApplyChanges)
        //        // filter: filter rules (DbObjectFilter.ShowUnchanged,DbObjectFilter.HideUnchanged)
        //        // run: execute comparison on specific dbObject(Run.Proc,Run.View,Run.Table,Run.ProcView,Run.ProcTable,Run.ViewTable,Run.All)
        //        //
        //        // Example:
        //        // string IndexPage = DbComparer.Compare(MyDbV1, MyDbV2, logger: myLogger, outputFolder: "customPath", makeChange: true);
        //        #endregion
        //        var psi = new ProcessStartInfo
        //        {
        //            FileName = IndexPage,
        //            UseShellExecute = true
        //        };
        //        Process.Start(psi);
        //    }
        //    //static void f()
        //    {
        //        var lines = File.ReadAllLines(@"C:\trash\diffinitytablecompare.csv");

        //        var s = lines[1];
        //        Line line = new Line(s);
        //        Console.WriteLine(line);

    }
}

public class Line
{
    public Line(string line)
    {
        string[] columns = line.Split(',');
        TestCase = columns[0];
        tableName = columns[1];
        columnName = columns[2];
        db1 = boolIt(columns[3]);
        db2 = boolIt(columns[4]);
        datatypeDb1 = columns[5];
        datatypeDb2 = columns[6];
        lengthdb1 = intIt(columns[7]);
        lengthdb2 = intIt(columns[8]);
        nullabledb1 = boolIt(columns[9]);
        nullabledb2 = boolIt(columns[10]);
        pkdb1 = boolIt(columns[11]);
        pkdb2 = boolIt(columns[12]);
        fkdb1 = boolIt(columns[13]);
        fkd2 = boolIt(columns[14]);
        bool boolIt(string s) => s.ToLower() == "true";
        int? intIt(string? s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (int.TryParse(s, out int value)) return value;
            throw new Exception($"Could not convert to integer: {s}");
        }
    }
    public string TestCase { get; set; }
    public string tableName { get; set; }
    public string columnName { get; set; }
    public bool db1 { get; set; }
    public bool db2 { get; set; }
    public string datatypeDb1 { get; set; }
    public string datatypeDb2 { get; set; }
    public int? lengthdb1 { get; set; }
    public int? lengthdb2 { get; set; }
    public bool nullabledb1 { get; set; }
    public bool nullabledb2 { get; set; }
    public bool pkdb1 { get; set; }
    public bool pkdb2 { get; set; }
    public bool fkdb1 { get; set; }
    public bool fkd2 { get; set; }

    public override string ToString()
    {
        return $"TestCase: {TestCase}\ntableName: {tableName}\ncolumnName: {columnName}\ndb1: {db1}\ndb2: {db2}\ndatatypeDb1: {datatypeDb1}\ndatatypeDb2: {datatypeDb2}\nlengthdb1: {lengthdb1}\nlengthdb2: {lengthdb2}\nnullabledb1: {nullabledb1}\nnullabledb2: {nullabledb2}\npkdb1: {pkdb1}\npkdb2: {pkdb2}\nfkdb1: {fkdb1}\nfkd2: {fkd2}\n";
    }
}
