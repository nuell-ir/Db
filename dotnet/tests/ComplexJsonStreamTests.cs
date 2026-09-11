using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class ComplexJsonStreamTests
{
	[TestMethod]
	public async Task ReadComplexJson_Stream_NullWhenAllResultsEmpty()
	{
		var ds = new DataSet();
		var dtScalar = ds.Tables.Add("Scalar");
		dtScalar.Columns.Add("Count", typeof(int));

		var dtObj = ds.Tables.Add("Obj");
		dtObj.Columns.Add("Name", typeof(string));

		var dtArr = ds.Tables.Add("Arr");
		dtArr.Columns.Add("Id", typeof(int));

		var dtCsv = ds.Tables.Add("Csv");
		dtCsv.Columns.Add("Code", typeof(string));

		var props = new (string Name, JsonValueType ResultType)[]
		{
				("ScalarResult", JsonValueType.Value),
				("ObjResult", JsonValueType.Object),
				("ArrResult", JsonValueType.Array),
				("CsvResult", JsonValueType.Csv),
		};

		using var reader = ds.CreateDataReader();
		using var stream = new MemoryStream();

		var result = await reader.ReadComplexJson(props, stream);
		Assert.IsNull(result);

		string json = Encoding.UTF8.GetString(stream.ToArray());
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("ScalarResult").ValueKind);
		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("ObjResult").ValueKind);
		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("ArrResult").ValueKind);
		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("CsvResult").ValueKind);
	}

	[TestMethod]
	public async Task ReadComplexJson_Stream_ProducesIdenticalOutputToString_ForAllTypes()
	{
		var ds = new DataSet();

		// 1. Scalar result (count)
		var dtCount = ds.Tables.Add("CountTable");
		dtCount.Columns.Add("Count", typeof(int));
		dtCount.Rows.Add(1200);

		// 2. Object result (single employee)
		var dtEmp = ds.Tables.Add("EmpTable");
		dtEmp.Columns.Add("Id", typeof(int));
		dtEmp.Columns.Add("Name", typeof(string));
		dtEmp.Columns.Add("Salary", typeof(decimal));
		dtEmp.Columns.Add("IsActive", typeof(bool));
		dtEmp.Rows.Add(1, "Alice Smith", 75000.50m, true);

		// 3. Array result (customers)
		var dtCust = ds.Tables.Add("CustTable");
		dtCust.Columns.Add("Id", typeof(int));
		dtCust.Columns.Add("Company", typeof(string));
		dtCust.Rows.Add(101, "Acme Corp");
		dtCust.Rows.Add(102, "Globex Inc");

		// 4. CSV result
		var dtCsv = ds.Tables.Add("CsvTable");
		dtCsv.Columns.Add("Code", typeof(string));
		dtCsv.Columns.Add("Qty", typeof(int));
		dtCsv.Rows.Add("A1", 50);
		dtCsv.Rows.Add("B2", 100);

		var props = new (string Name, JsonValueType ResultType)[]
		{
				("employeeCount", JsonValueType.Value),
				("oneEmployee", JsonValueType.Object),
				("customersArray", JsonValueType.Array),
				("inventoryCsv", JsonValueType.Csv)
		};

		// 1. Get expected string from string path
		using var stringReader = ds.CreateDataReader();
		string expected = await stringReader.ReadComplexJson(props, stream: null);
		Assert.IsNotNull(expected);

		// 2. Stream to MemoryStream using the stream path
		using var streamReader = ds.CreateDataReader();
		using var stream = new MemoryStream();
		string? result = await streamReader.ReadComplexJson(props, stream);

		Assert.IsNull(result);
		Assert.IsTrue(stream.Length > 0);

		string actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.AreEqual(expected, actual);

		// 3. Verify JSON integrity
		using var doc = JsonDocument.Parse(actual);
		var root = doc.RootElement;
		Assert.AreEqual(1200, root.GetProperty("employeeCount").GetInt32());
		Assert.AreEqual("Alice Smith", root.GetProperty("oneEmployee").GetProperty("Name").GetString());
		Assert.AreEqual(2, root.GetProperty("customersArray").GetArrayLength());
		Assert.AreEqual("Acme Corp", root.GetProperty("customersArray")[0].GetProperty("Company").GetString());
		Assert.IsNotNull(root.GetProperty("inventoryCsv").GetString());
		Assert.IsTrue(root.GetProperty("inventoryCsv").GetString()!.Contains("A1"));
	}

	[TestMethod]
	public async Task ReadComplexJson_DirectUtf8JsonWriter_WritesToExistingWriter()
	{
		var ds = new DataSet();
		var dtVal = ds.Tables.Add("Val");
		dtVal.Columns.Add("Number", typeof(int));
		dtVal.Rows.Add(42);

		var dtArr = ds.Tables.Add("Arr");
		dtArr.Columns.Add("Item", typeof(string));
		dtArr.Rows.Add("Alpha");
		dtArr.Rows.Add("Beta");

		var props = new (string Name, JsonValueType ResultType)[]
		{
				("val", JsonValueType.Value),
				("items", JsonValueType.Array)
		};

		using var reader = ds.CreateDataReader();
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions))
		{
			await reader.ReadComplexJson(props, writer);
		}

		string json = Encoding.UTF8.GetString(stream.ToArray());
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		Assert.AreEqual(42, root.GetProperty("val").GetInt32());
		Assert.AreEqual(2, root.GetProperty("items").GetArrayLength());
		Assert.AreEqual("Alpha", root.GetProperty("items")[0].GetProperty("Item").GetString());
	}

	[TestMethod]
	public void DbComplexJson_StreamOverloads_ExistInDb()
	{
		var asyncMethods = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .Where(m => m.Name == "ComplexJson" && m.GetParameters().Any(p => p.ParameterType == typeof(Stream)))
			 .ToList();

		// Should have at least 5 stream-enabled overloads
		Assert.IsTrue(asyncMethods.Count >= 5, $"Expected at least 5 ComplexJson stream overloads, found {asyncMethods.Count}.");

		// Verify optional stream parameter on main ComplexJson overload: ComplexJson(string, props, bool, Stream)
		var optionalStreamMethod = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .FirstOrDefault(m => m.Name == "ComplexJson" && m.GetParameters().Length == 4
				  && m.GetParameters()[1].ParameterType == typeof((string Name, JsonValueType ResultType)[])
				  && m.GetParameters()[2].ParameterType == typeof(bool)
				  && m.GetParameters()[3].ParameterType == typeof(Stream)
				  && m.GetParameters()[3].IsOptional);
		Assert.IsNotNull(optionalStreamMethod, "Expected ComplexJson method with optional stream parameter.");

		// Verify stream-second overload: ComplexJson(string, Stream, props, bool)
		var streamSecondMethod = typeof(nuel.Db).GetMethods(BindingFlags.Public | BindingFlags.Static)
			 .FirstOrDefault(m => m.Name == "ComplexJson" && m.GetParameters().Length == 4
				  && m.GetParameters()[1].ParameterType == typeof(Stream)
				  && m.GetParameters()[2].ParameterType == typeof((string Name, JsonValueType ResultType)[]));
		Assert.IsNotNull(streamSecondMethod, "Expected ComplexJson method with stream as second parameter.");
	}

	[TestMethod]
	public void DbComplexJson_NamedStreamCall_ResolvesUnambiguously()
	{
		var props = new (string Name, JsonValueType ResultType)[]
		{
				("test", JsonValueType.Value)
		};

		// Compile-time check: verifying Db.ComplexJson calls compile without CS0121 ambiguity
		Action compileCheck = () =>
		{
			_ = nuel.Db.ComplexJson("select 1", props);
			_ = nuel.Db.ComplexJson("select 1", props, true);
			_ = nuel.Db.ComplexJson("select 1", props, stream: Stream.Null);
			_ = nuel.Db.ComplexJson("select 1", props, true, Stream.Null);
			_ = nuel.Db.ComplexJson("select 1", Stream.Null, props);
			_ = nuel.Db.ComplexJson("select 1", Stream.Null, props, true);
		};
		Assert.IsNotNull(compileCheck);
	}

	[TestMethod]
	public async Task ReadComplexJson_AsyncOnlyStream_DoesNotThrowSyncException()
	{
		var ds = new DataSet();
		var dt = ds.Tables.Add("Data");
		dt.Columns.Add("Val", typeof(int));
		dt.Rows.Add(123);

		var props = new (string Name, JsonValueType ResultType)[]
		{
			("val", JsonValueType.Value)
		};

		using var reader = ds.CreateDataReader();
		using var stream = new AsyncOnlyStream();

		var result = await reader.ReadComplexJson(props, stream);
		Assert.IsNull(result);
		Assert.IsTrue(stream.Length > 0);
	}

	[TestMethod]
	public async Task ReadComplexJson_StringAndStream_MatchesForRepeatedSmallQueries()
	{
		for (int q = 0; q < 20; q++)
		{
			var ds = new DataSet();
			var dtScalar = ds.Tables.Add("Scalar");
			dtScalar.Columns.Add("Count", typeof(int));
			dtScalar.Rows.Add(q * 10);

			var dtObj = ds.Tables.Add("Obj");
			dtObj.Columns.Add("Name", typeof(string));
			dtObj.Columns.Add("Amount", typeof(decimal));
			dtObj.Rows.Add($"Item_{q}", q * 1.5m);

			var dtArr = ds.Tables.Add("Arr");
			dtArr.Columns.Add("Id", typeof(int));
			dtArr.Columns.Add("Active", typeof(bool));
			dtArr.Rows.Add(1, true);
			dtArr.Rows.Add(2, false);

			var props = new (string Name, JsonValueType ResultType)[]
			{
				("count", JsonValueType.Value),
				("item", JsonValueType.Object),
				("list", JsonValueType.Array)
			};

			using var stringReader = ds.CreateDataReader();
			string expected = await stringReader.ReadComplexJson(props, stream: null);

			using var streamReader = ds.CreateDataReader();
			using var stream = new MemoryStream();
			await streamReader.ReadComplexJson(props, stream);

			string actual = Encoding.UTF8.GetString(stream.ToArray());
			Assert.AreEqual(expected, actual);
		}
	}

	private sealed class AsyncOnlyStream : Stream
	{
		private readonly MemoryStream _inner = new();

		public override bool CanRead => _inner.CanRead;
		public override bool CanSeek => _inner.CanSeek;
		public override bool CanWrite => _inner.CanWrite;
		public override long Length => _inner.Length;
		public override long Position { get => _inner.Position; set => _inner.Position = value; }

		public override void Flush() => throw new InvalidOperationException("Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true instead.");
		public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous operations are disallowed.");
		public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
		public override void SetLength(long value) => _inner.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous operations are disallowed.");
		public override void Write(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException("Synchronous operations are disallowed.");

		public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			return _inner.WriteAsync(buffer, cancellationToken);
		}
	}
}
