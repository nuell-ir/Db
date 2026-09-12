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
	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, params (string name, object value)[] parameters)
		 => ComplexJson(query, props, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
		 => ComplexJson(query, props, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false)
		 => ComplexJson(query, props, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
	public static async Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		return await reader.ReadComplexJson(props);
	}

	internal static Task<string> ReadComplexJson(this SqlDataReader reader, (string Name, JsonValueType ResultType)[] props)
		=> ReadComplexJson((DbDataReader)reader, props);

	internal static async Task<string> ReadComplexJson(this DbDataReader reader, (string Name, JsonValueType ResultType)[] props)
	{
		var bufferWriter = new ArrayBufferWriter<byte>(2048);
		using var writer = new Utf8JsonWriter(bufferWriter, Data.JsonWriterOptions);
		await reader.ReadComplexJson(props, writer);
		writer.Flush();
		return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
	}

	internal static async Task ReadComplexJson(this DbDataReader reader, (string Name, JsonValueType ResultType)[] props, Utf8JsonWriter writer, CancellationToken cancellationToken = default)
	{
		var encodedNames = new JsonEncodedText[props.Length];
		for (int p = 0; p < props.Length; p++)
			encodedNames[p] = JsonEncodedText.Encode(props[p].Name);

		writer.WriteStartObject();
		for (int i = 0; i < props.Length; i++)
		{
			writer.WritePropertyName(encodedNames[i]);
			if (!reader.HasRows)
			{
				writer.WriteNullValue();
			}
			else
			{
				switch (props[i].ResultType)
				{
					case JsonValueType.Value:
						if (await reader.ReadAsync(cancellationToken))
						{
							if (reader.IsDBNull(0))
								writer.WriteNullValue();
							else
								writer.WriteDbValue(reader, Data.GetJsonColType(reader.GetFieldType(0)), 0);
						}
						else
							writer.WriteNullValue();
						break;
					case JsonValueType.Array:
					case JsonValueType.Object:
						await reader.ReadJson(props[i].ResultType, writer);
						break;
					case JsonValueType.Csv:
						writer.WriteStringValue(await reader.ReadCsv());
						break;
				}
			}
			await reader.NextResultAsync(cancellationToken);
		}
		writer.WriteEndObject();
		await writer.FlushAsync(cancellationToken);
	}
}
