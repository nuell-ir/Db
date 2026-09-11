using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and maps each row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of objects to create and populate from the result set.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the mapped objects, or null if no rows were returned.</returns>
	public static Task<List<T>> ObjList<T>(string query, params (string name, object value)[] parameters) where T : new()
		 => ObjList<T>(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and maps each row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of objects to create and populate from the result set.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the mapped objects, or null if no rows were returned.</returns>
	public static Task<List<T>> ObjList<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
		 => ObjList<T>(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and maps each row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of objects to create and populate from the result set.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the mapped objects, or null if no rows were returned.</returns>
	public static Task<List<T>> ObjList<T>(string query, bool isStoredProc = false) where T : new()
		 => ObjList<T>(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and maps each row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of objects to create and populate from the result set.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the mapped objects, or null if no rows were returned.</returns>
	public static async Task<List<T>> ObjList<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : new()
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		if (parameters is { Length: > 0 })
			cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false);
		if (!reader.HasRows)
			return null;

		var list = new List<T>();
		var mappings = reader.GetColumnMappings<T>();
		int mappingCount = mappings.Length;
		while (await reader.ReadAsync().ConfigureAwait(false))
		{
			var obj = new T();
			for (int i = 0; i < mappingCount; i++)
				mappings[i].Reader(obj, reader, mappings[i].Ordinal);
			list.Add(obj);
		}
		return list;
	}
}
