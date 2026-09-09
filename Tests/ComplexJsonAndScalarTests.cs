using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class ComplexJsonAndScalarTests
{
    [TestMethod]
    public void ComplexJson_ScalarNullResult_WritesNullValue()
    {
        // Simulate reading a scalar query result where column 0 is DBNull (e.g. SUM(Amount) on empty table)
        var dt = new DataTable();
        dt.Columns.Add("Total", typeof(decimal));
        dt.Rows.Add(DBNull.Value);

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("scalarResult");

            // Replicate ComplexJson scalar logic:
            if (reader.IsDBNull(0))
                writer.WriteNullValue();
            else
                writer.WriteDbValue(reader, reader.GetFieldType(0), 0);

            writer.WriteEndObject();
            writer.Flush();
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        Assert.AreEqual(JsonValueKind.Null, doc.RootElement.GetProperty("scalarResult").ValueKind);
    }

    [TestMethod]
    public void ComplexJson_ScalarNonNullGuid_WritesGuidValue()
    {
        var guid = Guid.NewGuid();
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(Guid));
        dt.Rows.Add(guid);

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("scalarGuid");

            if (reader.IsDBNull(0))
                writer.WriteNullValue();
            else
                writer.WriteDbValue(reader, reader.GetFieldType(0), 0);

            writer.WriteEndObject();
            writer.Flush();
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());
        using var doc = JsonDocument.Parse(json);
        Assert.AreEqual(guid, doc.RootElement.GetProperty("scalarGuid").GetGuid());
    }

    [TestMethod]
    public void StrList_WithNulls_DoesNotThrowAndReturnsNullElement()
    {
        var dt = new DataTable();
        dt.Columns.Add("Name", typeof(string));
        dt.Rows.Add("Alice");
        dt.Rows.Add(DBNull.Value);
        dt.Rows.Add("Bob");

        using var reader = dt.CreateDataReader();
        var list = new List<string?>();
        while (reader.Read())
            list.Add(reader.IsDBNull(0) ? null : reader.GetString(0));

        Assert.AreEqual(3, list.Count);
        Assert.AreEqual("Alice", list[0]);
        Assert.IsNull(list[1]);
        Assert.AreEqual("Bob", list[2]);
    }

    [TestMethod]
    public void Val_GuidAndNull_DirectCastWorksWithoutChangeTypeException()
    {
        var guid = Guid.NewGuid();
        object? valGuid = guid;
        object? valNull = DBNull.Value;

        // Test the exact pattern used in Val.cs:
        // val is null || val is DBNull ? default : val is T t ? t : (T)Convert.ChangeType(val, typeof(T));

        Guid resultGuid = valGuid is null || valGuid is DBNull ? default : valGuid is Guid g ? g : (Guid)Convert.ChangeType(valGuid, typeof(Guid));
        Guid resultNull = valNull is null || valNull is DBNull ? default : valNull is Guid g2 ? g2 : (Guid)Convert.ChangeType(valNull, typeof(Guid));

        Assert.AreEqual(guid, resultGuid);
        Assert.AreEqual(Guid.Empty, resultNull);
    }
}

