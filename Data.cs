using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuel
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

		internal static readonly SqlParameter[] NoParams = [];

		internal static SqlParameter[] SqlParams((string name, object value)[] parameters)
		=> [.. parameters.Select(p => new SqlParameter(p.name, p.value ?? DBNull.Value))];

		internal static readonly JsonWriterOptions JsonWriterOptions = new()
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};
	}
}

namespace nuel.Sync
{
	public static partial class Db
	{
		/// <summary>Gets or sets the database connection string.</summary>
		public static string ConnectionString
		{
			get => Data.ConnectionString;
			set => Data.ConnectionString = value;
		}

		/// <summary>Creates a <see cref="SqlParameter"/> whose value is set to <see cref="DBNull.Value"/> if the specified string is null or whitespace.</summary>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="value">The string value of the parameter.</param>
		/// <returns>A <see cref="SqlParameter"/> instance with either the trimmed string or <see cref="DBNull.Value"/>.</returns>
		public static SqlParameter NS(string name, string value)
		=> Data.NullableStringParam(name, value);
	}

	public enum JsonValueType
	{
		Array = nuel.JsonValueType.Array, Object = nuel.JsonValueType.Object, Value = nuel.JsonValueType.Value, Csv = nuel.JsonValueType.Csv
	}
}

namespace nuel.Async
{
	public static partial class Db
	{
		/// <summary>Gets or sets the database connection string.</summary>
		public static string ConnectionString
		{
			get => Data.ConnectionString;
			set => Data.ConnectionString = value;
		}

		/// <summary>Creates a <see cref="SqlParameter"/> whose value is set to <see cref="DBNull.Value"/> if the specified string is null or whitespace.</summary>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="value">The string value of the parameter.</param>
		/// <returns>A <see cref="SqlParameter"/> instance with either the trimmed string or <see cref="DBNull.Value"/>.</returns>
		public static SqlParameter NS(string name, string value)
		=> Data.NullableStringParam(name, value);
	}

	public enum JsonValueType
	{
		Array = nuel.JsonValueType.Array, Object = nuel.JsonValueType.Object, Value = nuel.JsonValueType.Value, Csv = nuel.JsonValueType.Csv
	}
}