using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuel;

/// <summary>Provides asynchronous database access methods for SQL Server operations.</summary>
public static partial class Db
{
	/// <summary>Asynchronously converts the first row of the query result to a JSON object string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the first row.</returns>
	public static Task<string> Json(string query, params (string name, object value)[] parameters)
		 => Json(query, JsonValueType.Object, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to a JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results.</returns>
	public static Task<string> Json(string query, JsonValueType result, params (string name, object value)[] parameters)
	=> Json(query, result, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the first row of the query result to a JSON object string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the first row.</returns>
	public static Task<string> Json(string query, bool isStoredProc, params (string name, object value)[] parameters)
	=> Json(query, JsonValueType.Object, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to a JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results.</returns>
	public static Task<string> Json(string query, JsonValueType result, bool isStoredProc, params (string name, object value)[] parameters)
	=> Json(query, result, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts the query results to a JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results.</returns>
	public static Task<string> Json(string query, JsonValueType result = JsonValueType.Object, bool isStoredProc = false)
		 => Json(query, result, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously converts the first row of the query result to a JSON object string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the first row.</returns>
	public static Task<string> Json(string query, bool isStoredProc)
		=> Json(query, JsonValueType.Object, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously converts the first row of the query result to a JSON object string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the first row.</returns>
	public static Task<string> Json(string query, bool isStoredProc, params SqlParameter[] parameters)
		=> Json(query, JsonValueType.Object, isStoredProc, parameters);

	/// <summary>Asynchronously converts the query results to a JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">The JSON structure to return (<see cref="JsonValueType.Object"/> for the first row, or <see cref="JsonValueType.Array"/> for all rows).</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string representing the query results.</returns>
	public static async Task<string> Json(string query, JsonValueType result, bool isStoredProc, params SqlParameter[] parameters)
	{
		await using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
		var bufferWriter = new ArrayBufferWriter<byte>(1024);
		using var writer = new Utf8JsonWriter(bufferWriter, Data.JsonWriterOptions);
		await reader.ReadJson(result, writer);
		writer.Flush();
		return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
	}

	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static Task<string> Json(string query, (string Name, JsonValueType ResultType)[] result, params (string name, object value)[] parameters)
		 => Json(query, result, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static Task<string> Json(string query, (string Name, JsonValueType ResultType)[] result, bool isStoredProc, params (string name, object value)[] parameters)
		 => Json(query, result, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static Task<string> Json(string query, (string Name, JsonValueType ResultType)[] result, bool isStoredProc = false)
		 => Json(query, result, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="result">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static async Task<string> Json(string query, (string Name, JsonValueType ResultType)[] result, bool isStoredProc, params SqlParameter[] parameters)
	{
		await using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		return await reader.ReadJson(result);
	}

	internal static async Task<string> ReadJson(this DbDataReader reader, (string Name, JsonValueType ResultType)[] result)
	{
		var bufferWriter = new ArrayBufferWriter<byte>(2048);
		using var writer = new Utf8JsonWriter(bufferWriter, Data.JsonWriterOptions);
		await reader.ReadJson(result, writer);
		writer.Flush();
		return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
	}
}
