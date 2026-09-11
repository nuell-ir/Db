using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static Task<string[]> MultiCsv(string query, params (string name, object value)[] parameters)
		=> MultiCsv(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static Task<string[]> MultiCsv(string query, bool isStoredProc, params (string name, object value)[] parameters)
		=> MultiCsv(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static Task<string[]> MultiCsv(string query, bool isStoredProc = false)
		=> MultiCsv(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously converts multiple results of a query to UTF-8 CSV directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 CSV directly to.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 CSV directly to the stream.</returns>
	public static Task<string[]> MultiCsv(string query, Stream stream, bool isStoredProc = false)
		=> MultiCsv(query, stream, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously converts multiple results of a query to UTF-8 CSV directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 CSV directly to.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 CSV directly to the stream.</returns>
	public static Task<string[]> MultiCsv(string query, Stream stream, params (string name, object value)[] parameters)
		=> MultiCsv(query, stream, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts multiple results of a query to UTF-8 CSV directly written to the specified stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 CSV directly to.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 CSV directly to the stream.</returns>
	public static Task<string[]> MultiCsv(string query, Stream stream, bool isStoredProc, params (string name, object value)[] parameters)
		=> MultiCsv(query, stream, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
	public static Task<string[]> MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
		=> MultiCsv(query, null, isStoredProc, parameters);

	/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings or writes UTF-8 CSV directly to a stream.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="stream">The stream to write UTF-8 CSV directly to, or null to return an array of CSV strings.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings, or null if a stream is provided.</returns>
	public static async Task<string[]> MultiCsv(string query, Stream stream, bool isStoredProc, params SqlParameter[] parameters)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		return await reader.ReadMultiCsv(stream);
	}

	internal static Task<string[]> ReadMultiCsv(this SqlDataReader reader, Stream stream = null)
		=> ReadMultiCsv((DbDataReader)reader, stream);

	internal static async Task<string[]> ReadMultiCsv(this DbDataReader reader, Stream stream = null)
	{
		if (stream != null)
		{
			await using var writer = new Utf8CsvStreamWriter(stream);
			bool hasWrittenAny = false;
			do
			{
				if (await reader.ReadAsync())
				{
					if (hasWrittenAny)
					{
						if (writer.FreeCapacity < 1)
							await writer.FlushAsync();
						writer.WriteByte((byte)'\n');
					}
					var colTypes = await writer.WriteCsvHeaderAsync(reader);
					await writer.WriteCsvRowAsync(reader, colTypes);
					while (await reader.ReadAsync())
					{
						if (writer.FreeCapacity < 256)
							await writer.FlushAsync();
						await writer.WriteCsvRowAsync(reader, colTypes);
					}
					hasWrittenAny = true;
				}
			} while (await reader.NextResultAsync());

			if (hasWrittenAny)
				await writer.FinishAsync();

			return null;
		}
		else
		{
			var results = new List<string>(4)
			{
				await reader.ReadCsv()
			};
			while (await reader.NextResultAsync())
				results.Add(await reader.ReadCsv());
			return [.. results];
		}
	}
}

