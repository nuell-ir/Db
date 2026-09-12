using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace nuel;

/// <summary>Streams a SQL Server query with multiple result sets as UTF-8 JSON to an ASP.NET Core response.</summary>
/// <remarks>
/// Uses the property names and result types defined in <paramref name="props"/>:
/// <see cref="JsonValueType.Value"/>, <see cref="JsonValueType.Object"/>, <see cref="JsonValueType.Array"/>, and <see cref="JsonValueType.Csv"/>.
/// The connection is opened when MVC executes the result and disposed after streaming.
/// </remarks>
public sealed class DbComplexJsonResult : ActionResult
{
	private readonly string _query;
	private readonly (string Name, JsonValueType ResultType)[] _props;
	private readonly bool _isStoredProc;
	private readonly SqlParameter[] _parameters;

	/// <summary>Creates a streaming complex JSON result without SQL parameters.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	public DbComplexJsonResult(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false)
		: this(query, props, isStoredProc, Data.NoParams) { }

	/// <summary>Creates a streaming complex JSON result using <see cref="Db.ConnectionString"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	public DbComplexJsonResult(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);
		ArgumentNullException.ThrowIfNull(props);
		_query = query;
		_props = props;
		_isStoredProc = isStoredProc;
		_parameters = parameters ?? Data.NoParams;
	}

	/// <summary>Creates a streaming complex JSON query result with named parameter values.</summary>
	/// <param name="query">The SQL query.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbComplexJsonResult(string query, (string Name, JsonValueType ResultType)[] props, params (string name, object value)[] parameters)
		: this(query, props, false, Data.SqlParams(parameters)) { }

	/// <summary>Creates a streaming complex JSON query or stored procedure result with named parameter values.</summary>
	/// <param name="query">The SQL query or stored procedure name.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query names a stored procedure.</param>
	/// <param name="parameters">The named parameter values.</param>
	public DbComplexJsonResult(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
		: this(query, props, isStoredProc, Data.SqlParams(parameters)) { }

	/// <summary>Executes the query and streams its multiple result sets as JSON, honoring request cancellation.</summary>
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
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		await WriteResponseAsync(context, reader, _props);
	}

	internal static async Task WriteResponseAsync(ActionContext context, DbDataReader reader, (string Name, JsonValueType ResultType)[] props)
	{
		var response = context.HttpContext.Response;
		response.ContentType = "application/json; charset=utf-8";
		response.ContentLength = null;
		var cancellationToken = context.HttpContext.RequestAborted;
		cancellationToken.ThrowIfCancellationRequested();

		await using var writer = new Utf8JsonWriter(response.Body, Data.JsonWriterOptions);
		await reader.ReadJson(props, writer, cancellationToken);
		await writer.FlushAsync(cancellationToken);
	}
}

