using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class CsvWriter
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

	internal enum CsvColType : byte
	{
		Int32,
		Int64,
		Int16,
		Byte,
		Single,
		Double,
		Decimal,
		DateTime,
		Boolean,
		String,
		Guid,
		DateTimeOffset,
		TimeSpan,
		ByteArray
	}

	internal static (char Flag, CsvColType ColType) GetCsvTypeInfo(Type colType)
	{
		colType = Nullable.GetUnderlyingType(colType) ?? colType;

		var typeCode = Type.GetTypeCode(colType);
		return typeCode switch
		{
			TypeCode.Int32 => ('!', CsvColType.Int32),
			TypeCode.Int64 => ('!', CsvColType.Int64),
			TypeCode.Int16 => ('!', CsvColType.Int16),
			TypeCode.Byte => ('!', CsvColType.Byte),
			TypeCode.Single => ('%', CsvColType.Single),
			TypeCode.Double => ('%', CsvColType.Double),
			TypeCode.Decimal => ('%', CsvColType.Decimal),
			TypeCode.DateTime => ('#', CsvColType.DateTime),
			TypeCode.Boolean => ('^', CsvColType.Boolean),
			TypeCode.Char or TypeCode.String => ('$', CsvColType.String),
			_ when colType == typeof(DateTimeOffset) => ('#', CsvColType.DateTimeOffset),
			_ when colType == typeof(Guid) => ('$', CsvColType.Guid),
			_ when colType == typeof(TimeSpan) => ('$', CsvColType.TimeSpan),
			_ when colType == typeof(byte[]) => ('$', CsvColType.ByteArray),
			_ => throw new NotSupportedException($"Type '{colType.FullName}' is not supported.")
		};
	}

	internal static char GetCsvTypeFlag(Type colType)
		=> GetCsvTypeInfo(colType).Flag;

	internal static (Type[] FieldTypes, CsvColType[] ColTypes) WriteCsvHeaderTypes(this StringBuilder str, DbDataReader reader)
	{
		int columns = reader.FieldCount;
		var fieldTypes = new Type[columns];
		var colTypes = new CsvColType[columns];
		for (int i = 0; i < columns; i++)
		{
			var type = reader.GetFieldType(i);
			fieldTypes[i] = type;
			var (flag, colType) = GetCsvTypeInfo(type);
			colTypes[i] = colType;
			str.Append(flag);
			str.Append(reader.GetName(i));
			if (i < columns - 1)
				str.Append(sep);
		}
		return (fieldTypes, colTypes);
	}

	internal static void WriteCsvRow(this StringBuilder str, DbDataReader reader, CsvColType[] colTypes)
	{
		Span<char> span = stackalloc char[64];
		str.Append(line);
		for (int i = 0; i < colTypes.Length; i++)
		{
			if (reader.IsDBNull(i))
				str.Append('Ø');
			else
			{
				switch (colTypes[i])
				{
					case CsvColType.Int32:
						reader.GetInt32(i).TryFormat(span, out int wInt, default, CultureInfo.InvariantCulture);
						str.Append(span[..wInt]);
						break;
					case CsvColType.Int64:
						reader.GetInt64(i).TryFormat(span, out int wLong, default, CultureInfo.InvariantCulture);
						str.Append(span[..wLong]);
						break;
					case CsvColType.Int16:
						reader.GetInt16(i).TryFormat(span, out int wShort, default, CultureInfo.InvariantCulture);
						str.Append(span[..wShort]);
						break;
					case CsvColType.Byte:
						reader.GetByte(i).TryFormat(span, out int wByte, default, CultureInfo.InvariantCulture);
						str.Append(span[..wByte]);
						break;
					case CsvColType.Single:
						reader.GetFloat(i).TryFormat(span, out int wFloat, default, CultureInfo.InvariantCulture);
						str.Append(span[..wFloat]);
						break;
					case CsvColType.Double:
						reader.GetDouble(i).TryFormat(span, out int wDouble, default, CultureInfo.InvariantCulture);
						str.Append(span[..wDouble]);
						break;
					case CsvColType.Decimal:
						reader.GetDecimal(i).TryFormat(span, out int wDec, default, CultureInfo.InvariantCulture);
						str.Append(span[..wDec]);
						break;
					case CsvColType.DateTime:
						long dtSec = new DateTimeOffset(reader.GetDateTime(i)).ToUnixTimeSeconds();
						dtSec.TryFormat(span, out int wDt, default, CultureInfo.InvariantCulture);
						str.Append(span[..wDt]);
						break;
					case CsvColType.Boolean:
						str.Append(reader.GetBoolean(i) ? '1' : '0');
						break;
					case CsvColType.String:
						str.Append(reader.GetString(i));
						break;
					case CsvColType.Guid:
						reader.GetGuid(i).TryFormat(span, out int wGuid, "D");
						str.Append(span[..wGuid]);
						break;
					case CsvColType.DateTimeOffset:
						long dtoSec = (reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(i) : reader.GetFieldValue<DateTimeOffset>(i)).ToUnixTimeSeconds();
						dtoSec.TryFormat(span, out int wDto, default, CultureInfo.InvariantCulture);
						str.Append(span[..wDto]);
						break;
					case CsvColType.TimeSpan:
						TimeSpan ts = reader is SqlDataReader tsSdr ? tsSdr.GetTimeSpan(i) : reader.GetFieldValue<TimeSpan>(i);
						ts.TryFormat(span, out int wTs, "c", CultureInfo.InvariantCulture);
						str.Append(span[..wTs]);
						break;
					case CsvColType.ByteArray:
						str.Append(Convert.ToBase64String((byte[])reader.GetValue(i)));
						break;
					default:
						throw new NotSupportedException($"Column type '{colTypes[i]}' is not supported.");
				}
			}
			if (i < colTypes.Length - 1)
				str.Append(sep);
		}
	}

	internal static void WriteCsvRow(this StringBuilder str, DbDataReader reader, Type[] fieldTypes)
	{
		var colTypes = new CsvColType[fieldTypes.Length];
		for (int i = 0; i < fieldTypes.Length; i++)
			colTypes[i] = GetCsvTypeInfo(fieldTypes[i]).ColType;
		str.WriteCsvRow(reader, colTypes);
	}

}

public static partial class Db
{
	private static readonly ConcurrentDictionary<Type, (PropertyInfo[] Props, Func<object, object>[] Getters)> _csvTypeCache = new();

	private static (PropertyInfo[] Props, Func<object, object>[] Getters) GetCsvMetadata(Type type)
	{
		return _csvTypeCache.GetOrAdd(type, static t =>
		{
			var props = t.GetProperties();
			var getters = new Func<object, object>[props.Length];
			for (int i = 0; i < props.Length; i++)
				getters[i] = CreateCsvGetter(props[i]);
			return (props, getters);
		});
	}

	private static Func<object, object> CreateCsvGetter(PropertyInfo prop)
	{
		var param = Expression.Parameter(typeof(object), "obj");
		Expression instance = prop.DeclaringType!.IsValueType
			? Expression.Unbox(param, prop.DeclaringType)
			: Expression.Convert(param, prop.DeclaringType);
		Expression property = Expression.Property(instance, prop);
		Expression box = Expression.Convert(property, typeof(object));
		return Expression.Lambda<Func<object, object>>(box, param).Compile();
	}

	/// <summary>Converts an array of objects to a CSV string.</summary>
	/// <param name="objects">The array of objects to convert.</param>
	/// <returns>A CSV formatted string representing the objects, or null if the array is null or empty.</returns>
	public static string Csv(object[] objects)
	{
		if (objects is null || objects.Length == 0)
			return null;

		var (props, propGetters) = GetCsvMetadata(objects[0].GetType());
		int objectCount = objects.Length;
		int propCount = props.Length;
		int estimatedCap = Math.Max(256, objectCount * propCount * 12);
		var str = new StringBuilder(estimatedCap);
		var fieldTypes = str.WriteCsvHeader(props);

		Span<char> span = stackalloc char[64];
		object val;
		for (int i = 0; i < objectCount; i++)
		{
			str.Append(CsvWriter.line);
			for (int p = 0; p < propCount; p++)
			{
				val = propGetters[p](objects[i]);
				if (val is null)
					str.Append('Ø');
				else
				{
					var type = fieldTypes[p];
					var underlying = Nullable.GetUnderlyingType(type) ?? type;

					if (val is DateTime dt)
					{
						long dtSec = new DateTimeOffset(dt).ToUnixTimeSeconds();
						dtSec.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is DateTimeOffset dto)
					{
						dto.ToUnixTimeSeconds().TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is bool b)
						str.Append(b ? '1' : '0');
					else if (val is byte[] bytes)
						str.Append(Convert.ToBase64String(bytes));
					else if (val is float f)
					{
						f.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is double d)
					{
						d.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is decimal dec)
					{
						dec.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is TimeSpan ts)
					{
						ts.TryFormat(span, out int written, "c", CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is int intVal)
					{
						intVal.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is long longVal)
					{
						longVal.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is short shortVal)
					{
						shortVal.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is byte byteVal)
					{
						byteVal.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (val is ISpanFormattable spanFormattable && (underlying == typeof(Guid) || Type.GetTypeCode(underlying) != TypeCode.Object))
					{
						spanFormattable.TryFormat(span, out int written, default, CultureInfo.InvariantCulture);
						str.Append(span[..written]);
					}
					else if (underlying == typeof(Guid) || Type.GetTypeCode(underlying) != TypeCode.Object)
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
		using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
		return await reader.ReadCsv();
	}

	internal static async Task<string> ReadCsv(this DbDataReader reader)
	{
		if (!await reader.ReadAsync())
			return null;

		var str = new StringBuilder(1024);
		var (_, colTypes) = str.WriteCsvHeaderTypes(reader);
		str.WriteCsvRow(reader, colTypes);
		while (await reader.ReadAsync())
			str.WriteCsvRow(reader, colTypes);
		return str.ToString();
	}
}
