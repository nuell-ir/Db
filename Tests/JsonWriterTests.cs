using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class JsonWriterTests
{
    [TestMethod]
    public void WriteDbValue_AllSupportedTypesAndNulls_WritesValidJson()
    {
        var guid = Guid.NewGuid();
        var dto = DateTimeOffset.UtcNow;
        var ts = TimeSpan.FromMinutes(15);
        var bytes = new byte[] { 255, 128, 0 };

        var dt = new DataTable();
        dt.Columns.Add("IntCol", typeof(int));
        dt.Columns.Add("GuidCol", typeof(Guid));
        dt.Columns.Add("DtoCol", typeof(DateTimeOffset));
        dt.Columns.Add("TsCol", typeof(TimeSpan));
        dt.Columns.Add("ByteCol", typeof(byte[]));
        dt.Columns.Add("NullCol", typeof(string));

        dt.Rows.Add(42, guid, dto, ts, bytes, DBNull.Value);

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            writer.WritePropertyName("intVal");
            writer.WriteDbValue(reader, typeof(int), 0);

            writer.WritePropertyName("guidVal");
            writer.WriteDbValue(reader, typeof(Guid), 1);

            writer.WritePropertyName("dtoVal");
            writer.WriteDbValue(reader, typeof(DateTimeOffset), 2);

            writer.WritePropertyName("tsVal");
            writer.WriteDbValue(reader, typeof(TimeSpan), 3);

            writer.WritePropertyName("byteVal");
            writer.WriteDbValue(reader, typeof(byte[]), 4);

            writer.WritePropertyName("nullVal");
            writer.WriteDbValue(reader, typeof(string), 5);

            writer.WriteEndObject();
            writer.Flush();
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual(42, root.GetProperty("intVal").GetInt32());
        Assert.AreEqual(guid, root.GetProperty("guidVal").GetGuid());
        Assert.AreEqual(ts.ToString(), root.GetProperty("tsVal").GetString());
        Assert.AreEqual(Convert.ToBase64String(bytes), root.GetProperty("byteVal").GetString());
        Assert.AreEqual(JsonValueKind.Null, root.GetProperty("nullVal").ValueKind);
    }

    [TestMethod]
    public void WriteDbValue_UnsupportedType_ThrowsNotSupportedException()
    {
        var dt = new DataTable();
        dt.Columns.Add("BadCol", typeof(object));
        dt.Rows.Add(new object());

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        Assert.ThrowsExactly<NotSupportedException>(() =>
            writer.WriteDbValue(reader, typeof(object), 0)
        );
    }
}
