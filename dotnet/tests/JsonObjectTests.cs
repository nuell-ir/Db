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

	[TestMethod]
	public void GetJsonNode_NullableTypes_ReturnsExpectedJsonNodes()
	{
		var guid = Guid.NewGuid();
		var dt = new DataTable();
		dt.Columns.Add("NullableInt", typeof(int));
		dt.Columns.Add("NullableGuid", typeof(Guid));
		dt.Columns.Add("NullableBool", typeof(bool));
		dt.Rows.Add(42, guid, true);

		using var reader = dt.CreateDataReader();
		Assert.IsTrue(reader.Read());

		var intNode = reader.GetJsonNode(typeof(int?), 0);
		Assert.AreEqual(42, (int)intNode!);

		var guidNode = reader.GetJsonNode(typeof(Guid?), 1);
		Assert.AreEqual(guid.ToString(), (string)guidNode!);

		var boolNode = reader.GetJsonNode(typeof(bool?), 2);
		Assert.AreEqual(true, (bool)boolNode!);
	}

	[TestMethod]
	public void GetJsonObject_WithoutColumnSchema_ReturnsExpectedJsonObject()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Columns.Add("Active", typeof(bool));
		dt.Rows.Add(1, "Alice", true);

		using var reader = dt.CreateDataReader();
		Assert.IsTrue(reader.Read());

		var json = reader.GetJsonObject();
		Assert.IsNotNull(json);
		Assert.AreEqual(1, (int)json["Id"]!);
		Assert.AreEqual("Alice", (string)json["Name"]!);
		Assert.AreEqual(true, (bool)json["Active"]!);
	}

	[TestMethod]
	public void GetJsonObject_NullColumnsCollection_FallsBackToDirectReader()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Rows.Add(99, "Bob");

		using var reader = dt.CreateDataReader();
		Assert.IsTrue(reader.Read());

		var json = reader.GetJsonObject(null);
		Assert.IsNotNull(json);
		Assert.AreEqual(99, (int)json["Id"]!);
		Assert.AreEqual("Bob", (string)json["Name"]!);
	}

	[TestMethod]
	public async Task JsonObject_NullOrWhitespaceQuery_ThrowsArgumentException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => nuel.Db.JsonObject(null!));
		await Assert.ThrowsExactlyAsync<ArgumentException>(() => nuel.Db.JsonObject("   "));
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => nuel.Db.JsonObject(null!, true));
		await Assert.ThrowsExactlyAsync<ArgumentException>(() => nuel.Db.JsonObject("   ", true));
	}
}

