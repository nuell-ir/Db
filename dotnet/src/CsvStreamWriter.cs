using System.Buffers;
using System.Buffers.Text;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class CsvWriter
{
	internal static async Task<CsvColType[]> WriteCsvHeaderAsync(this Utf8CsvStreamWriter writer, DbDataReader reader)
	{
		int columns = reader.FieldCount;
		var colTypes = new CsvColType[columns];
		for (int i = 0; i < columns; i++)
		{
			var (flag, colType) = GetCsvTypeInfo(reader.GetFieldType(i));
			colTypes[i] = colType;
			await writer.EnsureCapacityAsync(1);
			writer.WriteByte((byte)flag);
			await writer.WriteStringAsync(reader.GetName(i));
			if (i < columns - 1)
			{
				await writer.EnsureCapacityAsync(1);
				writer.WriteByte((byte)sep);
			}
		}
		return colTypes;
	}

	internal static async ValueTask WriteCsvRowAsync(this Utf8CsvStreamWriter writer, DbDataReader reader, CsvColType[] colTypes)
	{
		await writer.EnsureCapacityAsync(1);
		writer.WriteByte((byte)line);

		for (int i = 0; i < colTypes.Length; i++)
		{
			// Reserve room for any fixed-size value; strings and binary reserve their own space.
			await writer.EnsureCapacityAsync(64);
			if (reader.IsDBNull(i))
			{
				writer.WriteNull();
			}
			else
			{
				switch (colTypes[i])
				{
					case CsvColType.Int32:
						writer.WriteInt32(reader.GetInt32(i));
						break;
					case CsvColType.Int64:
						writer.WriteInt64(reader.GetInt64(i));
						break;
					case CsvColType.Int16:
						writer.WriteInt16(reader.GetInt16(i));
						break;
					case CsvColType.Byte:
						writer.WriteByteValue(reader.GetByte(i));
						break;
					case CsvColType.Single:
						writer.WriteFloat(reader.GetFloat(i));
						break;
					case CsvColType.Double:
						writer.WriteDouble(reader.GetDouble(i));
						break;
					case CsvColType.Decimal:
						writer.WriteDecimal(reader.GetDecimal(i));
						break;
					case CsvColType.DateTime:
						writer.WriteDateTime(reader.GetDateTime(i));
						break;
					case CsvColType.Boolean:
						writer.WriteByte(reader.GetBoolean(i) ? (byte)'1' : (byte)'0');
						break;
					case CsvColType.String:
						await writer.WriteStringAsync(reader.GetString(i));
						break;
					case CsvColType.Guid:
						writer.WriteGuid(reader.GetGuid(i));
						break;
					case CsvColType.DateTimeOffset:
						writer.WriteDateTimeOffset(reader is SqlDataReader sdr ? sdr.GetDateTimeOffset(i) : reader.GetFieldValue<DateTimeOffset>(i));
						break;
					case CsvColType.TimeSpan:
						writer.WriteTimeSpan(reader is SqlDataReader tsSdr ? tsSdr.GetTimeSpan(i) : reader.GetFieldValue<TimeSpan>(i));
						break;
					case CsvColType.ByteArray:
						await writer.WriteBytesBase64Async((byte[])reader.GetValue(i));
						break;
					default:
						throw new NotSupportedException($"Column type '{colTypes[i]}' is not supported.");
				}
			}

			if (i < colTypes.Length - 1)
			{
				await writer.EnsureCapacityAsync(1);
				writer.WriteByte((byte)sep);
			}
		}
	}

	internal static async Task WriteCsvRowAsync(this Utf8CsvStreamWriter writer, DbDataReader reader, Type[] fieldTypes)
	{
		var colTypes = new CsvColType[fieldTypes.Length];
		for (int i = 0; i < fieldTypes.Length; i++)
			colTypes[i] = GetCsvTypeInfo(fieldTypes[i]).ColType;
		await writer.WriteCsvRowAsync(reader, colTypes);
	}
}

internal sealed class Utf8CsvStreamWriter : IAsyncDisposable
{
	private readonly Stream _stream;
	private readonly CancellationToken _cancellationToken;
	private byte[] _buffer;
	private int _pos;
	private const int DefaultBufferSize = 4096;

	public Utf8CsvStreamWriter(Stream stream, CancellationToken cancellationToken = default)
	{
		_stream = stream;
		_cancellationToken = cancellationToken;
		_buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
		_pos = 0;
	}

	public Span<byte> FreeSpan => _buffer.AsSpan(_pos);
	public int FreeCapacity => _buffer.Length - _pos;

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteInt32(int val)
	{
		Utf8Formatter.TryFormat(val, FreeSpan, out int written);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteInt64(long val)
	{
		Utf8Formatter.TryFormat(val, FreeSpan, out int written);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteInt16(short val)
	{
		Utf8Formatter.TryFormat(val, FreeSpan, out int written);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteByteValue(byte val)
	{
		Utf8Formatter.TryFormat(val, FreeSpan, out int written);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteFloat(float val)
	{
		val.TryFormat(FreeSpan, out int written, default, CultureInfo.InvariantCulture);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteDouble(double val)
	{
		val.TryFormat(FreeSpan, out int written, default, CultureInfo.InvariantCulture);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteDecimal(decimal val)
	{
		val.TryFormat(FreeSpan, out int written, default, CultureInfo.InvariantCulture);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteDateTime(DateTime val)
	{
		val.TryFormat(FreeSpan, out int written, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteDateTimeOffset(DateTimeOffset val)
	{
		val.TryFormat(FreeSpan, out int written, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteTimeSpan(TimeSpan val)
	{
		val.TryFormat(FreeSpan, out int written, "c", CultureInfo.InvariantCulture);
		_pos += written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteGuid(Guid val)
	{
		Utf8Formatter.TryFormat(val, FreeSpan, out int written, 'D');
		_pos += written;
	}

	public async ValueTask WriteStringAsync(string str)
	{
		if (string.IsNullOrEmpty(str))
			return;

		int maxBytes = Encoding.UTF8.GetMaxByteCount(str.Length);
		if (_buffer.Length - _pos < maxBytes)
		{
			int actualBytes = Encoding.UTF8.GetByteCount(str);
			if (_buffer.Length - _pos < actualBytes)
			{
				await EnsureCapacitySlowAsync(actualBytes);
			}
		}
		_pos += Encoding.UTF8.GetBytes(str.AsSpan(), _buffer.AsSpan(_pos));
	}

	public async ValueTask WriteBytesBase64Async(byte[] bytes)
	{
		if (bytes == null || bytes.Length == 0)
			return;

		int maxLen = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
		if (_buffer.Length - _pos < maxLen)
		{
			await EnsureCapacitySlowAsync(maxLen);
		}
		Base64.EncodeToUtf8(bytes, _buffer.AsSpan(_pos), out _, out int written);
		_pos += written;
	}

	public async ValueTask FlushAsync()
	{
		if (_pos > 0)
		{
			await _stream.WriteAsync(_buffer.AsMemory(0, _pos), _cancellationToken);
			_pos = 0;
		}
	}

	public async ValueTask FinishAsync()
	{
		await FlushAsync();
		await _stream.FlushAsync(_cancellationToken);
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

