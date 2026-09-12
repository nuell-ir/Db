using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace nuel;

/// <summary>Streams a SQL Server query's first result set as UTF-8 JSON to an ASP.NET Core response.</summary>
/// <remarks>
/// Streams as JSON object or JSON array based on <see cref="JsonValueType"/>.
/// The connection is opened when MVC executes the result and disposed after streaming.
/// </remarks>
public sealed class DbJsonResult : ActionResult
{
	private readonly string _query;
	private readonly JsonValueType _result;
	private readonly bool _isStoredProc;
	private readonly SqlParameter[] _parameters;

	/// <summary>Creates a streaming JSON result without SQL parameters.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	public DbJsonResult(string query, JsonValueType result = JsonValueType.Object, bool isStoredProc = false)
		: this(query, result, isStoredProc, Data.NoParams) { }

	/// <summary>Creates a streaming JSON result without SQL parameters for a query or stored procedure.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	public DbJsonResult(string query, bool isStoredProc)
		: this(query, JsonValueType.Object, isStoredProc, Data.NoParams) { }

	/// <summary>Creates a streaming JSON result using <see cref="Db.ConnectionString"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	public DbJsonResult(string query, JsonValueType result, bool isStoredProc, params SqlParameter[] parameters)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);
		_query = query;
		_result = result;
		_isStoredProc = isStoredProc;
		_parameters = parameters ?? Data.NoParams;
	}

	/// <summary>Creates a streaming JSON result using <see cref="Db.ConnectionString"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	public DbJsonResult(string query, bool isStoredProc, params SqlParameter[] parameters)
		: this(query, JsonValueType.Object, isStoredProc, parameters) { }

	/// <summary>Creates a streaming JSON query result with named parameter values.</summary>
	/// <param name="query">The SQL query.</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbJsonResult(string query, params (string name, object value)[] parameters)
		: this(query, JsonValueType.Object, false, Data.SqlParams(parameters)) { }

	/// <summary>Creates a streaming JSON query result with named parameter values.</summary>
	/// <param name="query">The SQL query.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbJsonResult(string query, JsonValueType result, params (string name, object value)[] parameters)
		: this(query, result, false, Data.SqlParams(parameters)) { }

	/// <summary>Creates a streaming JSON query or stored procedure result with named parameter values.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbJsonResult(string query, bool isStoredProc, params (string name, object value)[] parameters)
		: this(query, JsonValueType.Object, isStoredProc, Data.SqlParams(parameters)) { }

	/// <summary>Creates a streaming JSON query or stored procedure result with named parameter values.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbJsonResult(string query, JsonValueType result, bool isStoredProc, params (string name, object value)[] parameters)
		: this(query, result, isStoredProc, Data.SqlParams(parameters)) { }

	/// <summary>Executes the query and streams its rows as JSON, honoring request cancellation.</summary>
	/// <param name="context">The MVC action context.</param>
	public override async Task ExecuteResultAsync(ActionContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		var cancellationToken = context.HttpContext.RequestAborted;
		cancellationToken.ThrowIfCancellationRequested();

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var command = new SqlCommand(_query, connection);
		if (_isStoredProc)
			command.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(command, _parameters);

		await connection.OpenAsync(cancellationToken);
		await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);
		await WriteResponseAsync(context, reader, _result);
	}

	internal static async Task WriteResponseAsync(ActionContext context, DbDataReader reader, JsonValueType result = JsonValueType.Object)
	{
		var response = context.HttpContext.Response;
		response.ContentType = "application/json; charset=utf-8";
		response.ContentLength = null;
		var cancellationToken = context.HttpContext.RequestAborted;
		cancellationToken.ThrowIfCancellationRequested();

		await using var writer = new Utf8JsonWriter(response.Body, Data.JsonWriterOptions);
		await reader.ReadJson(result, writer, cancellationToken);
		await writer.FlushAsync(cancellationToken);
	}
}

