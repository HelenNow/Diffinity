namespace Diffinity.DbComparer;

internal class ConvertLineToColumnDifference
{
    const string db1 = "corewell";
    const string db2 = "cmh";
    public ColumnDifference Convert(Line line)
    {
        var c = new ColumnDifference(line.columnName);
        if (line.db1) c.SetExistsInDb(db1, line.db1);
        if (line.db1) c.SetExistsInDb(db2, line.db2);
        return c;
    }
}
