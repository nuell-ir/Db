using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuel
{
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

		/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string or writes UTF-8 JSON directly to a stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="stream">The optional stream to write UTF-8 JSON directly to. If null, a JSON string is returned.</param>
		/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results, or null if a stream is provided.</returns>
		public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false, Stream stream = null)
			 => ComplexJson(query, stream, props, isStoredProc, Data.NoParams);

		/// <summary>Asynchronously executes a query with multiple result sets and writes the results as UTF-8 JSON directly to the specified stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
		/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
		public static Task<string> ComplexJson(string query, Stream stream, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false)
			 => ComplexJson(query, stream, props, isStoredProc, Data.NoParams);

		/// <summary>Asynchronously executes a query with multiple result sets and writes the results as UTF-8 JSON directly to the specified stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
		/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
		public static Task<string> ComplexJson(string query, Stream stream, (string Name, JsonValueType ResultType)[] props, params (string name, object value)[] parameters)
			 => ComplexJson(query, stream, props, false, Data.SqlParams(parameters));

		/// <summary>Asynchronously executes a query with multiple result sets and writes the results as UTF-8 JSON directly to the specified stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 JSON directly to.</param>
		/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 JSON directly to the stream.</returns>
		public static Task<string> ComplexJson(string query, Stream stream, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params (string name, object value)[] parameters)
			 => ComplexJson(query, stream, props, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The SQL parameters to apply to the command.</param>
		/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results.</returns>
		public static Task<string> ComplexJson(string query, (string Name, JsonValueType ResultType)[] props, bool isStoredProc, params SqlParameter[] parameters)
			=> ComplexJson(query, null, props, isStoredProc, parameters);


		/// <summary>Asynchronously executes a query with multiple result sets and formats the results into a single JSON string or writes UTF-8 JSON directly to a stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 JSON directly to, or null to return a JSON string.</param>
		/// <param name="props">An array of tuples defining property names and their corresponding result types.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The SQL parameters to apply to the command.</param>
		/// <returns>A task representing the asynchronous operation, returning a JSON string of the combined results, or null if a stream is provided.</returns>
		public static async Task<string> ComplexJson(string query, Stream stream, (string Name, JsonValueType ResultType)[] props, bool isStoredProc = false, params SqlParameter[] parameters)
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			return await reader.ReadComplexJson(props, stream);
		}

		internal static Task<string> ReadComplexJson(this SqlDataReader reader, (string Name, JsonValueType ResultType)[] props, Stream stream = null)
			=> ReadComplexJson((DbDataReader)reader, props, stream);

		internal static async Task<string> ReadComplexJson(this DbDataReader reader, (string Name, JsonValueType ResultType)[] props, Stream stream = null)
		{
			if (stream != null)
			{
				using var writer = new Utf8JsonWriter(stream, Data.JsonWriterOptions);
				await reader.ReadComplexJson(props, writer);
				await writer.FlushAsync();
				return null;
			}
			else
			{
				using var memoryStream = new MemoryStream();
				using var writer = new Utf8JsonWriter(memoryStream, Data.JsonWriterOptions);
				await reader.ReadComplexJson(props, writer);
				await writer.FlushAsync();
				return Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
			}
		}

		internal static async Task ReadComplexJson(this DbDataReader reader, (string Name, JsonValueType ResultType)[] props, Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			for (int i = 0; i < props.Length; i++)
			{
				await ReadResultAsync(i);
				await reader.NextResultAsync();
			}
			writer.WriteEndObject();
			await writer.FlushAsync();

			async Task ReadResultAsync(int i)
			{
				writer.WritePropertyName(props[i].Name);
				if (!reader.HasRows)
				{
					writer.WriteNullValue();
					return;
				}
				switch (props[i].ResultType)
				{
					case JsonValueType.Value:
						if (await reader.ReadAsync())
						{
							if (reader.IsDBNull(0))
								writer.WriteNullValue();
							else
								writer.WriteDbValue(reader, reader.GetFieldType(0), 0);
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
		}
	}
}