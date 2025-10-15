namespace Diffinity;

public class ColumnDifference
{
    private Dictionary<string,bool> ColumnMap=new();
    public string ColumnName { get; private set; }
    public bool ExistsInDb(string table)=>ColumnMap[table];
    public void SetExistsInDb(string db, bool exists) => ColumnMap[db] = exists;
    #region constructors and static factory methods
    public ColumnDifference(string name)
    {
        this.ColumnName = name;
    } 
    #endregion
}