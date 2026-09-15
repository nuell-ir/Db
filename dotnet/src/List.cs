using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{T}"/>.</summary>
	/// <typeparam name="T">The value type of the items in the list.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the values of the first column, or null if no rows were returned.</returns>
	public static Task<List<T>> List<T>(string query, params (string name, object value)[] parameters) where T : struct
		=> List<T>(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{T}"/>.</summary>
	/// <typeparam name="T">The value type of the items in the list.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the values of the first column, or null if no rows were returned.</returns>
	public static Task<List<T>> List<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
		=> List<T>(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{T}"/>.</summary>
	/// <typeparam name="T">The value type of the items in the list.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the values of the first column, or null if no rows were returned.</returns>
	public static Task<List<T>> List<T>(string query, bool isStoredProc = false) where T : struct
		=> List<T>(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and returns the values of the first column as a <see cref="System.Collections.Generic.List{T}"/>.</summary>
	/// <typeparam name="T">The value type of the items in the list.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="System.Collections.Generic.List{T}"/> containing the values of the first column, or null if no rows were returned.</returns>
	public static async Task<List<T>> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
	{
		await using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		if (parameters is { Length: > 0 })
			cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess).ConfigureAwait(false);
		if (!reader.HasRows || reader.FieldCount == 0)
			return null;

		var list = new List<T>();
		bool sameType = reader.GetFieldType(0) == typeof(T);
		while (await reader.ReadAsync().ConfigureAwait(false))
		{
			if (reader.IsDBNull(0))
			{
				list.Add(default);
			}
			else if (sameType)
			{
				list.Add(reader.GetFieldValue<T>(0));
			}
			else
			{
				var val = reader.GetValue(0);
				list.Add(val is T t ? t : (T)Convert.ChangeType(val, typeof(T)));
			}
		}
		return list;
	}
}
