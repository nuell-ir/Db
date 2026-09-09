using System.Collections;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Db.Tests;

[TestClass]
public class TransactionTests
{
    [TestMethod]
    public void Transaction_Sync_NullInputs_ReturnNull()
    {
        Assert.IsNull(nuel.Sync.Db.Transaction((string)null!));
        Assert.IsNull(nuel.Sync.Db.Transaction((string)null!, isStoredProc: false));
        Assert.IsNull(nuel.Sync.Db.Transaction((string)null!, ("@p", 1)));
        Assert.IsNull(nuel.Sync.Db.Transaction((string)null!, new SqlParameter("@p", 1)));
        Assert.IsNull(nuel.Sync.Db.Transaction((IEnumerable<string>)null!));
        Assert.IsNull(nuel.Sync.Db.Transaction((IEnumerable<(string, SqlParameter[])>)null!));
        Assert.IsNull(nuel.Sync.Db.Transaction((IEnumerable<(string, (string, object)[])>)null!));
        Assert.IsNull(nuel.Sync.Db.Transaction((IEnumerable<SqlCommand>)null!));
    }

    [TestMethod]
    public async Task Transaction_Async_NullInputs_ReturnNull()
    {
        Assert.IsNull(await nuel.Async.Db.Transaction((string)null!));
        Assert.IsNull(await nuel.Async.Db.Transaction((string)null!, isStoredProc: false));
        Assert.IsNull(await nuel.Async.Db.Transaction((string)null!, ("@p", 1)));
        Assert.IsNull(await nuel.Async.Db.Transaction((string)null!, new SqlParameter("@p", 1)));
        Assert.IsNull(await nuel.Async.Db.Transaction((IEnumerable<string>)null!));
        Assert.IsNull(await nuel.Async.Db.Transaction((IEnumerable<(string, SqlParameter[])>)null!));
        Assert.IsNull(await nuel.Async.Db.Transaction((IEnumerable<(string, (string, object)[])>)null!));
        Assert.IsNull(await nuel.Async.Db.Transaction((IEnumerable<SqlCommand>)null!));
    }

    private class SingleEnumerationTracker<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        public int EnumerationCount { get; private set; }

        public SingleEnumerationTracker(IEnumerable<T> source)
        {
            _source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            return _source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [TestMethod]
    public void Transaction_IEnumerable_DoesNotPreEnumerateWithCount()
    {
        // In the previous implementation, queries.Count() enumerated the collection before connection.Open().
        // In the new implementation, queries is not enumerated before connection opening.
        var tracker = new SingleEnumerationTracker<string>(new[] { "SELECT 1" });

        try
        {
            nuel.Sync.Db.ConnectionString = "Server=invalid_non_existent;Database=test;Trusted_Connection=True;";
            nuel.Sync.Db.Transaction(tracker);
        }
        catch
        {
            // Expected failure opening connection
        }

        // Must be 0 because connection opening failed before the single-pass enumeration loop was reached
        Assert.AreEqual(0, tracker.EnumerationCount, "IEnumerable was enumerated before connection open (e.g. via Count()).");
    }

    [TestMethod]
    public async Task Transaction_Async_IEnumerable_DoesNotPreEnumerateWithCount()
    {
        var tracker = new SingleEnumerationTracker<string>(new[] { "SELECT 1" });

        try
        {
            nuel.Async.Db.ConnectionString = "Server=invalid_non_existent;Database=test;Trusted_Connection=True;";
            await nuel.Async.Db.Transaction(tracker);
        }
        catch
        {
            // Expected failure opening connection
        }

        Assert.AreEqual(0, tracker.EnumerationCount, "IEnumerable was enumerated before connection open (e.g. via Count()).");
    }

    [TestMethod]
    public void Transaction_DataSqlParams_ConvertsCorrectly()
    {
        (string name, object value)[] tuples = [("@name", "GO;ok"), ("@num", 42), ("@nullVal", null!)];
        var sqlParams = nuel.Data.SqlParams(tuples);

        Assert.AreEqual(3, sqlParams.Length);
        Assert.AreEqual("@name", sqlParams[0].ParameterName);
        Assert.AreEqual("GO;ok", sqlParams[0].Value);
        Assert.AreEqual("@num", sqlParams[1].ParameterName);
        Assert.AreEqual(42, sqlParams[1].Value);
        Assert.AreEqual("@nullVal", sqlParams[2].ParameterName);
        Assert.AreEqual(DBNull.Value, sqlParams[2].Value);
    }
}

