using System.Data;
using System.Reflection;
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
	[TestMethod]
	public async Task ReadJson_ObjectResult_DirectUtf8OutputToStream()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Columns.Add("Score", typeof(double));
		dt.Rows.Add(1, "Alice", 99.5);

		using var reader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			await reader.ReadJson(JsonValueType.Object, writer);
			writer.Flush();
		}

		stream.Position = 0;
		using var doc = JsonDocument.Parse(stream);
		var root = doc.RootElement;
		Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
		Assert.AreEqual(1, root.GetProperty("Id").GetInt32());
		Assert.AreEqual("Alice", root.GetProperty("Name").GetString());
		Assert.AreEqual(99.5, root.GetProperty("Score").GetDouble());
	}

	[TestMethod]
	public async Task ReadJson_ArrayResult_DirectUtf8OutputToStream()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Title", typeof(string));
		dt.Rows.Add(1, "Item A");
		dt.Rows.Add(2, "Item B");

		using var reader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			await reader.ReadJson(JsonValueType.Array, writer);
			writer.Flush();
		}

		stream.Position = 0;
		using var doc = JsonDocument.Parse(stream);
		var root = doc.RootElement;
		Assert.AreEqual(JsonValueKind.Array, root.ValueKind);
		Assert.AreEqual(2, root.GetArrayLength());
		Assert.AreEqual(1, root[0].GetProperty("Id").GetInt32());
		Assert.AreEqual("Item A", root[0].GetProperty("Title").GetString());
		Assert.AreEqual(2, root[1].GetProperty("Id").GetInt32());
		Assert.AreEqual("Item B", root[1].GetProperty("Title").GetString());
	}

	[TestMethod]
	public async Task ReadJson_EmptyReader_WritesExpectedNullOrEmptyArray()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));

		// Object on empty reader -> null
		using (var reader = dt.CreateDataReader())
		using (var stream = new MemoryStream())
		using (var writer = new Utf8JsonWriter(stream))
		{
			await reader.ReadJson(JsonValueType.Object, writer);
			writer.Flush();
			stream.Position = 0;
			using var doc = JsonDocument.Parse(stream);
			Assert.AreEqual(JsonValueKind.Null, doc.RootElement.ValueKind);
		}

		// Array on empty reader -> []
		using (var reader = dt.CreateDataReader())
		using (var stream = new MemoryStream())
		using (var writer = new Utf8JsonWriter(stream))
		{
			await reader.ReadJson(JsonValueType.Array, writer);
			writer.Flush();
			stream.Position = 0;
			using var doc = JsonDocument.Parse(stream);
			Assert.AreEqual(JsonValueKind.Array, doc.RootElement.ValueKind);
			Assert.AreEqual(0, doc.RootElement.GetArrayLength());
		}
	}

	[TestMethod]
	public async Task ReadJsonAsync_ArrayResult_DirectUtf8OutputToStream()
	{
		var dt = new DataTable();
		dt.Columns.Add("Code", typeof(string));
		dt.Rows.Add("XYZ");
		dt.Rows.Add("ABC");

		using var reader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream))
		{
			await nuel.Db.ReadJson(reader, JsonValueType.Array, writer);
		}

		stream.Position = 0;
		using var doc = JsonDocument.Parse(stream);
		Assert.AreEqual(JsonValueKind.Array, doc.RootElement.ValueKind);
		Assert.AreEqual(2, doc.RootElement.GetArrayLength());
		Assert.AreEqual("XYZ", doc.RootElement[0].GetProperty("Code").GetString());
		Assert.AreEqual("ABC", doc.RootElement[1].GetProperty("Code").GetString());
	}

	[TestMethod]
	public void DbJson_HasNoStreamOverloadsOnDb_AndAllReturnTaskString()
	{
		var jsonMethods = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .Where(m => m.Name == "Json")
			 .ToList();
		Assert.IsTrue(jsonMethods.Count > 0, "Expected Json methods on Db.");

		// Verify no method takes a Stream
		var asyncStreamMethods = jsonMethods
			 .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(Stream)))
			 .ToList();
		Assert.AreEqual(0, asyncStreamMethods.Count, "Json should have no stream overloads on Db.");

		// Verify all public Json methods return Task<string>
		foreach (var m in jsonMethods)
		{
			Assert.AreEqual(typeof(Task<string>), m.ReturnType, $"Method {m} should return Task<string>.");
		}

		// Verify ComplexJson has no stream overloads
		var asyncComplexMethods = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .Where(m => m.Name == "ComplexJson" && m.GetParameters().Any(p => p.ParameterType == typeof(Stream)))
			 .ToList();
		Assert.AreEqual(0, asyncComplexMethods.Count, "ComplexJson should have no stream overloads on Db.");
	}

	[TestMethod]
	public async Task ReadJson_StringAndStream_MatchesForObjectAndArray_WithManySmallQueries()
	{
		for (int q = 0; q < 50; q++)
		{
			var dt = new DataTable();
			dt.Columns.Add("Id", typeof(int));
			dt.Columns.Add("Name", typeof(string));
			dt.Columns.Add("Active", typeof(bool));
			dt.Columns.Add("Ts", typeof(TimeSpan));

			for (int r = 0; r < 8; r++)
			{
				dt.Rows.Add(q * 10 + r, $"Name_{q}_{r}", r % 2 == 0, TimeSpan.FromMinutes(r * 15 + q));
			}

			// Test Array
			using var streamArr = new MemoryStream();
			using (var writer = new Utf8JsonWriter(streamArr))
			{
				using var readerArr = dt.CreateDataReader();
				await nuel.Db.ReadJson(readerArr, JsonValueType.Array, writer);
			}
			string streamArrJson = Encoding.UTF8.GetString(streamArr.ToArray());

			var bufferWriterArr = new System.Buffers.ArrayBufferWriter<byte>(1024);
			using (var writer = new Utf8JsonWriter(bufferWriterArr))
			{
				using var readerArr = dt.CreateDataReader();
				await nuel.Db.ReadJson(readerArr, JsonValueType.Array, writer);
			}
			string bufferArrJson = Encoding.UTF8.GetString(bufferWriterArr.WrittenSpan);

			Assert.AreEqual(streamArrJson, bufferArrJson);

			// Test Object
			using var streamObj = new MemoryStream();
			using (var writer = new Utf8JsonWriter(streamObj))
			{
				using var readerObj = dt.CreateDataReader();
				await nuel.Db.ReadJson(readerObj, JsonValueType.Object, writer);
			}
			string streamObjJson = Encoding.UTF8.GetString(streamObj.ToArray());

			var bufferWriterObj = new System.Buffers.ArrayBufferWriter<byte>(512);
			using (var writer = new Utf8JsonWriter(bufferWriterObj))
			{
				using var readerObj = dt.CreateDataReader();
				await nuel.Db.ReadJson(readerObj, JsonValueType.Object, writer);
			}
			string bufferObjJson = Encoding.UTF8.GetString(bufferWriterObj.WrittenSpan);

			Assert.AreEqual(streamObjJson, bufferObjJson);
		}
	}
}
