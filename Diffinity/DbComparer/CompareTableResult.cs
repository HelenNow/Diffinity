namespace Diffinity;

public class CompareTableResult
{
    string db1, db2;
    
    public List<TableDifference> TableDifferences = new();

    #region constructors and static factory methods
    public CompareTableResult(string db1, string db2)
    {
        this.db1 = db1;
        this.db2 = db2;

    } 
    #endregion
}