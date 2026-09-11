using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace nuel;

/// <summary>Represents the format or structure to use when outputting JSON data.</summary>
public enum JsonValueType
{
	/// <summary>JSON array format.</summary>
	Array,
	/// <summary>JSON object format.</summary>
	Object,
	/// <summary>Raw scalar value format.</summary>
	Value,
	/// <summary>CSV formatted string within JSON.</summary>
	Csv
}

public static partial class Data
{
	internal static string ConnectionString;

	internal static SqlParameter NullableStringParam(string name, string value)
	=> new SqlParameter(name, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim());

	internal static readonly SqlParameter[] NoParams = [];

	internal static SqlParameter[] SqlParams((string name, object value)[] parameters)
	{
		if (parameters == null || parameters.Length == 0)
			return NoParams;

		var result = new SqlParameter[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
			result[i] = new SqlParameter(parameters[i].name, parameters[i].value ?? DBNull.Value);
		return result;
	}

	internal static readonly JsonWriterOptions JsonWriterOptions = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	internal static string EscapeIdentifier(string identifier)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

		var parts = identifier.Split('.');
		for (int i = 0; i < parts.Length; i++)
		{
			var part = parts[i].Trim();
			if (part.Length >= 2 && part.StartsWith('[') && part.EndsWith(']'))
				part = part[1..^1].Replace("]]", "]");

			parts[i] = $"[{part.Replace("]", "]]")}]";
		}
		return string.Join(".", parts);
	}

	internal static void AttachParams(SqlCommand cmd, (string name, object value)[] parameters)
	{
		if (parameters is { Length: > 0 })
		{
			for (int i = 0; i < parameters.Length; i++)
				cmd.Parameters.Add(new SqlParameter(parameters[i].name, parameters[i].value ?? DBNull.Value));
		}
	}

	internal static void AttachParams(SqlCommand cmd, SqlParameter[] parameters)
	{
		if (parameters is { Length: > 0 })
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				var p = parameters[i];
				if (p is null)
					continue;

				try
				{
					cmd.Parameters.Add(p);
				}
				catch (ArgumentException)
				{
					cmd.Parameters.Add(CloneParam(p));
				}
			}
		}
	}

	internal static SqlParameter CloneParam(SqlParameter param)
	{
		return param is ICloneable cloneable
			? (SqlParameter)cloneable.Clone()
			: new SqlParameter(param.ParameterName, param.Value)
			{
				SqlDbType = param.SqlDbType,
				Direction = param.Direction,
				Size = param.Size,
				Precision = param.Precision,
				Scale = param.Scale,
				IsNullable = param.IsNullable
			};
	}
}

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
