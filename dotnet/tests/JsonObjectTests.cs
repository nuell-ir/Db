using System.Data;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class JsonObjectTests
{
	[TestMethod]
	public void GetJsonNode_SupportedTypes_ReturnsExpectedJsonNodes()
	{
		var guid = Guid.NewGuid();
		var dto = DateTimeOffset.UtcNow;
		var ts = TimeSpan.FromHours(2);
		var bytes = new byte[] { 1, 2, 3 };

		var dt = new DataTable();
		dt.Columns.Add("IntCol", typeof(int));
		dt.Columns.Add("StrCol", typeof(string));
		dt.Columns.Add("GuidCol", typeof(Guid));
		dt.Columns.Add("DtoCol", typeof(DateTimeOffset));
		dt.Columns.Add("TsCol", typeof(TimeSpan));
		dt.Columns.Add("ByteCol", typeof(byte[]));
		dt.Columns.Add("NullCol", typeof(string));

		dt.Rows.Add(100, "hello", guid, dto, ts, bytes, DBNull.Value);

		using var reader = dt.CreateDataReader();
		Assert.IsTrue(reader.Read());

		var intNode = reader.GetJsonNode(typeof(int), 0);
		Assert.AreEqual(100, (int)intNode!);

		var strNode = reader.GetJsonNode(typeof(string), 1);
		Assert.AreEqual("hello", (string)strNode!);

		var guidNode = reader.GetJsonNode(typeof(Guid), 2);
		Assert.AreEqual(guid.ToString(), (string)guidNode!);

		var dtoNode = reader.GetJsonNode(typeof(DateTimeOffset), 3);
		Assert.IsNotNull(dtoNode);

		var tsNode = reader.GetJsonNode(typeof(TimeSpan), 4);
		Assert.AreEqual(ts.ToString(), (string)tsNode!);

		var byteNode = reader.GetJsonNode(typeof(byte[]), 5);
		Assert.AreEqual(Convert.ToBase64String(bytes), (string)byteNode!);

		var nullNode = reader.GetJsonNode(typeof(string), 6);
		Assert.IsNull(nullNode);
	}

	[TestMethod]
	public void GetJsonNode_UnsupportedType_ThrowsNotSupportedException()
	{
		var dt = new DataTable();
		dt.Columns.Add("ObjCol", typeof(object));
		dt.Rows.Add(new object());

		using var reader = dt.CreateDataReader();
		Assert.IsTrue(reader.Read());

		Assert.ThrowsExactly<NotSupportedException>(() => reader.GetJsonNode(typeof(object), 0));
	}
}
