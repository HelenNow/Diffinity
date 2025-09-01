using Xunit;
using Diffinity.ViewHelper;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Linq;
using System.Collections.Generic;

namespace Diffinity.Tests
{
    public class ViewFetcherTests
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
            testDb.Execute("CREATE TABLE Users (Id INT PRIMARY KEY, Name NVARCHAR(50));");

            // Create a sample view
            testDb.Execute("CREATE VIEW UserView AS SELECT Id, Name FROM Users;");

            return $"{MasterConnectionString}Initial Catalog={dbName};";
        }

        [Fact]
        public void GetViewsNames_ShouldReturnUserView()
        {
            // Arrange
            string testDbConnection = SetupTestDatabase();

            // Act
            var views = ViewFetcher.GetViewsNames(testDbConnection);

            // Assert
            Assert.Contains("UserView", views);
        }

        [Fact]
        public void GetViewBody_ShouldReturnCorrectBody()
        {
            // Arrange
            string testDbConnection = SetupTestDatabase();
            string expectedBody = "SELECT Id, Name FROM Users";

            // Act
            var (sourceBody, destinationBody) = ViewFetcher.GetViewBody(testDbConnection, testDbConnection, "UserView");

            // Assert
            Assert.Contains(expectedBody, sourceBody, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains(expectedBody, destinationBody, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
