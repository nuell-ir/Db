using System.Data;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuell;

namespace Db.Tests;

[TestClass]
public class CsvRowTests
{
    [TestMethod]
    public void WriteCsvRow_WithDataReader_SerializesAllSupportedTypesAndNulls()
    {
        var guid = Guid.NewGuid();
        var dto = new DateTimeOffset(2026, 9, 9, 15, 30, 0, TimeSpan.Zero);
        var ts = new TimeSpan(3, 45, 10);
        var bytes = new byte[] { 99, 100, 101 };

        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("GuidCol", typeof(Guid));
        dt.Columns.Add("DtoCol", typeof(DateTimeOffset));
        dt.Columns.Add("TsCol", typeof(TimeSpan));
        dt.Columns.Add("BytesCol", typeof(byte[]));
        dt.Columns.Add("NullCol", typeof(string));

        dt.Rows.Add(123, guid, dto, ts, bytes, DBNull.Value);

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        var str = new StringBuilder();
        var fieldTypes = str.WriteCsvHeader(reader);

        Assert.AreEqual(6, fieldTypes.Length);
        Assert.AreEqual(typeof(int), fieldTypes[0]);
        Assert.AreEqual(typeof(Guid), fieldTypes[1]);
        Assert.AreEqual(typeof(DateTimeOffset), fieldTypes[2]);
        Assert.AreEqual(typeof(TimeSpan), fieldTypes[3]);
        Assert.AreEqual(typeof(byte[]), fieldTypes[4]);
        Assert.AreEqual(typeof(string), fieldTypes[5]);

        str.WriteCsvRow(reader, fieldTypes);
        var csv = str.ToString();

        // Header and row are present
        Assert.IsTrue(csv.Contains("!Id~$GuidCol~#DtoCol~$TsCol~$BytesCol~$NullCol"));
        Assert.IsTrue(csv.Contains($"|123~{guid}~{dto.ToUnixTimeSeconds()}~03:45:10~{Convert.ToBase64String(bytes)}~Ø"));
    }

    [TestMethod]
    public void WriteCsvRow_UnsupportedType_ThrowsNotSupportedException()
    {
        var dt = new DataTable();
        dt.Columns.Add("Bad", typeof(object));
        dt.Rows.Add(new object());

        using var reader = dt.CreateDataReader();
        Assert.IsTrue(reader.Read());

        var str = new StringBuilder();
        Assert.ThrowsExactly<NotSupportedException>(() => str.WriteCsvHeader(reader));
    }
}

