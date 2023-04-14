using System.Data;
using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;

namespace nuell
{
	public static class CsvWriter
	{
		internal const char sep = '~';
		internal const char line = '|';

		internal static TypeCode[] WriteCsvHeader(this StringBuilder str, SqlDataReader reader)
		{
			int columns = reader.FieldCount;
			var fieldTypes = new TypeCode[columns];
			TypeCode type;
			for (int i = 0; i < columns; i++)
			{
				type = Type.GetTypeCode(reader.GetFieldType(i));
				fieldTypes[i] = type;
				str.Append(GetCsvTypeFlag(type));
				str.Append(reader.GetName(i));
				str.Append(sep);
			}
			str.Remove(str.Length - 1, 1);
			str.Append(line);
			return fieldTypes;
		}

		internal static TypeCode[] WriteCsvHeader(this StringBuilder str, PropertyInfo[] props)
		{
			var fieldTypes = new TypeCode[props.Length];
			TypeCode type;
			for (int i = 0; i < props.Length; i++)
			{
				type = Type.GetTypeCode(props[i].PropertyType);
				fieldTypes[i] = type;
				str.Append(GetCsvTypeFlag(type));
				str.Append(props[i].Name);
				str.Append(sep);
			}
			str.Remove(str.Length - 1, 1);
			str.Append(line);
			return fieldTypes;
		}

		private static char GetCsvTypeFlag(TypeCode colType)
		{			
			switch (colType)
			{
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return '!';

				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return '%';

				case TypeCode.DateTime:
					return '#';

				case TypeCode.Boolean:
					return '^';

				default:
					return '$';
			}
		}

		internal static void WriteCsvRow(this StringBuilder str, SqlDataReader reader, TypeCode[] fieldTypes)
		{
			for (int i = 0; i < fieldTypes.Length; i++)
			{
				if (reader.IsDBNull(i))
					str.Append('Ø');
				else
					switch (fieldTypes[i])
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
							str.Append(reader.GetFloat(i));
							break;
						case TypeCode.Double:
							str.Append(reader.GetDouble(i));
							break;
						case TypeCode.Decimal:
							str.Append(reader.GetDecimal(i));
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
					}
				str.Append(sep);
			}
			str.Remove(str.Length - 1, 1);
			str.Append(line);
		}
	}
}

namespace nuell.Sync
{
	public static partial class Db
	{
		/// <summary>Converts the query result to CSV string.</summary>
		public static string Csv(string query, params (string name, object value)[] parameters)
		=> Csv(query, false, Data.SqlParams(parameters));

		/// <summary>Converts the query result to CSV string.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static string Csv(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => Csv(query, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the query result to CSV string.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static string Csv(string query, bool isStoredProc = false)
			 => Csv(query, isStoredProc, Data.NoParams);


		/// <summary>Converts the query result to CSV string.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static string Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(query, cnnct);
			if (isStoredProc)
				cmnd.CommandType = CommandType.StoredProcedure;
			cmnd.Parameters.AddRange(parameters);
			cnnct.Open();
			using var reader = cmnd.ExecuteReader();
			return reader.ReadCsv();
		}

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		public static string[] MultiCsv(string query, params (string name, object value)[] parameters)
			 => MultiCsv(query, false, Data.SqlParams(parameters));

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static string[] MultiCsv(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => MultiCsv(query, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static string[] MultiCsv(string query, bool isStoredProc = false)
			 => MultiCsv(query, isStoredProc, Data.NoParams);

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static string[] MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(query, cnnct);
			if (isStoredProc)
				cmnd.CommandType = CommandType.StoredProcedure;
			cmnd.Parameters.AddRange(parameters);
			cnnct.Open();
			using var reader = cmnd.ExecuteReader();
			var results = new List<string>
			{
				reader.ReadCsv()
			};
			while (reader.NextResult())
				results.Add(reader.ReadCsv());
			return results.ToArray();
		}

		private static string ReadCsv(this SqlDataReader reader)
		{
			if (!reader.HasRows)
				return null;
			var str = new StringBuilder();
			reader.Read();
			var fieldTypes = str.WriteCsvHeader(reader);
			str.WriteCsvRow(reader, fieldTypes);
			while (reader.Read())
				str.WriteCsvRow(reader, fieldTypes);
			str.Remove(str.Length - 1, 1);
			return str.ToString();
		}

		/// <summary>Converts the object array to CSV string.</summary>
		public static string Csv(object[] objects)
		{
			if (objects is null || objects.Length == 0)
				return null;

			var props = objects[0].GetType().GetProperties();
			var str = new StringBuilder();
			var typeCodes = str.WriteCsvHeader(props);

			object val;
			for (int i = 0; i < objects.Length; i++)
			{
				for (int p = 0; p < props.Length; p++)
				{
					val = props[p].GetValue(objects[i]);
					if (val is null)
						str.Append('Ø');
					else
						switch (typeCodes[p])
						{
							case TypeCode.DateTime:
								str.Append(new DateTimeOffset((DateTime)val).ToUnixTimeSeconds());
								break;
							case TypeCode.Boolean:
								str.Append((bool)val ? 1 : 0);
								break;
							default:
								str.Append(val);
								break;
						}
					str.Append(CsvWriter.sep);
				}
				str.Remove(str.Length - 1, 1);
				str.Append(CsvWriter.line);
			}
			str.Remove(str.Length - 1, 1);

			return str.ToString();
		}
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		/// <summary>Converts the query result to CSV string.</summary>
		public static Task<string> Csv(string query, params (string name, object value)[] parameters)
			 => Csv(query, false, Data.SqlParams(parameters));

		/// <summary>Converts the query result to CSV string.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static Task<string> Csv(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => Csv(query, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the query result to CSV string.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static Task<string> Csv(string query, bool isStoredProc = false)
			 => Csv(query, isStoredProc, Data.NoParams);

		/// <summary>Converts the query result to CSV string.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static async Task<string> Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(query, cnnct);
			if (isStoredProc)
				cmnd.CommandType = CommandType.StoredProcedure;
			cmnd.Parameters.AddRange(parameters);
			await cnnct.OpenAsync();
			using var reader = await cmnd.ExecuteReaderAsync();
			return await reader.ReadCsv();
		}

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static Task<string[]> MultiCsv(string query, params (string name, object value)[] parameters)
		=> MultiCsv(query, false, Data.SqlParams(parameters));

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static Task<string[]> MultiCsv(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => MultiCsv(query, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static Task<string[]> MultiCsv(string query, bool isStoredProc = false)
			 => MultiCsv(query, isStoredProc, Data.NoParams);

		/// <summary>Converts the multiple results of a query to an array of CSV strings.</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>
		public static async Task<string[]> MultiCsv(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var cmnd = new SqlCommand(query, cnnct);
			if (isStoredProc)
				cmnd.CommandType = CommandType.StoredProcedure;
			cmnd.Parameters.AddRange(parameters);
			await cnnct.OpenAsync();
			using var reader = await cmnd.ExecuteReaderAsync();
			var results = new List<string>
			{
				await reader.ReadCsv()
			};
			while (await reader.NextResultAsync())
				results.Add(await reader.ReadCsv());
			return results.ToArray();
		}

		private static async Task<string> ReadCsv(this SqlDataReader reader)
		{
			if (!reader.HasRows)
				return null;
			var str = new StringBuilder();
			await reader.ReadAsync();
			var fieldTypes = str.WriteCsvHeader(reader);
			str.WriteCsvRow(reader, fieldTypes);
			while (await reader.ReadAsync())
				str.WriteCsvRow(reader, fieldTypes);
			str.Remove(str.Length - 1, 1);
			return str.ToString();
		}
	}
}