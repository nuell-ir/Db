using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class DbComplexJsonResultTests
{
	internal static Task WriteResponseAsync(System.Data.Common.DbDataReader reader, (string Name, JsonValueType ResultType)[] props, Stream stream)
	{
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = stream;
		return DbComplexJsonResult.WriteResponseAsync(context, reader, props);
	}

	[TestMethod]
	public async Task Response_StreamsComplexJsonMatchingStringFormat_AndLeavesBodyOpen()
	{
		var ds = new DataSet();
		var dtCount = ds.Tables.Add("CountTable");
		dtCount.Columns.Add("Count", typeof(int));
		dtCount.Rows.Add(1200);

		var dtEmp = ds.Tables.Add("EmpTable");
		dtEmp.Columns.Add("Id", typeof(int));
		dtEmp.Columns.Add("Name", typeof(string));
		dtEmp.Rows.Add(1, "Alice Smith");

		var dtCust = ds.Tables.Add("CustTable");
		dtCust.Columns.Add("Id", typeof(int));
		dtCust.Columns.Add("Company", typeof(string));
		dtCust.Rows.Add(101, "Acme Corp");
		dtCust.Rows.Add(102, "Globex Inc");

		var dtCsv = ds.Tables.Add("CsvTable");
		dtCsv.Columns.Add("Code", typeof(string));
		dtCsv.Columns.Add("Qty", typeof(int));
		dtCsv.Rows.Add("A1", 50);

		var props = new (string Name, JsonValueType ResultType)[]
		{
			("employeeCount", JsonValueType.Value),
			("oneEmployee", JsonValueType.Object),
			("customersArray", JsonValueType.Array),
			("inventoryCsv", JsonValueType.Csv)
		};

		using var expectedReader = ds.CreateDataReader();
		var expected = await expectedReader.ReadComplexJson(props);

		using var reader = ds.CreateDataReader();
		using var body = new AsyncOnlyStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;
		context.HttpContext.Response.ContentLength = 1;

		await DbComplexJsonResult.WriteResponseAsync(context, reader, props);

		var actual = Encoding.UTF8.GetString(body.ToArray());
		Assert.AreEqual(expected, actual);
		Assert.AreEqual("application/json; charset=utf-8", context.HttpContext.Response.ContentType);
		Assert.IsNull(context.HttpContext.Response.ContentLength);
		Assert.IsTrue(body.CanWrite);

		using var doc = JsonDocument.Parse(actual);
		var root = doc.RootElement;
		Assert.AreEqual(1200, root.GetProperty("employeeCount").GetInt32());
		Assert.AreEqual("Alice Smith", root.GetProperty("oneEmployee").GetProperty("Name").GetString());
		Assert.AreEqual(2, root.GetProperty("customersArray").GetArrayLength());
	}

	[TestMethod]
	public async Task Response_EmptyResults_StreamsNullProperties()
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
		using var body = new MemoryStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;

		await DbComplexJsonResult.WriteResponseAsync(context, reader, props);

		var json = Encoding.UTF8.GetString(body.ToArray());
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("ScalarResult").ValueKind);
		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("ObjResult").ValueKind);
		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("ArrResult").ValueKind);
		Assert.AreEqual(JsonValueKind.Null, root.GetProperty("CsvResult").ValueKind);
	}

	[TestMethod]
	public async Task ExecuteResult_CancelledRequest_DoesNotOpenDatabase()
	{
		var props = new (string Name, JsonValueType ResultType)[]
		{
			("count", JsonValueType.Value)
		};
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.RequestAborted = new CancellationToken(true);
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			new DbComplexJsonResult("select 1", props).ExecuteResultAsync(context));
	}

	[TestMethod]
	public void Constructors_SupportQueriesProceduresAndParameters()
	{
		var props = new (string Name, JsonValueType ResultType)[]
		{
			("id", JsonValueType.Value)
		};

		Assert.IsInstanceOfType<ActionResult>(new DbComplexJsonResult("select 1", props));
		_ = new DbComplexJsonResult("dbo.Report", props, true);
		_ = new DbComplexJsonResult("select @id", props, ("id", 1));
		_ = new DbComplexJsonResult("dbo.Report", props, true, ("id", 1));
		_ = new DbComplexJsonResult("select @id", props, false, new Microsoft.Data.SqlClient.SqlParameter("id", 1));
		Assert.Throws<ArgumentException>(() => new DbComplexJsonResult(" ", props));
		Assert.Throws<ArgumentNullException>(() => new DbComplexJsonResult("select 1", null));
	}

	private sealed class AsyncOnlyStream : MemoryStream
	{
		public int Writes { get; private set; }
		public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous write");
		public override void Flush() => throw new InvalidOperationException("Synchronous flush");
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Writes++;
			var bytes = buffer.ToArray();
			base.Write(bytes, 0, bytes.Length);
			return ValueTask.CompletedTask;
		}
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}
	}
}

