using Diffinity.TableHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diffinity;
    public class TableHelperComparer
{
        public CompareTableResult GetAndCompareTables(DbServer source,DbServer destination)
        {
            // Step 1: Get all tables
            var sourceTables = TableFetcher.GetTablesNames(source.connectionString);
            var destinationTables = TableFetcher.GetTablesNames(destination.connectionString);

            var tablesDb1 = sourceTables.Select(t => $"{t.schema}.{t.name}").ToList();
            var tablesDb2 = destinationTables.Select(t => $"{t.schema}.{t.name}").ToList();

            // Step 2: Get column details
            var columnsDb1 = new Dictionary<string, List<tableDto>>();
            var columnsDb2 = new Dictionary<string, List<tableDto>>();
            var allTables = new HashSet<string>(tablesDb1);
            allTables.UnionWith(tablesDb2);

            foreach (var fullTableName in allTables)
            {
                var parts = fullTableName.Split('.');
                string schema = parts[0];
                string tableName = parts[1];

                var (srcCols, destCols) = TableFetcher.GetTableInfo(
                    source.connectionString, destination.connectionString, schema, tableName);

                columnsDb1[fullTableName] = srcCols;
                columnsDb2[fullTableName] = destCols;
            }

            // Step 3: Compare
            return TableComparer.CompareTables(source.name, destination.name, tablesDb1, tablesDb2, columnsDb1, columnsDb2);
        }
    }
