using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace nuel;

/// <summary>Streams a SQL Server query's first result set as UTF-8 CSV to an ASP.NET Core response.</summary>
/// <remarks>
/// Uses the same custom format as <see cref="Db.Csv(string, bool)"/>:
/// type-prefixed headers, ~ column separators, | row separators, and Ø for nulls.
/// This is not comma-delimited CSV. Empty results produce an empty response body.
/// The connection is opened when ASP.NET Core executes the result and disposed after streaming.
/// </remarks>
public sealed class DbCsvResult : ActionResult, IResult
{
	private readonly string _query;
	private readonly bool _isStoredProc;
	private readonly SqlParameter[] _parameters;

	/// <summary>Creates a streaming result without SQL parameters.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	public DbCsvResult(string query, bool isStoredProc = false)
		: this(query, isStoredProc, Data.NoParams) { }

	/// <summary>Creates a streaming result using <see cref="Db.ConnectionString"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	public DbCsvResult(string query, bool isStoredProc, params SqlParameter[] parameters)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);
		_query = query;
		_isStoredProc = isStoredProc;
		_parameters = parameters ?? Data.NoParams;
	}

	/// <summary>Creates a streaming query result with named parameter values.</summary>
	/// <param name="query">The SQL query.</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbCsvResult(string query, params (string name, object value)[] parameters)
		: this(query, false, Data.SqlParams(parameters)) { }

	/// <summary>Creates a streaming query or stored procedure result with named parameter values.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbCsvResult(string query, bool isStoredProc, params (string name, object value)[] parameters)
		: this(query, isStoredProc, Data.SqlParams(parameters)) { }

	/// <summary>Executes the query and streams its rows, honoring request cancellation.</summary>
	/// <param name="context">The MVC action context.</param>
	public override Task ExecuteResultAsync(ActionContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		return ExecuteAsync(context.HttpContext);
	}

	/// <summary>Executes the query and streams its rows, honoring request cancellation.</summary>
	/// <param name="context">The HTTP context.</param>
	public async Task ExecuteAsync(HttpContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		var cancellationToken = context.RequestAborted;
		cancellationToken.ThrowIfCancellationRequested();

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var command = new SqlCommand(_query, connection);
		if (_isStoredProc)
			command.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(command, _parameters);

		await connection.OpenAsync(cancellationToken);
		await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);
		await WriteResponseAsync(context, reader);
	}

	internal static async Task WriteResponseAsync(HttpContext context, DbDataReader reader)
	{
		var response = context.Response;
		response.ContentType = "text/csv; charset=utf-8";
		response.ContentLength = null;
		var cancellationToken = context.RequestAborted;
		cancellationToken.ThrowIfCancellationRequested();
		if (!await reader.ReadAsync(cancellationToken))
			return;

		await using var writer = new Utf8CsvStreamWriter(response.Body, cancellationToken);
		var colTypes = await writer.WriteCsvHeaderAsync(reader);
		await writer.WriteCsvRowAsync(reader, colTypes);
		while (await reader.ReadAsync(cancellationToken))
		{
			if (writer.FreeCapacity < 256)
				await writer.FlushAsync();
			await writer.WriteCsvRowAsync(reader, colTypes);
		}
		await writer.FinishAsync();
	}
}
