using System;
using System.Collections.Generic;
using Xunit;
using Diffinity.TableHelper;

namespace Diffinity.Tests
{
    public class TableComparerAndUpdaterTests
    {
        // Dummy connection string; in real tests, you can point to a test database
        private const string DestinationConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=TestDb;Trusted_Connection=True;";
        private const string FullTableName = "dbo.TestTable";

        [Fact]
        public void ComparerAndUpdater_ReturnsTrue_WhenTablesAreEqual()
        {
            // Arrange
            var source = new tableDto
            {
                columnName = "Id",
                columnType = "int",
                isNullable = "NO",
                maxLength = "0",
                isPrimaryKey = "true",
                isForeignKey = "false"
            };
            var destination = new tableDto
            {
                columnName = "Id",
                columnType = "int",
                isNullable = "NO",
                maxLength = "0",
                isPrimaryKey = "true",
                isForeignKey = "false"
            };

            // Act
            var result = TableComparerAndUpdater.ComparerAndUpdater(
                DestinationConnectionString,
                source,
                destination,
                FullTableName,
                ComparerAction.None // Use the correct enum from your project
            );

            // Assert
            Assert.True(result.Item1); // Are equal
            Assert.Null(result.Item2); // No differences
        }

        [Fact]
        public void ComparerAndUpdater_ReturnsDifferences_WhenTablesDiffer()
        {
            // Arrange
            var source = new tableDto
            {
                columnName = "Id",
                columnType = "int",
                isNullable = "NO",
                maxLength = "0",
                isPrimaryKey = "True",
                isForeignKey = "False"
            };

            var destination = new tableDto
            {
                columnName = "Id_old",
                columnType = "varchar(50)",
                isNullable = "YES",
                maxLength = "50",
                isPrimaryKey = "False",
                isForeignKey = "True"
            };


            // Act
            var result = TableComparerAndUpdater.ComparerAndUpdater(
                DestinationConnectionString,
                source,
                destination,
                FullTableName,
                ComparerAction.None // Do not apply changes in unit test
            );

            // Assert
            Assert.False(result.Item1); // Not equal
            Assert.NotNull(result.Item2);
            Assert.Contains("Id", result.Item2); // Column name difference
            Assert.Contains("columnType: 'int' != 'varchar(50)'", result.Item2);
            Assert.Contains("isNullable: 'NO' != 'YES'", result.Item2);
            Assert.Contains("maxLength: '0' != '50'", result.Item2);
            Assert.Contains("isPrimaryKey: 'True' != 'False'", result.Item2);
            Assert.Contains("isForeignKey: 'False' != 'True'", result.Item2);
        }

        [Fact]
        public void ComparerAndUpdater_ReturnsFalse_WhenDestinationTableIsNull()
        {
            // Arrange
            var source = new tableDto
            {
                columnName = "Id",
                columnType = "int",
                isNullable = "NO",
                maxLength = "0",
                isPrimaryKey = "true",
                isForeignKey = "false"
            };
            tableDto destination = null;

            // Act
            var result = TableComparerAndUpdater.ComparerAndUpdater(
                DestinationConnectionString,
                source,
                destination,
                FullTableName,
                ComparerAction.None
            );

            // Assert
            Assert.False(result.Item1);
            Assert.Null(result.Item2);
        }
    }
}
