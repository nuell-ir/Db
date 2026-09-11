using System.Data;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class MultiCsvStreamTests
{
	[TestMethod]
	public async Task ReadMultiCsv_Stream_NullWhenNoRows()
	{
		var ds = new DataSet();
		var dt1 = ds.Tables.Add("Table1");
		dt1.Columns.Add("Id", typeof(int));

		var dt2 = ds.Tables.Add("Table2");
		dt2.Columns.Add("Name", typeof(string));

		using var reader = ds.CreateDataReader();
		using var stream = new MemoryStream();

		var result = await reader.ReadMultiCsv(stream);

		Assert.IsNull(result);
		Assert.AreEqual(0, stream.Length);
	}

	[TestMethod]
	public async Task ReadMultiCsv_Stream_ProducesIdenticalOutputToString_ForAllTypes()
	{
		var guid = Guid.NewGuid();
		var now = DateTime.UtcNow;
		var dto = new DateTimeOffset(now);
		var ts = new TimeSpan(2, 45, 30);
		var bytes = new byte[] { 10, 20, 30, 40, 50 };

		var ds = new DataSet();

		// Table 1: All types
		var dt1 = ds.Tables.Add("AllTypes");
		dt1.Columns.Add("IntCol", typeof(int));
		dt1.Columns.Add("LongCol", typeof(long));
		dt1.Columns.Add("ShortCol", typeof(short));
		dt1.Columns.Add("ByteCol", typeof(byte));
		dt1.Columns.Add("FloatCol", typeof(float));
		dt1.Columns.Add("DoubleCol", typeof(double));
		dt1.Columns.Add("DecimalCol", typeof(decimal));
		dt1.Columns.Add("DateTimeCol", typeof(DateTime));
		dt1.Columns.Add("DateTimeOffsetCol", typeof(DateTimeOffset));
		dt1.Columns.Add("TimeSpanCol", typeof(TimeSpan));
		dt1.Columns.Add("GuidCol", typeof(Guid));
		dt1.Columns.Add("BytesCol", typeof(byte[]));
		dt1.Columns.Add("BoolCol", typeof(bool));
		dt1.Columns.Add("StringCol", typeof(string));

		dt1.Rows.Add(
			 42,
			 1234567890123L,
			 (short)32000,
			 (byte)255,
			 3.14159f,
			 2.718281828459,
			 99999.99m,
			 now,
			 dto,
			 ts,
			 guid,
			 bytes,
			 true,
			 "Hello ~ World | Test"
		);

		dt1.Rows.Add(
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 DBNull.Value,
			 false,
			 DBNull.Value
		);

		// Table 2: Customers
		var dt2 = ds.Tables.Add("Customers");
		dt2.Columns.Add("CustomerId", typeof(int));
		dt2.Columns.Add("CompanyName", typeof(string));
		dt2.Columns.Add("Balance", typeof(decimal));
		dt2.Rows.Add(101, "Acme Corp", 1500.75m);
		dt2.Rows.Add(102, "Globex Inc", -250.00m);

		// 1. Expected output using string path
		using var stringReader = ds.CreateDataReader();
		string[]? stringResults = await stringReader.ReadMultiCsv(null);
		Assert.IsNotNull(stringResults);
		Assert.AreEqual(2, stringResults.Length);

		string expected = string.Join('\n', stringResults);

		// 2. Stream to MemoryStream
		using var streamReader = ds.CreateDataReader();
		using var stream = new MemoryStream();
		string[]? streamResult = await streamReader.ReadMultiCsv(stream);

		Assert.IsNull(streamResult);
		Assert.IsTrue(stream.Length > 0);

		string actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public async Task ReadMultiCsv_Stream_HandlesPartiallyEmptyResultSets()
	{
		var ds = new DataSet();

		// Table 1: Empty
		var dt1 = ds.Tables.Add("EmptyTable");
		dt1.Columns.Add("Id", typeof(int));

		// Table 2: Populated
		var dt2 = ds.Tables.Add("Items");
		dt2.Columns.Add("Code", typeof(string));
		dt2.Columns.Add("Qty", typeof(int));
		dt2.Rows.Add("A1", 10);
		dt2.Rows.Add("B2", 20);

		// Table 3: Empty
		var dt3 = ds.Tables.Add("AnotherEmpty");
		dt3.Columns.Add("Desc", typeof(string));

		// String path
		using var stringReader = ds.CreateDataReader();
		string[]? stringResults = await stringReader.ReadMultiCsv(null);
		Assert.IsNotNull(stringResults);
		Assert.AreEqual(3, stringResults.Length);
		Assert.IsNull(stringResults[0]);
		Assert.IsNotNull(stringResults[1]);
		Assert.IsNull(stringResults[2]);

		// Stream path: should contain only the populated table without extra newlines
		using var streamReader = ds.CreateDataReader();
		using var stream = new MemoryStream();
		await streamReader.ReadMultiCsv(stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.AreEqual(stringResults[1], actual);
		Assert.IsFalse(actual.StartsWith("\n"));
		Assert.IsFalse(actual.EndsWith("\n"));
	}

	[TestMethod]
	public async Task ReadMultiCsv_Stream_BuffersAndFlushesLargeDatasets()
	{
		var ds = new DataSet();

		var dt1 = ds.Tables.Add("Large1");
		dt1.Columns.Add("Id", typeof(int));
		dt1.Columns.Add("Text", typeof(string));
		for (int i = 0; i < 1500; i++)
			dt1.Rows.Add(i, $"Description {i:D6}");

		var dt2 = ds.Tables.Add("Large2");
		dt2.Columns.Add("Id", typeof(int));
		dt2.Columns.Add("Score", typeof(double));
		for (int i = 0; i < 1500; i++)
			dt2.Rows.Add(i, i * 2.5);

		using var stringReader = ds.CreateDataReader();
		string[]? stringResults = await stringReader.ReadMultiCsv(null);
		Assert.IsNotNull(stringResults);

		string expected = string.Join('\n', stringResults);

		using var streamReader = ds.CreateDataReader();
		using var stream = new MemoryStream();
		await streamReader.ReadMultiCsv(stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void DbMultiCsv_StreamOverloads_ExistInDb()
	{
		var asyncMethods = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .Where(m => m.Name == "MultiCsv" && m.GetParameters().Any(p => p.ParameterType == typeof(Stream)))
			 .ToList();

		Assert.IsTrue(asyncMethods.Count >= 4, $"Expected at least 4 MultiCsv stream overloads, found {asyncMethods.Count}.");

		// Verify stream-second overload: MultiCsv(string, Stream, bool)
		var streamSecondMethod = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .FirstOrDefault(m => m.Name == "MultiCsv" && m.GetParameters().Length == 3
				  && m.GetParameters()[1].ParameterType == typeof(Stream)
				  && m.GetParameters()[2].Name == "isStoredProc");
		Assert.IsNotNull(streamSecondMethod, "Expected MultiCsv method with stream as second parameter.");

		// Verify original overload: MultiCsv(string, bool) preserving binary compatibility
		var originalMethod = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .FirstOrDefault(m => m.Name == "MultiCsv" && m.GetParameters().Length == 2
				  && m.GetParameters()[0].ParameterType == typeof(string)
				  && m.GetParameters()[1].ParameterType == typeof(bool));
		Assert.IsNotNull(originalMethod, "Expected original MultiCsv(string, bool) method.");
	}

	[TestMethod]
	public void DbMultiCsv_NamedStreamCall_ResolvesUnambiguously()
	{
		// Compile-time check: verifying Db.MultiCsv calls compile without CS0121 ambiguity
		Action compileCheck = () =>
		{
			_ = nuel.Db.MultiCsv("select 1", stream: Stream.Null);
			_ = nuel.Db.MultiCsv("select 1");
			_ = nuel.Db.MultiCsv("select 1", true);
			_ = nuel.Db.MultiCsv("select 1", Stream.Null);
			_ = nuel.Db.MultiCsv("select 1", Stream.Null, true);
		};
		Assert.IsNotNull(compileCheck);
	}
}

