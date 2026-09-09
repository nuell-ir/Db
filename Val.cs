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
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		var val = await cmd.ExecuteScalarAsync();
		return val is null || val is DBNull ? default : val is T t ? t : (T)Convert.ChangeType(val, typeof(T));
	}
}