using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;

namespace nuel;

public static class CsvWriter
	{
		internal const char sep = '~';
		internal const char line = '|';

		internal static Type[] WriteCsvHeader(this StringBuilder str, DbDataReader reader)
		{
			int columns = reader.FieldCount;
			var fieldTypes = new Type[columns];
			Type type;
			for (int i = 0; i < columns; i++)
			{
				type = reader.GetFieldType(i);
				fieldTypes[i] = type;
				str.Append(GetCsvTypeFlag(type));
				str.Append(reader.GetName(i));
				if (i < columns - 1)
					str.Append(sep);
			}
			return fieldTypes;
		}

		internal static Type[] WriteCsvHeader(this StringBuilder str, PropertyInfo[] props)
		{
			var fieldTypes = new Type[props.Length];
			Type type;
			for (int i = 0; i < props.Length; i++)
			{
				type = props[i].PropertyType;
				fieldTypes[i] = type;
				str.Append(GetCsvTypeFlag(type));
				str.Append(props[i].Name);
				if (i < props.Length - 1)
					str.Append(sep);
			}
			return fieldTypes;
		}

		internal static char GetCsvTypeFlag(Type colType)
		{
			colType = Nullable.GetUnderlyingType(colType) ?? colType;

			var typeCode = Type.GetTypeCode(colType);
			return typeCode switch
			{
				TypeCode.Byte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => '!',
				TypeCode.Decimal or TypeCode.Double or TypeCode.Single => '%',
				TypeCode.DateTime => '#',
				TypeCode.Boolean => '^',
				TypeCode.Char or TypeCode.String => '$',
				_ when colType == typeof(DateTimeOffset) => '#',
				_ when colType == typeof(Guid) || colType == typeof(TimeSpan) || colType == typeof(byte[]) => '$',
				_ => throw new NotSupportedException($"Type '{colType.FullName}' is not supported.")
			};
		}

		internal static void WriteCsvRow(this StringBuilder str, DbDataReader reader, Type[] fieldTypes)
		{
			str.Append(line);
			for (int i = 0; i < fieldTypes.Length; i++)
			{
				if (reader.IsDBNull(i))
					str.Append('Ø');
				else
				{
					var type = fieldTypes[i];
					switch (Type.GetTypeCode(type))
					{
						case TypeCode.Int32:
							str.Append(reader.GetInt32(i));
							break;
						case TypeCode.Int64:
							str.Append(reader.GetInt64(i));
							break;
						case TypeCode.Int16:
							str.Append(reader.GetInt16(i));
							break;
						case TypeCode.Byte:
							str.Append(reader.GetByte(i));
							break;
						case TypeCode.Single:
							str.Append(reader.GetFloat(i).ToString(CultureInfo.InvariantCulture));
							break;
						case TypeCode.Double:
							str.Append(reader.GetDouble(i).ToString(CultureInfo.InvariantCulture));
							break;
						case TypeCode.Decimal:
							str.Append(reader.GetDecimal(i).ToString(CultureInfo.InvariantCulture));
							break;
						case TypeCode.DateTime:
							str.Append(new DateTimeOffset(reader.GetDateTime(i)).ToUnixTimeSeconds());
							break;
						case TypeCode.Boolean:
							str.Append(reader.GetBoolean(i) ? 1 : 0);
							break;
						case TypeCode.Char:
						case TypeCode.String:
							str.Append(reader.GetString(i));
							break;
						default:
							if (type == typeof(Guid))
								str.Append(reader.GetGuid(i));
							else if (type == typeof(DateTimeOffset))
								str.Append((reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(i) : reader.GetFieldValue<DateTimeOffset>(i)).ToUnixTimeSeconds());
							else if (type == typeof(TimeSpan))
								str.Append(reader is SqlDataReader sdr ? sdr.GetTimeSpan(i) : reader.GetFieldValue<TimeSpan>(i));
							else if (type == typeof(byte[]))
								str.Append(Convert.ToBase64String((byte[])reader.GetValue(i)));
							else
								throw new NotSupportedException($"Type '{type.FullName}' is not supported.");
							break;
					}
				}
				if (i < fieldTypes.Length - 1)
					str.Append(sep);
			}
		}
	}

public static partial class Db
{
	/// <summary>Converts an array of objects to a CSV string.</summary>
	/// <param name="objects">The array of objects to convert.</param>
	/// <returns>A CSV formatted string representing the objects, or null if the array is null or empty.</returns>
	public static string Csv(object[] objects)
	{
		if (objects is null || objects.Length == 0)
			return null;

		var props = objects[0].GetType().GetProperties();
		var propGetters = props.Select(p => (Func<object, object>)(o => p.GetValue(o))).ToArray();
		var str = new StringBuilder();
		var fieldTypes = str.WriteCsvHeader(props);
		int objectCount = objects.Length;
		int propCount = props.Length;

		object val;
		for (int i = 0; i < objectCount; i++)
		{
			str.Append(CsvWriter.line);
			for (int p = 0; p < props.Length; p++)
			{
				val = propGetters[p](objects[i]);
				if (val is null)
					str.Append('Ø');
				else
				{
					var type = fieldTypes[p];
					var underlying = Nullable.GetUnderlyingType(type) ?? type;

					if (val is DateTime dt)
						str.Append(new DateTimeOffset(dt).ToUnixTimeSeconds());
					else if (val is DateTimeOffset dto)
						str.Append(dto.ToUnixTimeSeconds());
					else if (val is bool b)
						str.Append(b ? '1' : '0');
					else if (val is byte[] bytes)
						str.Append(Convert.ToBase64String(bytes));
					else if (val is float f)
						str.Append(f.ToString(CultureInfo.InvariantCulture));
					else if (val is double d)
						str.Append(d.ToString(CultureInfo.InvariantCulture));
					else if (val is decimal dec)
						str.Append(dec.ToString(CultureInfo.InvariantCulture));
					else if (underlying == typeof(Guid) || underlying == typeof(TimeSpan) || Type.GetTypeCode(underlying) != TypeCode.Object)
						str.Append(val);
					else
						throw new NotSupportedException($"Type '{type.FullName}' is not supported.");
				}
				if (p < propCount - 1)
					str.Append(CsvWriter.sep);
			}
		}

		return str.ToString();
	}
		/// <summary>Asynchronously converts the query result to a CSV string.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <returns>A task representing the asynchronous operation, returning a CSV formatted string, or null if no rows were returned.</returns>
		public static Task<string> Csv(string query, params (string name, object value)[] parameters)
			 => Csv(query, false, Data.SqlParams(parameters));

		/// <summary>Asynchronously converts the query result to a CSV string.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <returns>A task representing the asynchronous operation, returning a CSV formatted string, or null if no rows were returned.</returns>
		public static Task<string> Csv(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => Csv(query, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Asynchronously converts the query result to a CSV string.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <returns>A task representing the asynchronous operation, returning a CSV formatted string, or null if no rows were returned.</returns>
		public static Task<string> Csv(string query, bool isStoredProc = false)
			 => Csv(query, isStoredProc, Data.NoParams);

		/// <summary>Asynchronously converts the query result to a CSV string.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The SQL parameters to apply to the command.</param>
		/// <returns>A task representing the asynchronous operation, returning a CSV formatted string, or null if no rows were returned.</returns>
		public static async Task<string> Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			return await reader.ReadCsv();
		}

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

		/// <summary>Asynchronously converts multiple results of a query to an array of CSV strings.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The SQL parameters to apply to the command.</param>
		/// <returns>A task representing the asynchronous operation, returning an array of CSV formatted strings.</returns>
		public static async Task<string[]> MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			var results = new List<string>
			{
				await reader.ReadCsv()
			};
			while (await reader.NextResultAsync())
				results.Add(await reader.ReadCsv());
			return [.. results];
		}

		internal static async Task<string> ReadCsv(this SqlDataReader reader)
		{
			if (!reader.HasRows)
				return null;
			var str = new StringBuilder();
			await reader.ReadAsync();
			var fieldTypes = str.WriteCsvHeader(reader);
			str.WriteCsvRow(reader, fieldTypes);
			while (await reader.ReadAsync())
				str.WriteCsvRow(reader, fieldTypes);
			return str.ToString();
		}
	}
