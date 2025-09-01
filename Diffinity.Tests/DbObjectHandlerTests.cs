using Xunit;
using Diffinity;
using Microsoft.Data.SqlClient;
using Dapper;
using System;

namespace Diffinity.Tests
{
    public class DbObjectHandlerTests
    {
        private const string MasterConnectionString = @"Server=(localdb)\mssqllocaldb;Integrated Security=true;";

        private string SetupTestDatabase()
        {
            string dbName = "TestDb_" + Guid.NewGuid().ToString("N");
            using var master = new SqlConnection(MasterConnectionString);
            master.Open();
            master.Execute($"CREATE DATABASE [{dbName}];");

            return $"{MasterConnectionString}Initial Catalog={dbName};";
        }

        #region AreBodiesEqual tests
        [Fact]
        public void AreBodiesEqual_SameBodies_ReturnsTrue()
        {
            string body1 = "CREATE PROCEDURE dbo.TestProc AS SELECT 1;";
            string body2 = "CREATE PROCEDURE dbo.TestProc AS SELECT 1;";

            bool result = DbObjectHandler.AreBodiesEqual(body1, body2);

            Assert.True(result);
        }

        [Fact]
        public void AreBodiesEqual_DifferentBodies_ReturnsFalse()
        {
            string body1 = "CREATE PROCEDURE dbo.TestProc AS SELECT 1;";
            string body2 = "CREATE PROCEDURE dbo.TestProc AS SELECT 2;";

            bool result = DbObjectHandler.AreBodiesEqual(body1, body2);

            Assert.False(result);
        }

        [Fact]
        public void AreBodiesEqual_IgnoresWhitespaceAndBrackets()
        {
            string body1 = "CREATE PROCEDURE [dbo].[TestProc] AS SELECT 1;";
            string body2 = "CREATE PROCEDURE dbo.TestProc AS SELECT 1;";

            bool result = DbObjectHandler.AreBodiesEqual(body1, body2);

            Assert.True(result);
        }
        #endregion

        #region AlterDbObject tests
        [Fact]
        public void AlterDbObject_CreatesObject_WhenDestinationEmpty()
        {
            string conn = SetupTestDatabase();
            string createProc = "CREATE PROCEDURE dbo.TestProc AS SELECT 1;";

            // Destination body empty → should execute CREATE
            DbObjectHandler.AlterDbObject(conn, createProc, "");

            // Verify procedure exists
            using var testDb = new SqlConnection(conn);
            var procExists = testDb.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sys.procedures WHERE name = 'TestProc'");
            Assert.Equal(1, procExists);
        }

        [Fact]
        public void AlterDbObject_AltersObject_WhenDestinationExists()
        {
            string conn = SetupTestDatabase();
            string createProc = "CREATE PROCEDURE dbo.TestProc AS SELECT 1;";

            // Create first
            DbObjectHandler.AlterDbObject(conn, createProc, "");

            string alteredProc = "CREATE PROCEDURE dbo.TestProc AS SELECT 2;";
            DbObjectHandler.AlterDbObject(conn, alteredProc, createProc);

            // Verify the procedure body changed
            using var testDb = new SqlConnection(conn);
            string body = testDb.ExecuteScalar<string>(
                "SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.TestProc'))");
            Assert.Contains("SELECT 2", body);
        }

        [Fact]
        public void AlterDbObject_Throws_WhenSourceEmpty()
        {
            string conn = SetupTestDatabase();

            Assert.Throws<ArgumentException>(() =>
                DbObjectHandler.AlterDbObject(conn, "", ""));
        }
        #endregion
    }
}
