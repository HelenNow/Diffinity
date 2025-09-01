using Diffinity;
using Diffinity.HtmlHelper;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using static Diffinity.DbComparer;
using static Diffinity.HtmlHelper.HtmlReportWriter;

namespace Diffinity.Tests
{
    public class DbComparerTests
    {
        private readonly DbServer _source = new("SourceServer", "SourceDb");
        private readonly DbServer _dest = new("DestServer", "DestDb");

        [Fact]
        public void MakeSafe_ReplacesInvalidChars()
        {
            string unsafeName = "proc:name/with*bad|chars?";
            var makeSafeMethod = typeof(DbComparer)
                .GetMethod("MakeSafe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            string result = makeSafeMethod.Invoke(null, new object[] { unsafeName }).ToString();

            Assert.DoesNotContain(":", result);
            Assert.DoesNotContain("/", result);
            Assert.Contains("_", result); // ensure replacement occurs
        }

        [Fact]
        public void Compare_WithInvalidRun_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DbComparer.Compare(_source, _dest, run: (Run)999));
        }

        [Fact]
        public void CompareProcs_ReturnsIndexPath()
        {
            var ignored = new HashSet<string>();
            var result = DbComparer.CompareProcs(_source, _dest, "FakeOutput", ComparerAction.DoNotApplyChanges, DbObjectFilter.HideUnchanged, Run.Proc, ignored);
            Assert.Equal("Procedures/index.html", result.path);
        }

        [Fact]
        public void CompareViews_ReturnsIndexPath()
        {
            var ignored = new HashSet<string>();
            var result = DbComparer.CompareViews(_source, _dest, "FakeOutput", ComparerAction.DoNotApplyChanges, DbObjectFilter.HideUnchanged, Run.View, ignored);
            Assert.Equal("Views/index.html", result.path);
        }

        [Fact]
        public void CompareTables_ReturnsIndexPath()
        {
            var ignored = new HashSet<string>();
            var result = DbComparer.CompareTables(_source, _dest, "FakeOutput", ComparerAction.DoNotApplyChanges, DbObjectFilter.HideUnchanged, Run.Table, ignored);
            Assert.Equal("Tables/index.html", result.path);
        }
    }

}