using Dapper;
using Diffinity.ProcHelper;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Diffinity.Tests
{
    public class ProcedureFetcherTests
    {
        private const string SourceConnectionString = "FakeSource";
        private const string DestConnectionString = "FakeDest";

        [Fact]
        public void GetProcedureNames_ReturnsListOfNames()
        {
            // Arrange
            var expected = new List<string> { "Proc1", "Proc2" };

            // Mock IDbConnection
            var mockConnection = new Mock<IDbConnection>();
            mockConnection.Setup(d => d.Query<string>(It.IsAny<string>(), null, null, true, null, null))
                          .Returns(expected);

            // Wrap SqlConnection creation using a Func (requires internal testing hook)
            Func<string, IDbConnection> connectionFactory = _ => mockConnection.Object;

            // Act
            var originalMethod = typeof(ProcedureFetcher).GetMethod("GetProcedureNames");
            var result = originalMethod.Invoke(null, new object[] { SourceConnectionString }) as List<string>;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.Count, result.Count);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetProcedureBody_ReturnsSourceAndDestinationBodies()
        {
            // Arrange
            string procedureName = "TestProc";
            string sourceBody = "SELECT 1";
            string destBody = "SELECT 2";

            var mockSourceConnection = new Mock<IDbConnection>();
            mockSourceConnection.Setup(c => c.QueryFirst<string>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                                .Returns(sourceBody);

            var mockDestConnection = new Mock<IDbConnection>();
            mockDestConnection.Setup(c => c.QueryFirst<string>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                              .Returns(destBody);

            // Act
            var originalMethod = typeof(ProcedureFetcher).GetMethod("GetProcedureBody");
            var result = ((string sourceBody, string destinationBody))originalMethod.Invoke(null, new object[] { SourceConnectionString, DestConnectionString, procedureName });

            // Assert
            Assert.Equal(sourceBody, result.sourceBody);
            Assert.Equal(destBody, result.destinationBody);
        }

        [Fact]
        public void GetProcedureBody_ThrowsException_ForInvalidProcedure()
        {
            string procedureName = "NonExistentProc";

            var mockConnection = new Mock<IDbConnection>();
            mockConnection.Setup(c => c.QueryFirst<string>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                          .Throws(new InvalidOperationException("No rows"));

            var originalMethod = typeof(ProcedureFetcher).GetMethod("GetProcedureBody");

            // Assert
            Assert.Throws<TargetInvocationException>(() =>
            {
                var result = originalMethod.Invoke(null, new object[] { SourceConnectionString, DestConnectionString, procedureName });
            });
        }
    }
}
