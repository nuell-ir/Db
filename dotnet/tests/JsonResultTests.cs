using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class DbJsonResultTests
{
	internal static Task WriteResponseAsync(System.Data.Common.DbDataReader reader, Stream stream, JsonValueType result = JsonValueType.Object)
	{
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = stream;
		return DbJsonResult.WriteResponseAsync(context, reader, result);
	}

	[TestMethod]
	public async Task Response_StreamsJsonObject_AndLeavesBodyOpen()
	{
		using var table = new DataTable();
		table.Columns.Add("Id", typeof(int));
		table.Columns.Add("Name", typeof(string));
		table.Columns.Add("Score", typeof(double));
		table.Rows.Add(1, "Alice", 99.5);

		using var reader = table.CreateDataReader();
		using var body = new AsyncOnlyStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;
		context.HttpContext.Response.ContentLength = 1;

		await DbJsonResult.WriteResponseAsync(context, reader, JsonValueType.Object);

		var actual = Encoding.UTF8.GetString(body.ToArray());
		Assert.AreEqual("application/json; charset=utf-8", context.HttpContext.Response.ContentType);
		Assert.IsNull(context.HttpContext.Response.ContentLength);
		Assert.IsTrue(body.CanWrite);

		using var doc = JsonDocument.Parse(actual);
		var root = doc.RootElement;
		Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
		Assert.AreEqual(1, root.GetProperty("Id").GetInt32());
		Assert.AreEqual("Alice", root.GetProperty("Name").GetString());
		Assert.AreEqual(99.5, root.GetProperty("Score").GetDouble());
	}

	[TestMethod]
	public async Task Response_StreamsJsonArray_AndLeavesBodyOpen()
	{
		using var table = new DataTable();
		table.Columns.Add("Id", typeof(int));
		table.Columns.Add("Title", typeof(string));
		table.Rows.Add(1, "Item A");
		table.Rows.Add(2, "Item B");

		using var reader = table.CreateDataReader();
		using var body = new AsyncOnlyStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;
		context.HttpContext.Response.ContentLength = 1;

		await DbJsonResult.WriteResponseAsync(context, reader, JsonValueType.Array);

		var actual = Encoding.UTF8.GetString(body.ToArray());
		Assert.AreEqual("application/json; charset=utf-8", context.HttpContext.Response.ContentType);
		Assert.IsNull(context.HttpContext.Response.ContentLength);
		Assert.IsTrue(body.CanWrite);

		using var doc = JsonDocument.Parse(actual);
		var root = doc.RootElement;
		Assert.AreEqual(JsonValueKind.Array, root.ValueKind);
		Assert.AreEqual(2, root.GetArrayLength());
		Assert.AreEqual(1, root[0].GetProperty("Id").GetInt32());
		Assert.AreEqual("Item A", root[0].GetProperty("Title").GetString());
		Assert.AreEqual(2, root[1].GetProperty("Id").GetInt32());
		Assert.AreEqual("Item B", root[1].GetProperty("Title").GetString());
	}

	[TestMethod]
	public async Task Response_EmptyResult_Object_StreamsNull()
	{
		using var table = new DataTable();
		table.Columns.Add("Id", typeof(int));
		using var reader = table.CreateDataReader();
		using var body = new MemoryStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;

		await DbJsonResult.WriteResponseAsync(context, reader, JsonValueType.Object);

		var actual = Encoding.UTF8.GetString(body.ToArray());
		using var doc = JsonDocument.Parse(actual);
		Assert.AreEqual(JsonValueKind.Null, doc.RootElement.ValueKind);
	}

	[TestMethod]
	public async Task Response_EmptyResult_Array_StreamsEmptyArray()
	{
		using var table = new DataTable();
		table.Columns.Add("Id", typeof(int));
		using var reader = table.CreateDataReader();
		using var body = new MemoryStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;

		await DbJsonResult.WriteResponseAsync(context, reader, JsonValueType.Array);

		var actual = Encoding.UTF8.GetString(body.ToArray());
		using var doc = JsonDocument.Parse(actual);
		Assert.AreEqual(JsonValueKind.Array, doc.RootElement.ValueKind);
		Assert.AreEqual(0, doc.RootElement.GetArrayLength());
	}

	[TestMethod]
	public async Task ExecuteResult_CancelledRequest_DoesNotOpenDatabase()
	{
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.RequestAborted = new CancellationToken(true);
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			new DbJsonResult("select 1").ExecuteResultAsync(context));
	}

	[TestMethod]
	public void Constructors_SupportQueriesProceduresAndParameters()
	{
		Assert.IsInstanceOfType<ActionResult>(new DbJsonResult("select 1"));
		_ = new DbJsonResult("select 1", JsonValueType.Array);
		_ = new DbJsonResult("dbo.Report", true);
		_ = new DbJsonResult("dbo.Report", JsonValueType.Array, true);
		_ = new DbJsonResult("select @id", ("id", 1));
		_ = new DbJsonResult("select @id", JsonValueType.Array, ("id", 1));
		_ = new DbJsonResult("dbo.Report", true, ("id", 1));
		_ = new DbJsonResult("dbo.Report", JsonValueType.Array, true, ("id", 1));
		_ = new DbJsonResult("select @id", false, new Microsoft.Data.SqlClient.SqlParameter("id", 1));
		_ = new DbJsonResult("select @id", JsonValueType.Array, false, new Microsoft.Data.SqlClient.SqlParameter("id", 1));
		Assert.Throws<ArgumentException>(() => new DbJsonResult(" "));
		Assert.Throws<ArgumentNullException>(() => new DbJsonResult(null!));
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

