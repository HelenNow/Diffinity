using Diffinity;
using System.Diagnostics;

internal class Program
{
    static void Main(string[] args)
    {
        var MyDbV1 = new DbServer("My Db V1", Environment.GetEnvironmentVariable("db_v1_cs"));
        var MyDbV2 = new DbServer("My Db V2", Environment.GetEnvironmentVariable("db_v2_cs"));
        string IndexPage = DbComparer.Compare(MyDbV1, MyDbV2);
        var psi = new ProcessStartInfo
        {
            FileName = IndexPage,
            UseShellExecute = true
        };
        Process.Start(psi);

    }
}
