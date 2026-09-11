using System.Data;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class CsvStreamTests
{
	[TestMethod]
	public async Task ReadCsv_Stream_NullWhenNoRows()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		// No rows added

		using var reader = dt.CreateDataReader();
		using var stream = new MemoryStream();

		var result = await reader.ReadCsv(stream);

		Assert.IsNull(result);
		Assert.AreEqual(0, stream.Length);
	}

	[TestMethod]
	public async Task ReadCsv_Stream_ProducesIdenticalOutputToString_ForAllTypes()
	{
		var guid = Guid.NewGuid();
		var now = DateTime.UtcNow;
		var dto = new DateTimeOffset(now);
		var ts = new TimeSpan(2, 45, 30);
		var bytes = new byte[] { 10, 20, 30, 40, 50 };

		var dt = new DataTable();
		dt.Columns.Add("IntCol", typeof(int));
		dt.Columns.Add("LongCol", typeof(long));
		dt.Columns.Add("ShortCol", typeof(short));
		dt.Columns.Add("ByteCol", typeof(byte));
		dt.Columns.Add("FloatCol", typeof(float));
		dt.Columns.Add("DoubleCol", typeof(double));
		dt.Columns.Add("DecimalCol", typeof(decimal));
		dt.Columns.Add("DateTimeCol", typeof(DateTime));
		dt.Columns.Add("DateTimeOffsetCol", typeof(DateTimeOffset));
		dt.Columns.Add("TimeSpanCol", typeof(TimeSpan));
		dt.Columns.Add("GuidCol", typeof(Guid));
		dt.Columns.Add("BytesCol", typeof(byte[]));
		dt.Columns.Add("BoolCol", typeof(bool));
		dt.Columns.Add("StringCol", typeof(string));

		// Row 1: all values populated
		dt.Rows.Add(
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

		// Row 2: with nulls and false
		dt.Rows.Add(
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

		// 1. Get expected string using the string path
		using var stringReader = dt.CreateDataReader();
		string expected = await stringReader.ReadCsv(null);
		Assert.IsNotNull(expected);

		// 2. Stream to MemoryStream using the stream path
		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		string? result = await streamReader.ReadCsv(stream);

		Assert.IsNull(result);
		Assert.IsTrue(stream.Length > 0);

		string actual = Encoding.UTF8.GetString(stream.ToArray());

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public async Task ReadCsv_Stream_BuffersAndFlushesLargeDataset()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Columns.Add("Score", typeof(double));
		dt.Columns.Add("Guid", typeof(Guid));

		// 2,000 rows will easily exceed the 32 KB buffer threshold (~150 KB)
		for (int i = 0; i < 2000; i++)
		{
			dt.Rows.Add(i, $"Item Number {i:D6}", i * 1.5, Guid.NewGuid());
		}

		using var stringReader = dt.CreateDataReader();
		string expected = await stringReader.ReadCsv(null);

		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		await streamReader.ReadCsv(stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());

		Assert.AreEqual(expected, actual);
		Assert.IsTrue(actual.StartsWith("!Id~$Name~%Score~$Guid|0~Item Number 000000~0~"));
	}

	[TestMethod]
	public async Task ReadCsv_Stream_LargeColumn_EnsuresCapacity()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("BigText", typeof(string));

		// 100,000 characters in a single column to test buffer expansion
		string hugeText = new string('A', 100000);
		dt.Rows.Add(1, hugeText);

		using var stringReader = dt.CreateDataReader();
		string expected = await stringReader.ReadCsv(null);

		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		await streamReader.ReadCsv(stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());

		Assert.AreEqual(expected, actual);
		Assert.AreEqual(100000 + "!Id~$BigText|1~".Length, actual.Length);
	}

	[TestMethod]
	public void DbCsv_StreamOverloads_ExistInDb()
	{
		var asyncMethods = typeof(nuel.Db).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
			 .Where(m => m.Name == "Csv" && m.GetParameters().Any(p => p.ParameterType == typeof(Stream)))
			 .ToList();
		Assert.IsTrue(asyncMethods.Count >= 4, $"Expected at least 4 Csv stream overloads, found {asyncMethods.Count}.");

		// Verify stream-second overload: Csv(string, Stream, bool)
		var streamSecondMethod = typeof(nuel.Db).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
			 .FirstOrDefault(m => m.Name == "Csv" && m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Stream) && m.GetParameters()[2].Name == "isStoredProc");
		Assert.IsNotNull(streamSecondMethod, "Expected Csv method with stream as second parameter.");

		// Verify original overload: Csv(string, bool) preserving binary compatibility
		var originalMethod = typeof(nuel.Db).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
			 .FirstOrDefault(m => m.Name == "Csv" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string) && m.GetParameters()[1].ParameterType == typeof(bool));
		Assert.IsNotNull(originalMethod, "Expected original Csv(string, bool) method.");
	}

	[TestMethod]
	public void DbCsv_NamedStreamCall_ResolvesUnambiguously()
	{
		// Compile-time check: verifying Db.Csv calls compile without CS0121 ambiguity
		Action compileCheck = () =>
		{
			_ = nuel.Db.Csv("select 1", stream: Stream.Null);
			_ = nuel.Db.Csv("select 1");
			_ = nuel.Db.Csv("select 1", true);
			_ = nuel.Db.Csv("select 1", Stream.Null);
			_ = nuel.Db.Csv("select 1", Stream.Null, true);
		};
		Assert.IsNotNull(compileCheck);
	}

	[TestMethod]
	public async Task ReadCsv_Stream_SmallDataset_MatchesStringPath()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Code", typeof(string));
		dt.Columns.Add("Amount", typeof(decimal));
		dt.Columns.Add("Active", typeof(bool));
		dt.Columns.Add("Created", typeof(DateTime));

		var baseDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		for (int i = 0; i < 25; i++)
		{
			dt.Rows.Add(i + 1, $"ITEM-{i:D4}", (i + 1) * 19.99m, i % 2 == 0, baseDate.AddHours(i));
		}

		using var stringReader = dt.CreateDataReader();
		string expected = await stringReader.ReadCsv(null);

		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		await streamReader.ReadCsv(stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public async Task ReadCsv_Stream_HundredsOfConsecutiveSmallQueries_MatchesStringPath()
	{
		for (int q = 0; q < 100; q++)
		{
			var dt = new DataTable();
			dt.Columns.Add("Id", typeof(int));
			dt.Columns.Add("Tag", typeof(string));
			dt.Columns.Add("Score", typeof(double));

			for (int r = 0; r < 10; r++)
			{
				dt.Rows.Add(q * 100 + r, $"tag_{q}_{r}", r * 2.5);
			}

			using var stringReader = dt.CreateDataReader();
			string expected = await stringReader.ReadCsv(null);

			using var streamReader = dt.CreateDataReader();
			using var stream = new MemoryStream();
			await streamReader.ReadCsv(stream);

			string actual = Encoding.UTF8.GetString(stream.ToArray());
			Assert.AreEqual(expected, actual);
		}
	}
}

