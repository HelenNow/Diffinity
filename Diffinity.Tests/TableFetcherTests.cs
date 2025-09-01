using Xunit;
using Diffinity.TableHelper;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Linq;
using System.Collections.Generic;

namespace Diffinity.Tests
{
    public class TableFetcherTests
    {
        private const string MasterConnectionString = @"Server=(localdb)\mssqllocaldb;Integrated Security=true;";

        private string SetupTestDatabase()
        {
            string dbName = "TestDb_" + System.Guid.NewGuid().ToString("N");
            using var master = new SqlConnection(MasterConnectionString);
            master.Open();
            master.Execute($"CREATE DATABASE [{dbName}];");
            using var testDb = new SqlConnection($"{MasterConnectionString}Initial Catalog={dbName};");
            testDb.Open();
            // Create a sample table
            testDb.Execute("CREATE TABLE Users (Id INT PRIMARY KEY, Name NVARCHAR(50) NULL);");
            return $"{MasterConnectionString}Initial Catalog={dbName};";
        }

        [Fact]
        public void GetTablesNames_ShouldReturnUsersTable()
        {
            // Arrange
            string testDbConnection = SetupTestDatabase();

            // Act
            var tables = TableFetcher.GetTablesNames(testDbConnection);

            // Assert
            Assert.Contains("Users", tables);
        }

        [Fact]
        public void GetTableInfo_ShouldReturnColumns()
        {
            // Arrange
            string testDbConnection = SetupTestDatabase();

            // Act
            var (sourceColumns, destinationColumns) = TableFetcher.GetTableInfo(testDbConnection, testDbConnection, "Users");

            // Assert
            Assert.Equal(2, sourceColumns.Count);
            Assert.Equal(2, destinationColumns.Count);

            Assert.Contains(sourceColumns, c => c.columnName == "Id");
            Assert.Contains(sourceColumns, c => c.columnName == "Name");
        }

        [Fact]
        public void TableComparerAndUpdater_ShouldDetectDifferences()
        {
            // Arrange
            string testDbConnection = SetupTestDatabase();

            var sourceTable = new tableDto
            {
                columnName = "Id",
                columnType = "int",
                isNullable = "NO",
                maxLength = "0",
                isPrimaryKey = "YES",
                isForeignKey = "NO"
            };
            var destinationTable = new tableDto
            {
                columnName = "Id",
                columnType = "int",
                isNullable = "YES", // difference here
                maxLength = "0",
                isPrimaryKey = "YES",
                isForeignKey = "NO"
            };

            // Act
            var (areEqual, differences) = TableComparerAndUpdater.ComparerAndUpdater(
                testDbConnection, sourceTable, destinationTable, "Users", ComparerAction.DoNotApplyChanges);

            // Assert
            Assert.False(areEqual);
            Assert.Contains(differences, d => d.Contains("isNullable"));
        }
    }
}
