using Xunit;
using Diffinity;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Diffinity.Tests
{
    public class DiffIgnoreLoaderTests : IDisposable
    {
        private const string DiffIgnoreFile = ".diffignore";

        public DiffIgnoreLoaderTests()
        {
            // Ensure no leftover file exists before each test
            if (File.Exists(DiffIgnoreFile))
                File.Delete(DiffIgnoreFile);
        }

        public void Dispose()
        {
            // Clean up after tests
            if (File.Exists(DiffIgnoreFile))
                File.Delete(DiffIgnoreFile);
        }

        [Fact]
        public void LoadIgnoredObjects_FileDoesNotExist_ReturnsEmptySet()
        {
            // Arrange: file does not exist
            if (File.Exists(DiffIgnoreFile)) File.Delete(DiffIgnoreFile);

            // Act
            var result = DiffIgnoreLoader.LoadIgnoredObjects();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void LoadIgnoredObjects_FileExists_ReturnsNonEmptySet()
        {
            // Arrange
            var lines = new[]
            {
                "Table1",
                "View1",
                "Proc1"
            };
            File.WriteAllLines(DiffIgnoreFile, lines);

            // Act
            var result = DiffIgnoreLoader.LoadIgnoredObjects();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("Table1", result);
            Assert.Contains("View1", result);
            Assert.Contains("Proc1", result);
        }

        [Fact]
        public void LoadIgnoredObjects_IgnoresEmptyLinesAndComments()
        {
            // Arrange
            var lines = new[]
            {
                "# This is a comment",
                "   ",
                "Table1",
                "  # Another comment",
                "View1"
            };
            File.WriteAllLines(DiffIgnoreFile, lines);

            // Act
            var result = DiffIgnoreLoader.LoadIgnoredObjects();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("Table1", result);
            Assert.Contains("View1", result);
        }

        [Fact]
        public void LoadIgnoredObjects_IsCaseInsensitive()
        {
            // Arrange
            var lines = new[]
            {
                "table1",
                "VIEW1",
                "Proc1"
            };
            File.WriteAllLines(DiffIgnoreFile, lines);

            // Act
            var result = DiffIgnoreLoader.LoadIgnoredObjects();

            // Assert
            Assert.Contains("TABLE1", result, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("view1", result, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("proc1", result, StringComparer.OrdinalIgnoreCase);
        }
    }
}
