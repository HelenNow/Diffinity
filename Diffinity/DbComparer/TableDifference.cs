namespace Diffinity;

public class TableDifference
{
    private Dictionary<string, bool> TableMap = new();

    public string TableName { get; private set; }
    public List<ColumnDifference> ColumnDifferences = new();
    public bool ExistsInDb(string db) => TableMap[db];
    public void SetExistsInDb(string db, bool exists) => TableMap[db] = exists;

    #region constructors and static factory methods
    public TableDifference(string name)
    {
        this.TableName = name;
    } 
    #endregion
}
