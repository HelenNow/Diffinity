namespace Diffinity;

public class CompareTableTester
{
    private const string TableName = "Student";

    public void Test()
    {
        var tr = new CompareTableResult("corewell", "cmh");
        TableDifference student = new(TableName);
        ColumnDifference nameColumn = new("name");
        nameColumn.SetExistsInDb(TableName, true);
        student.ColumnDifferences.Add(nameColumn);
        tr.TableDifferences.Add(student);
    }
}
