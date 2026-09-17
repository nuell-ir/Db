using System.Data;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class CsvStreamTests
{
	[TestMethod]
	[DataRow(DateTimeKind.Utc)]
	[DataRow(DateTimeKind.Local)]
	[DataRow(DateTimeKind.Unspecified)]
	public async Task Csv_Dates_PreserveWallClockAndOffsetAcrossAllWriters(DateTimeKind kind)
	{
		var items = new[] { -500L, 1500L }.Select(ms => new
		{
			Date = DateTime.SpecifyKind(DateTime.UnixEpoch.AddMilliseconds(ms), kind),
			Offset = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToOffset(TimeSpan.FromHours(3.5))
		}).ToArray();
		const string expected = "#Date~#Offset|1969-12-31T23:59:59~1970-01-01T03:29:59+03:30|1970-01-01T00:00:01~1970-01-01T03:30:01+03:30";
		Assert.AreEqual(expected, nuel.Db.Csv(items));

		var table = new DataTable();
		table.Columns.Add("Date", typeof(DateTime)).DateTimeMode = kind switch
		{
			DateTimeKind.Utc => DataSetDateTime.Utc,
			DateTimeKind.Local => DataSetDateTime.Local,
			_ => DataSetDateTime.Unspecified
		};
		table.Columns.Add("Offset", typeof(DateTimeOffset));
		foreach (var item in items)
			table.Rows.Add(item.Date, item.Offset);
		using var reader = table.CreateDataReader();
		Assert.AreEqual(expected, await reader.ReadCsv());
		using var streamReader = table.CreateDataReader();
		using var stream = new MemoryStream();
		await DbCsvResultTests.WriteResponseAsync(streamReader, stream);
		Assert.AreEqual(expected, Encoding.UTF8.GetString(stream.ToArray()));
	}

	[TestMethod]
	public async Task DbCsvResult_Stream_EmptyWhenNoRows()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		// No rows added

		using var reader = dt.CreateDataReader();
		using var stream = new MemoryStream();

		await DbCsvResultTests.WriteResponseAsync(reader, stream);

		Assert.AreEqual(0, stream.Length);
	}

	[TestMethod]
	public async Task DbCsvResult_Stream_ProducesIdenticalOutputToString_ForAllTypes()
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
		string expected = await stringReader.ReadCsv();
		Assert.IsNotNull(expected);

		// 2. Stream to MemoryStream using the stream path
		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		await DbCsvResultTests.WriteResponseAsync(streamReader, stream);

		Assert.IsTrue(stream.Length > 0);

		string actual = Encoding.UTF8.GetString(stream.ToArray());

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public async Task DbCsvResult_Stream_BuffersAndFlushesLargeDataset()
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
		string expected = await stringReader.ReadCsv();

		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		await DbCsvResultTests.WriteResponseAsync(streamReader, stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());

		Assert.AreEqual(expected, actual);
		Assert.IsTrue(actual.StartsWith("!Id~$Name~%Score~$Guid|0~Item Number 000000~0~"));
	}

	[TestMethod]
	public async Task DbCsvResult_Stream_LargeColumn_EnsuresCapacity()
	{
		var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("BigText", typeof(string));

		// 100,000 characters in a single column to test buffer expansion
		string hugeText = new string('A', 100000);
		dt.Rows.Add(1, hugeText);

		using var stringReader = dt.CreateDataReader();
		string expected = await stringReader.ReadCsv();

		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		await DbCsvResultTests.WriteResponseAsync(streamReader, stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());

		Assert.AreEqual(expected, actual);
		Assert.AreEqual(100000 + "!Id~$BigText|1~".Length, actual.Length);
	}

	[TestMethod]
	public void DbCsv_Overloads_ReturnStringsAndHaveNoStreamParameters()
	{
		var methods = typeof(nuel.Db).GetMethods()
			.Where(m => m.Name == "Csv").ToArray();
		Assert.AreEqual(5, methods.Length);
		foreach (var method in methods)
		{
			Assert.IsFalse(method.GetParameters().Any(p => typeof(Stream).IsAssignableFrom(p.ParameterType)));
			Assert.IsTrue(method.ReturnType == typeof(string) || method.ReturnType == typeof(Task<string>));
		}

		// Compile-time coverage without executing database queries.
		Action compileCheck = () =>
		{
			_ = nuel.Db.Csv("select 1");
			_ = nuel.Db.Csv("dbo.Report", true);
			_ = nuel.Db.Csv("select @id", ("id", 1));
			_ = nuel.Db.Csv("dbo.Report", true, ("id", 1));
			_ = nuel.Db.Csv("select @id", false, new Microsoft.Data.SqlClient.SqlParameter("id", 1));
		};
		Assert.IsNotNull(compileCheck);
	}

	[TestMethod]
	public async Task DbCsvResult_Stream_SmallDataset_MatchesStringPath()
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
		string expected = await stringReader.ReadCsv();

		using var streamReader = dt.CreateDataReader();
		using var stream = new MemoryStream();
		await DbCsvResultTests.WriteResponseAsync(streamReader, stream);

		string actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public async Task DbCsvResult_Stream_HundredsOfConsecutiveSmallQueries_MatchesStringPath()
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
			string expected = await stringReader.ReadCsv();

			using var streamReader = dt.CreateDataReader();
			using var stream = new MemoryStream();
			await DbCsvResultTests.WriteResponseAsync(streamReader, stream);

			string actual = Encoding.UTF8.GetString(stream.ToArray());
			Assert.AreEqual(expected, actual);
		}
	}
}
