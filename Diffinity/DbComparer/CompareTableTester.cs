namespace Diffinity;

public class CompareTableTester
{
    private const string TableName = "Student";

    public void Test(DbServer source,DbServer destination)
    {
        var comparer = new TableHelperComparer();
        var result = comparer.GetAndCompareTables(source,destination);

        foreach (var t in result.TableDifferences)
        {
            if (t.ExistsInDb(source.name) && !t.ExistsInDb(destination.name))
            {
                Console.WriteLine($"Table: {t.TableName} exists only in {source.name}");
                continue;
            }
            else if (!t.ExistsInDb(source.name) && t.ExistsInDb(destination.name))
            {
                Console.WriteLine($"Table: {t.TableName} exists only in {destination.name}");
                continue;
            }
            Console.WriteLine($"Table: {t.TableName} exist in both");

            foreach (var c in t.ColumnDifferences)
            {
                if (!c.ExistsInDb(source.name) || !c.ExistsInDb(destination.name))
                {
                    Console.WriteLine($"   {c.ColumnName} exists only in one DB");
                }
                else
                {
                    var a1 = c.Db1Attributes;
                    var a2 = c.Db2Attributes;

                    if (a1.columnType != a2.columnType || a1.isNullable != a2.isNullable || a1.maxLength != a2.maxLength)
                    {
                        Console.WriteLine($"     Column {c.ColumnName} differs:");
                        Console.WriteLine($"     {source.name}: {a1.columnType}({a1.maxLength})({a1.isNullable})");
                        Console.WriteLine($"     {destination.name}: {a2.columnType}({a2.maxLength})({a2.isNullable})");
                    }
                }
            }
        }

    }
}
