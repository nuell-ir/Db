using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class ObjectReflectorTests
{
    private class PocoModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? NullableInt { get; set; }
        public Guid UniqueId { get; set; }
        public Guid? NullableGuid { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public byte[]? Data { get; set; }
        public long BigNumber { get; set; }
    }

    [TestMethod]
    public void GetObject_WithDBNullValues_DoesNotThrowAndAssignsNullOrDefaults()
    {
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("NullableInt", typeof(int));
        dt.Columns.Add("UniqueId", typeof(Guid));
        dt.Columns.Add("NullableGuid", typeof(Guid));
        dt.Columns.Add("Timestamp", typeof(DateTimeOffset));
        dt.Columns.Add("Duration", typeof(TimeSpan));
        dt.Columns.Add("Data", typeof(byte[]));
        dt.Columns.Add("BigNumber", typeof(long));

        // Row with DBNull for nullable / reference types and a DBNull for non-nullable int
        dt.Rows.Add(
            DBNull.Value, // Id (non-nullable int -> should remain default 0 without throwing)
            DBNull.Value, // Name (string -> should be null without throwing)
            DBNull.Value, // NullableInt (int? -> should be null without throwing)
            DBNull.Value, // UniqueId (non-nullable Guid -> should remain Guid.Empty)
            DBNull.Value, // NullableGuid (Guid? -> should be null without throwing)
            DBNull.Value, // Timestamp (non-nullable DateTimeOffset -> default)
            DBNull.Value, // Duration (non-nullable TimeSpan -> default)
            DBNull.Value, // Data (byte[] -> null)
            DBNull.Value  // BigNumber (long -> 0)
        );

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        var poco = reader.GetObject<PocoModel>();

        Assert.IsNotNull(poco);
        Assert.AreEqual(0, poco.Id);
        Assert.IsNull(poco.Name);
        Assert.IsNull(poco.NullableInt);
        Assert.AreEqual(Guid.Empty, poco.UniqueId);
        Assert.IsNull(poco.NullableGuid);
        Assert.AreEqual(default(DateTimeOffset), poco.Timestamp);
        Assert.AreEqual(TimeSpan.Zero, poco.Duration);
        Assert.IsNull(poco.Data);
        Assert.AreEqual(0L, poco.BigNumber);
    }

    [TestMethod]
    public void GetObject_WithValidValues_MapsCorrectlyAndConvertsTypes()
    {
        var guid = Guid.NewGuid();
        var dto = DateTimeOffset.UtcNow;
        var ts = TimeSpan.FromMinutes(45);
        var bytes = new byte[] { 10, 20, 30 };

        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("NullableInt", typeof(int));
        dt.Columns.Add("UniqueId", typeof(Guid));
        dt.Columns.Add("NullableGuid", typeof(Guid));
        dt.Columns.Add("Timestamp", typeof(DateTimeOffset));
        dt.Columns.Add("Duration", typeof(TimeSpan));
        dt.Columns.Add("Data", typeof(byte[]));
        dt.Columns.Add("BigNumber", typeof(int)); // Int in SQL, long in POCO (type conversion test)

        dt.Rows.Add(
            42,
            "John Doe",
            99,
            guid,
            guid,
            dto,
            ts,
            bytes,
            12345 // boxed int -> long property
        );

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        var poco = reader.GetObject<PocoModel>();

        Assert.IsNotNull(poco);
        Assert.AreEqual(42, poco.Id);
        Assert.AreEqual("John Doe", poco.Name);
        Assert.AreEqual(99, poco.NullableInt);
        Assert.AreEqual(guid, poco.UniqueId);
        Assert.AreEqual(guid, poco.NullableGuid);
        Assert.AreEqual(dto, poco.Timestamp);
        Assert.AreEqual(ts, poco.Duration);
        CollectionAssert.AreEqual(bytes, poco.Data);
        Assert.AreEqual(12345L, poco.BigNumber);
    }
}

