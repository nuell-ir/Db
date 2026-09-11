using System.Data;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static Task<object[]> Values(string query, params (string name, object value)[] parameters)
		 => Values(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static Task<object[]> Values(string query, bool isStoredProc, params (string name, object value)[] parameters)
		 => Values(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static Task<object[]> Values(string query, bool isStoredProc = false)
		 => Values(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and returns all values from all rows and columns across all result sets as an array of objects.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of objects containing all values returned by the query.</returns>
	public static async Task<object[]> Values(string query, bool isStoredProc, params SqlParameter[] parameters)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		if (parameters is { Length: > 0 })
			cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		var results = new List<object>();
		await AddValues().ConfigureAwait(false);
		while (await reader.NextResultAsync().ConfigureAwait(false))
			await AddValues().ConfigureAwait(false);
		return results.Count == 0 ? [] : results.ToArray();

		async Task AddValues()
		{
			int fieldCount = reader.FieldCount;
			if (fieldCount == 0)
				return;

			if (fieldCount == 1)
			{
				while (await reader.ReadAsync().ConfigureAwait(false))
					results.Add(reader.GetValue(0));
			}
			else
			{
				var values = new object[fieldCount];
				while (await reader.ReadAsync().ConfigureAwait(false))
				{
					reader.GetValues(values);
					for (int i = 0; i < fieldCount; i++)
						results.Add(values[i]);
				}
			}
		}
	}
}
