using Diffinity.TableHelper;

namespace Diffinity
{
    public static class TableComparer
    {
        public static CompareTableResult CompareTables(string db1,string db2,List<string> tablesDb1,List<string> tablesDb2,Dictionary<string, List<tableDto>> columnsDb1,Dictionary<string, List<tableDto>> columnsDb2)
        {
            CompareTableResult result = new(db1, db2);

            var allTables = new HashSet<string>(tablesDb1);
            allTables.UnionWith(tablesDb2);

            foreach (var table in allTables)
            {
                TableDifference tableDiff = new(table);
                tableDiff.SetExistsInDb(db1, tablesDb1.Contains(table));
                tableDiff.SetExistsInDb(db2, tablesDb2.Contains(table));

                var allColumns = new HashSet<string>();
                if (columnsDb1.ContainsKey(table)) allColumns.UnionWith(columnsDb1[table].Select(c => c.columnName));
                if (columnsDb2.ContainsKey(table)) allColumns.UnionWith(columnsDb2[table].Select(c => c.columnName));

                foreach (var column in allColumns)
                {
                    var colDiff = new ColumnDifference(column);
                    colDiff.SetExistsInDb(db1, columnsDb1.ContainsKey(table) && columnsDb1[table].Any(c => c.columnName == column));
                    colDiff.SetExistsInDb(db2, columnsDb2.ContainsKey(table) && columnsDb2[table].Any(c => c.columnName == column));

                    // Attach attribute info if exists
                    colDiff.Db1Attributes = columnsDb1.ContainsKey(table) ? columnsDb1[table].FirstOrDefault(c => c.columnName == column) : null;
                    colDiff.Db2Attributes = columnsDb2.ContainsKey(table) ? columnsDb2[table].FirstOrDefault(c => c.columnName == column) : null;

                    tableDiff.ColumnDifferences.Add(colDiff);
                }

                result.TableDifferences.Add(tableDiff);
            }

            return result;
        }
    }
}
