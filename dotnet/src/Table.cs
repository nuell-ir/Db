using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public static Task<DataTable> Table(string query, params (string name, object value)[] parameters)
		=> Table(query, false, parameters);

	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public async static Task<DataTable> Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(cmd, parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false);
		return await ReadTableAsync(reader).ConfigureAwait(false);
	}

	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public static Task<DataTable> Table(string query, bool isStoredProc = false)
		=> Table(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public static Task<DataTable> Table(string query, params SqlParameter[] parameters)
		=> Table(query, false, parameters);

	/// <summary>Asynchronously executes the query and returns the results in a <see cref="DataTable"/>.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a <see cref="DataTable"/> containing the query results, or null if no rows were returned.</returns>
	public async static Task<DataTable> Table(string query, bool isStoredProc, params SqlParameter[] parameters)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(cmd, parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false);
		return await ReadTableAsync(reader).ConfigureAwait(false);
	}

	internal static async Task<DataTable> ReadTableAsync(DbDataReader reader)
	{
		if (!reader.HasRows || reader.FieldCount == 0)
			return null;

		var dt = new DataTable();
		int columns = reader.FieldCount;
		for (int i = 0; i < columns; i++)
		{
			string name = reader.GetName(i);
			Type type = reader.GetFieldType(i);

			if (string.IsNullOrEmpty(name))
			{
				name = $"Column{i + 1}";
			}
			else if (dt.Columns.Contains(name))
			{
				int suffix = 1;
				string uniqueName = $"{name}_{suffix}";
				while (dt.Columns.Contains(uniqueName))
					uniqueName = $"{name}_{++suffix}";
				name = uniqueName;
			}

			dt.Columns.Add(name, type);
		}

		dt.BeginLoadData();
		try
		{
			var values = new object[columns];
			while (await reader.ReadAsync().ConfigureAwait(false))
			{
				reader.GetValues(values);
				dt.Rows.Add(values);
			}
		}
		finally
		{
			dt.EndLoadData();
		}

		return dt;
	}
}
