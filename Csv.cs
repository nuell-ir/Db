using System.Buffers;
using System.Buffers.Text;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
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
			Span<char> span = stackalloc char[64];
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
							reader.GetInt32(i).TryFormat(span, out int wInt, default, CultureInfo.InvariantCulture);
							str.Append(span[..wInt]);
							break;
						case TypeCode.Int64:
							reader.GetInt64(i).TryFormat(span, out int wLong, default, CultureInfo.InvariantCulture);
							str.Append(span[..wLong]);
							break;
						case TypeCode.Int16:
							reader.GetInt16(i).TryFormat(span, out int wShort, default, CultureInfo.InvariantCulture);
							str.Append(span[..wShort]);
							break;
						case TypeCode.Byte:
							reader.GetByte(i).TryFormat(span, out int wByte, default, CultureInfo.InvariantCulture);
							str.Append(span[..wByte]);
							break;
						case TypeCode.Single:
							reader.GetFloat(i).TryFormat(span, out int wFloat, default, CultureInfo.InvariantCulture);
							str.Append(span[..wFloat]);
							break;
						case TypeCode.Double:
							reader.GetDouble(i).TryFormat(span, out int wDouble, default, CultureInfo.InvariantCulture);
							str.Append(span[..wDouble]);
							break;
						case TypeCode.Decimal:
							reader.GetDecimal(i).TryFormat(span, out int wDec, default, CultureInfo.InvariantCulture);
							str.Append(span[..wDec]);
							break;
						case TypeCode.DateTime:
							long dtSec = new DateTimeOffset(reader.GetDateTime(i)).ToUnixTimeSeconds();
							dtSec.TryFormat(span, out int wDt, default, CultureInfo.InvariantCulture);
							str.Append(span[..wDt]);
							break;
						case TypeCode.Boolean:
							str.Append(reader.GetBoolean(i) ? '1' : '0');
							break;
						case TypeCode.Char:
						case TypeCode.String:
							str.Append(reader.GetString(i));
							break;
						default:
							if (type == typeof(Guid))
							{
								reader.GetGuid(i).TryFormat(span, out int wGuid, "D");
								str.Append(span[..wGuid]);
							}
							else if (type == typeof(DateTimeOffset))
							{
								long dtoSec = (reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(i) : reader.GetFieldValue<DateTimeOffset>(i)).ToUnixTimeSeconds();
								dtoSec.TryFormat(span, out int wDto, default, CultureInfo.InvariantCulture);
								str.Append(span[..wDto]);
							}
							else if (type == typeof(TimeSpan))
							{
								TimeSpan ts = reader is SqlDataReader sdr ? sdr.GetTimeSpan(i) : reader.GetFieldValue<TimeSpan>(i);
								ts.TryFormat(span, out int wTs, "c", CultureInfo.InvariantCulture);
								str.Append(span[..wTs]);
							}
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

		internal static async Task<Type[]> WriteCsvHeaderAsync(this Utf8CsvStreamWriter writer, DbDataReader reader)
		{
			int columns = reader.FieldCount;
			var fieldTypes = new Type[columns];
			Type type;
			for (int i = 0; i < columns; i++)
			{
				type = reader.GetFieldType(i);
				fieldTypes[i] = type;
				char flag = GetCsvTypeFlag(type);
				await writer.EnsureCapacityAsync(1);
				writer.WriteByte((byte)flag);
				await writer.WriteStringAsync(reader.GetName(i));
				if (i < columns - 1)
				{
					await writer.EnsureCapacityAsync(1);
					writer.WriteByte((byte)sep);
				}
			}
			return fieldTypes;
		}

		internal static async Task WriteCsvRowAsync(this Utf8CsvStreamWriter writer, DbDataReader reader, Type[] fieldTypes)
		{
			await writer.EnsureCapacityAsync(1);
			writer.WriteByte((byte)line);

			for (int i = 0; i < fieldTypes.Length; i++)
			{
				if (reader.IsDBNull(i))
				{
					await writer.EnsureCapacityAsync(2);
					writer.WriteNull();
				}
				else
				{
					var type = fieldTypes[i];
					switch (Type.GetTypeCode(type))
					{
						case TypeCode.Int32:
							await writer.EnsureCapacityAsync(16);
							Utf8Formatter.TryFormat(reader.GetInt32(i), writer.FreeSpan, out int wInt);
							writer.Advance(wInt);
							break;
						case TypeCode.Int64:
							await writer.EnsureCapacityAsync(32);
							Utf8Formatter.TryFormat(reader.GetInt64(i), writer.FreeSpan, out int wLong);
							writer.Advance(wLong);
							break;
						case TypeCode.Int16:
							await writer.EnsureCapacityAsync(16);
							Utf8Formatter.TryFormat(reader.GetInt16(i), writer.FreeSpan, out int wShort);
							writer.Advance(wShort);
							break;
						case TypeCode.Byte:
							await writer.EnsureCapacityAsync(8);
							Utf8Formatter.TryFormat(reader.GetByte(i), writer.FreeSpan, out int wByte);
							writer.Advance(wByte);
							break;
						case TypeCode.Single:
							await writer.EnsureCapacityAsync(32);
							reader.GetFloat(i).TryFormat(writer.FreeSpan, out int wFloat, default, CultureInfo.InvariantCulture);
							writer.Advance(wFloat);
							break;
						case TypeCode.Double:
							await writer.EnsureCapacityAsync(32);
							reader.GetDouble(i).TryFormat(writer.FreeSpan, out int wDouble, default, CultureInfo.InvariantCulture);
							writer.Advance(wDouble);
							break;
						case TypeCode.Decimal:
							await writer.EnsureCapacityAsync(40);
							reader.GetDecimal(i).TryFormat(writer.FreeSpan, out int wDec, default, CultureInfo.InvariantCulture);
							writer.Advance(wDec);
							break;
						case TypeCode.DateTime:
							await writer.EnsureCapacityAsync(32);
							long dtSec = new DateTimeOffset(reader.GetDateTime(i)).ToUnixTimeSeconds();
							Utf8Formatter.TryFormat(dtSec, writer.FreeSpan, out int wDt);
							writer.Advance(wDt);
							break;
						case TypeCode.Boolean:
							await writer.EnsureCapacityAsync(1);
							writer.WriteByte(reader.GetBoolean(i) ? (byte)'1' : (byte)'0');
							break;
						case TypeCode.Char:
						case TypeCode.String:
							await writer.WriteStringAsync(reader.GetString(i));
							break;
						default:
							if (type == typeof(Guid))
							{
								await writer.EnsureCapacityAsync(36);
								Utf8Formatter.TryFormat(reader.GetGuid(i), writer.FreeSpan, out int wGuid, 'D');
								writer.Advance(wGuid);
							}
							else if (type == typeof(DateTimeOffset))
							{
								await writer.EnsureCapacityAsync(32);
								long dtoSec = (reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(i) : reader.GetFieldValue<DateTimeOffset>(i)).ToUnixTimeSeconds();
								Utf8Formatter.TryFormat(dtoSec, writer.FreeSpan, out int wDto);
								writer.Advance(wDto);
							}
							else if (type == typeof(TimeSpan))
							{
								await writer.EnsureCapacityAsync(32);
								TimeSpan ts = reader is SqlDataReader sdr ? sdr.GetTimeSpan(i) : reader.GetFieldValue<TimeSpan>(i);
								ts.TryFormat(writer.FreeSpan, out int wTs, "c", CultureInfo.InvariantCulture);
								writer.Advance(wTs);
							}
							else if (type == typeof(byte[]))
							{
								await writer.WriteBytesBase64Async((byte[])reader.GetValue(i));
							}
							else
							{
								throw new NotSupportedException($"Type '{type.FullName}' is not supported.");
							}
							break;
					}
				}

				if (i < fieldTypes.Length - 1)
				{
					await writer.EnsureCapacityAsync(1);
					writer.WriteByte((byte)sep);
				}
			}
		}
	}

internal sealed class Utf8CsvStreamWriter : IAsyncDisposable
{
	private readonly Stream _stream;
	private byte[] _buffer;
	private int _pos;
	private const int DefaultBufferSize = 32768;

	public Utf8CsvStreamWriter(Stream stream)
	{
		_stream = stream;
		_buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
		_pos = 0;
	}

	public Span<byte> FreeSpan => _buffer.AsSpan(_pos);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Advance(int count) => _pos += count;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ValueTask EnsureCapacityAsync(int needed)
	{
		if (_buffer.Length - _pos >= needed)
			return ValueTask.CompletedTask;

		return EnsureCapacitySlowAsync(needed);
	}

	private async ValueTask EnsureCapacitySlowAsync(int needed)
	{
		await FlushAsync();
		if (_buffer.Length < needed)
		{
			byte[] newBuf = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, needed));
			ArrayPool<byte>.Shared.Return(_buffer);
			_buffer = newBuf;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteByte(byte b)
	{
		_buffer[_pos++] = b;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteNull()
	{
		_buffer[_pos++] = 0xC3;
		_buffer[_pos++] = 0x98;
	}

	public async ValueTask WriteStringAsync(string str)
	{
		if (string.IsNullOrEmpty(str))
			return;

		int byteCount = Encoding.UTF8.GetByteCount(str);
		await EnsureCapacityAsync(byteCount);
		_pos += Encoding.UTF8.GetBytes(str.AsSpan(), _buffer.AsSpan(_pos));
	}

	public async ValueTask WriteBytesBase64Async(byte[] bytes)
	{
		if (bytes == null || bytes.Length == 0)
			return;

		int maxLen = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		await EnsureCapacityAsync(maxLen);
		Base64.EncodeToUtf8(bytes, _buffer.AsSpan(_pos), out _, out int written);
		_pos += written;
	}

	public async ValueTask FlushAsync()
	{
		if (_pos > 0)
		{
			await _stream.WriteAsync(_buffer.AsMemory(0, _pos));
			_pos = 0;
		}
	}

	public async ValueTask FinishAsync()
	{
		await FlushAsync();
		await _stream.FlushAsync();
	}

	public ValueTask DisposeAsync()
	{
		if (_buffer != null)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
			_buffer = null;
		}
		return ValueTask.CompletedTask;
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

		Span<char> span = stackalloc char[64];
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

		/// <summary>Asynchronously converts the query result to UTF-8 CSV directly written to the specified stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 CSV directly to.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 CSV directly to the stream.</returns>
		public static Task<string> Csv(string query, Stream stream, bool isStoredProc = false)
			 => Csv(query, stream, isStoredProc, Data.NoParams);

		/// <summary>Asynchronously converts the query result to UTF-8 CSV directly written to the specified stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 CSV directly to.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 CSV directly to the stream.</returns>
		public static Task<string> Csv(string query, Stream stream, params (string name, object value)[] parameters)
			=> Csv(query, stream, false, Data.SqlParams(parameters));

		/// <summary>Asynchronously converts the query result to UTF-8 CSV directly written to the specified stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 CSV directly to.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <returns>A task representing the asynchronous operation, returning null after writing UTF-8 CSV directly to the stream.</returns>
		public static Task<string> Csv(string query, Stream stream, bool isStoredProc, params (string name, object value)[] parameters)
			=> Csv(query, stream, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Asynchronously converts the query result to a CSV string.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The SQL parameters to apply to the command.</param>
		/// <returns>A task representing the asynchronous operation, returning a CSV formatted string, or null if no rows were returned.</returns>
		public static Task<string> Csv(string query, bool isStoredProc, params SqlParameter[] parameters)
			=> Csv(query, null, isStoredProc, parameters);

		/// <summary>Asynchronously converts the query result to a CSV string or writes UTF-8 CSV directly to a stream.</summary>
		/// <param name="query">The SQL query or stored procedure name to execute.</param>
		/// <param name="stream">The stream to write UTF-8 CSV directly to, or null to return a CSV string.</param>
		/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
		/// <param name="parameters">The SQL parameters to apply to the command.</param>
		/// <returns>A task representing the asynchronous operation, returning a CSV formatted string, or null if a stream is provided or if no rows were returned.</returns>
		public static async Task<string> Csv(string query, Stream stream, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			return await reader.ReadCsv(stream);
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

		internal static Task<string> ReadCsv(this SqlDataReader reader)
			=> ReadCsv((DbDataReader)reader, null);

		internal static async Task<string> ReadCsv(this DbDataReader reader, Stream stream = null)
		{
			if (!reader.HasRows)
				return null;

			if (stream != null)
			{
				await using var writer = new Utf8CsvStreamWriter(stream);
				await reader.ReadAsync();
				var fieldTypes = await writer.WriteCsvHeaderAsync(reader);
				await writer.WriteCsvRowAsync(reader, fieldTypes);
				while (await reader.ReadAsync())
					await writer.WriteCsvRowAsync(reader, fieldTypes);
				await writer.FinishAsync();
				return null;
			}
			else
			{
				var str = new StringBuilder();
				await reader.ReadAsync();
				var fieldTypes = str.WriteCsvHeader(reader);
				str.WriteCsvRow(reader, fieldTypes);
				while (await reader.ReadAsync())
					str.WriteCsvRow(reader, fieldTypes);
				return str.ToString();
			}
		}
	}
