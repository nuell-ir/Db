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

	[TestMethod]
	public void Data_SqlParams_HandlesNullEmptyAndValues()
	{
		Assert.AreSame(Data.NoParams, Data.SqlParams(null!));
		Assert.AreSame(Data.NoParams, Data.SqlParams([]));

		var parameters = Data.SqlParams([("id", 42), ("name", null!)]);
		Assert.AreEqual(2, parameters.Length);
		Assert.AreEqual("@id", parameters[0].ParameterName.StartsWith('@') ? parameters[0].ParameterName : "@" + parameters[0].ParameterName);
		Assert.AreEqual(42, parameters[0].Value);
		Assert.AreEqual(DBNull.Value, parameters[1].Value);
	}

	[TestMethod]
	public void Val_ReaderPattern_HandlesMatchingMismatchAndNulls()
	{
		var dt = new DataTable();
		dt.Columns.Add("IntCol", typeof(int));
		dt.Columns.Add("ShortCol", typeof(short));
		dt.Columns.Add("GuidCol", typeof(Guid));
		var expectedGuid = Guid.NewGuid();
		dt.Rows.Add(100, (short)5, expectedGuid);
		dt.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value);

		using var reader = dt.CreateDataReader();

		// Row 1: Valid values
		Assert.IsTrue(reader.Read());

		// Test matching type direct read
		int intVal = reader.GetFieldType(0) == typeof(int) ? reader.GetFieldValue<int>(0) : (int)Convert.ChangeType(reader.GetValue(0), typeof(int));
		Assert.AreEqual(100, intVal);

		// Test type conversion fallback (short -> int)
		int convertedVal = reader.GetFieldType(1) == typeof(int) ? reader.GetFieldValue<int>(1) : (int)Convert.ChangeType(reader.GetValue(1), typeof(int));
		Assert.AreEqual(5, convertedVal);

		// Test Guid matching type direct read
		Guid guidVal = reader.GetFieldType(2) == typeof(Guid) ? reader.GetFieldValue<Guid>(2) : (Guid)Convert.ChangeType(reader.GetValue(2), typeof(Guid));
		Assert.AreEqual(expectedGuid, guidVal);

		// Row 2: DBNull values
		Assert.IsTrue(reader.Read());
		int nullVal = reader.FieldCount > 0 && !reader.IsDBNull(0) ? (reader.GetFieldType(0) == typeof(int) ? reader.GetFieldValue<int>(0) : (int)Convert.ChangeType(reader.GetValue(0), typeof(int))) : default;
		Assert.AreEqual(0, nullVal);

		// Row 3: No more rows
		Assert.IsFalse(reader.Read());
	}

	[TestMethod]
	public void Str_ReaderPattern_HandlesStringsAndNonStrings()
	{
		var dt = new DataTable();
		dt.Columns.Add("StringCol", typeof(string));
		dt.Columns.Add("IntCol", typeof(int));
		dt.Rows.Add("TestString", 999);
		dt.Rows.Add(DBNull.Value, DBNull.Value);

		using var reader = dt.CreateDataReader();

		// Row 1
		Assert.IsTrue(reader.Read());
		string? strResult = reader.GetFieldType(0) == typeof(string) ? reader.GetString(0) : reader.GetValue(0).ToString();
		Assert.AreEqual("TestString", strResult);

		string? intConvertedStr = reader.GetFieldType(1) == typeof(string) ? reader.GetString(1) : reader.GetValue(1).ToString();
		Assert.AreEqual("999", intConvertedStr);

		// Row 2
		Assert.IsTrue(reader.Read());
		string? nullResult = reader.FieldCount > 0 && !reader.IsDBNull(0) ? (reader.GetFieldType(0) == typeof(string) ? reader.GetString(0) : reader.GetValue(0).ToString()) : null;
		Assert.IsNull(nullResult);
	}

	[TestMethod]
	public void List_ReaderPattern_HandlesNullsAndConversions()
	{
		var dt = new DataTable();
		dt.Columns.Add("ShortCol", typeof(short));
		dt.Rows.Add((short)10);
		dt.Rows.Add(DBNull.Value);
		dt.Rows.Add((short)30);

		using var reader = dt.CreateDataReader();
		var list = new List<int>();
		bool sameType = reader.GetFieldType(0) == typeof(int);
		while (reader.Read())
		{
			if (reader.IsDBNull(0))
				list.Add(default);
			else if (sameType)
				list.Add(reader.GetFieldValue<int>(0));
			else
			{
				var val = reader.GetValue(0);
				list.Add(val is int t ? t : (int)Convert.ChangeType(val, typeof(int)));
			}
		}

		Assert.AreEqual(3, list.Count);
		Assert.AreEqual(10, list[0]);
		Assert.AreEqual(0, list[1]);
		Assert.AreEqual(30, list[2]);
	}

	[TestMethod]
	public void Values_ReaderPattern_SingleAndMultiColumn()
	{
		var dt = new DataTable();
		dt.Columns.Add("A", typeof(int));
		dt.Columns.Add("B", typeof(string));
		dt.Rows.Add(1, "one");
		dt.Rows.Add(2, "two");

		using var reader = dt.CreateDataReader();
		var results = new List<object>();
		int fieldCount = reader.FieldCount;
		var values = new object[fieldCount];
		while (reader.Read())
		{
			reader.GetValues(values);
			for (int i = 0; i < fieldCount; i++)
				results.Add(values[i]);
		}

		Assert.AreEqual(4, results.Count);
		Assert.AreEqual(1, results[0]);
		Assert.AreEqual("one", results[1]);
		Assert.AreEqual(2, results[2]);
		Assert.AreEqual("two", results[3]);
	}
}

