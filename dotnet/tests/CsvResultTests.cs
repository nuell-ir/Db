using System.Data;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class DbCsvResultTests
{
	internal static Task WriteResponseAsync(System.Data.Common.DbDataReader reader, Stream stream)
	{
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = stream;
		return DbCsvResult.WriteResponseAsync(context, reader);
	}

	[TestMethod]
	public async Task Response_StreamsWideRowsMatchingExistingFormat_AndLeavesBodyOpen()
	{
		using var table = new DataTable();
		table.Columns.Add("Text", typeof(string));
		for (int i = 0; i < 200; i++)
			table.Columns.Add($"Number{i}", typeof(long));
		for (int row = 0; row < 5; row++)
			table.Rows.Add(new object[] { new string('é', 4096) }
				.Concat(Enumerable.Repeat<object>(long.MaxValue, 200)).ToArray());
		using var expectedReader = table.CreateDataReader();
		var expected = await expectedReader.ReadCsv();
		using var reader = table.CreateDataReader();
		using var body = new AsyncOnlyStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;
		context.HttpContext.Response.ContentLength = 1;

		await DbCsvResult.WriteResponseAsync(context, reader);

		Assert.AreEqual(expected, Encoding.UTF8.GetString(body.ToArray()));
		Assert.AreEqual("text/csv; charset=utf-8", context.HttpContext.Response.ContentType);
		Assert.IsNull(context.HttpContext.Response.ContentLength);
		Assert.IsTrue(body.CanWrite);
		Assert.IsTrue(body.Writes > 1);
	}

	[TestMethod]
	public async Task Response_EmptyResult_HasEmptyBody()
	{
		using var table = new DataTable();
		table.Columns.Add("Id", typeof(int));
		using var reader = table.CreateDataReader();
		using var body = new MemoryStream();
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.Response.Body = body;
		await DbCsvResult.WriteResponseAsync(context, reader);
		Assert.AreEqual(0L, body.Length);
	}

	[TestMethod]
	public async Task ExecuteResult_CancelledRequest_DoesNotOpenDatabase()
	{
		var context = new ActionContext { HttpContext = new DefaultHttpContext() };
		context.HttpContext.RequestAborted = new CancellationToken(true);
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			new DbCsvResult("select 1").ExecuteResultAsync(context));
	}

	[TestMethod]
	public void Constructors_SupportQueriesProceduresAndParameters()
	{
		Assert.IsInstanceOfType<ActionResult>(new DbCsvResult("select 1"));
		_ = new DbCsvResult("dbo.Report", true);
		_ = new DbCsvResult("select @id", ("id", 1));
		_ = new DbCsvResult("dbo.Report", true, ("id", 1));
		_ = new DbCsvResult("select @id", false, new Microsoft.Data.SqlClient.SqlParameter("id", 1));
		Assert.Throws<ArgumentException>(() => new DbCsvResult(" "));
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
