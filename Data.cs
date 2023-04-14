using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuell
{
	internal enum JsonValueType
	{
		Array, Object, Value, Csv
	}

	public static partial class Data
	{
		internal static string ConnectionString;

		internal static SqlParameter NullableStringParam(string name, string value)
		=> new SqlParameter(name, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim());

		internal static readonly SqlParameter[] NoParams = new SqlParameter[] { };

		internal static SqlParameter[] SqlParams((string name, object value)[] parameters)
		=> parameters.Select(p => new SqlParameter(p.name, p.value ?? DBNull.Value)).ToArray();

		internal static readonly JsonWriterOptions JsonWriterOptions = new()
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};
	}
}

namespace nuell.Sync
{
	public static partial class Db
	{
		public static string ConnectionString
		{
			get => nuell.Data.ConnectionString;
			set => nuell.Data.ConnectionString = value;
		}

		public static SqlParameter NS(string name, string value)
		=> Data.NullableStringParam(name, value);
	}

	public enum JsonValueType
	{
		Array = nuell.JsonValueType.Array, Object = nuell.JsonValueType.Object, Value = nuell.JsonValueType.Value, Csv = nuell.JsonValueType.Csv
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		public static string ConnectionString
		{
			get => nuell.Data.ConnectionString;
			set => nuell.Data.ConnectionString = value;
		}

		public static SqlParameter NS(string name, string value)
		=> Data.NullableStringParam(name, value);
	}

	public enum JsonValueType
	{
		Array = nuell.JsonValueType.Array, Object = nuell.JsonValueType.Object, Value = nuell.JsonValueType.Value, Csv = nuell.JsonValueType.Csv
	}
}