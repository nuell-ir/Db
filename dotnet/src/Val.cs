using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and returns the value of the first column of the first row converted to the value type <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The value type of the returned scalar value.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning the scalar value of type <typeparamref name="T"/>, or default(<typeparamref name="T"/>) if null or DBNull.</returns>
	public static Task<T> Val<T>(string query, params (string name, object value)[] parameters) where T : struct
		 => Val<T>(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns the value of the first column of the first row converted to the value type <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The value type of the returned scalar value.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning the scalar value of type <typeparamref name="T"/>, or default(<typeparamref name="T"/>) if null or DBNull.</returns>
	public static Task<T> Val<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
		 => Val<T>(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns the value of the first column of the first row converted to the value type <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The value type of the returned scalar value.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning the scalar value of type <typeparamref name="T"/>, or default(<typeparamref name="T"/>) if null or DBNull.</returns>
	public static Task<T> Val<T>(string query, bool isStoredProc = false) where T : struct
		 => Val<T>(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and returns the value of the first column of the first row converted to the value type <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The value type of the returned scalar value.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning the scalar value of type <typeparamref name="T"/>, or default(<typeparamref name="T"/>) if null or DBNull.</returns>
	public async static Task<T> Val<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		if (parameters is { Length: > 0 })
			cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess).ConfigureAwait(false);
		if (await reader.ReadAsync().ConfigureAwait(false) && reader.FieldCount > 0 && !reader.IsDBNull(0))
		{
			if (reader.GetFieldType(0) == typeof(T))
				return reader.GetFieldValue<T>(0);

			var val = reader.GetValue(0);
			return val is T t ? t : (T)Convert.ChangeType(val, typeof(T));
		}
		return default;
	}
}
