using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace nuel;

internal readonly struct ColumnMapping<T>
{
	public readonly int Ordinal;
	public readonly Action<T, DbDataReader, int> Reader;

	public ColumnMapping(int ordinal, Action<T, DbDataReader, int> reader)
	{
		Ordinal = ordinal;
		Reader = reader;
	}
}

internal static class TypeCache<T>
{
	private static readonly Dictionary<string, Action<T, DbDataReader, int>> _columnReaders;
	private static readonly Dictionary<string, PropertyInfo> _properties;

	static TypeCache()
	{
		var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		_columnReaders = new Dictionary<string, Action<T, DbDataReader, int>>(props.Length, StringComparer.OrdinalIgnoreCase);
		_properties = new Dictionary<string, PropertyInfo>(props.Length, StringComparer.OrdinalIgnoreCase);

		foreach (var prop in props)
		{
			if (!prop.CanWrite || prop.GetSetMethod() == null)
				continue;

			_properties.TryAdd(prop.Name, prop);
			_columnReaders.TryAdd(prop.Name, CreateColumnReader(prop));
		}
	}

	public static Action<T, DbDataReader, int> GetColumnReader(string columnName)
	{
		_columnReaders.TryGetValue(columnName, out var reader);
		return reader;
	}

	public static Dictionary<string, PropertyInfo> Properties => _properties;

	private static Action<T, DbDataReader, int> CreateColumnReader(PropertyInfo prop)
	{
		Type propType = prop.PropertyType;
		Type targetType = Nullable.GetUnderlyingType(propType) ?? propType;
		bool isNullableOrRef = !propType.IsValueType || Nullable.GetUnderlyingType(propType) != null;

		if (typeof(T).IsValueType)
		{
			return (target, reader, ordinal) =>
			{
				object boxed = target;
				if (reader.IsDBNull(ordinal))
				{
					if (isNullableOrRef)
						prop.SetValue(boxed, null);
				}
				else
				{
					object val = reader.GetValue(ordinal);
					if (val.GetType() != targetType)
						val = ConvertValue(val, targetType);
					prop.SetValue(boxed, val);
				}
			};
		}

		var targetParam = Expression.Parameter(typeof(T), "target");
		var valParam = Expression.Parameter(typeof(object), "val");
		Expression castVal = Expression.Convert(valParam, propType);
		Expression assign = Expression.Assign(Expression.Property(targetParam, prop), castVal);
		Action<T, object> setter = Expression.Lambda<Action<T, object>>(assign, targetParam, valParam).Compile();

		if (isNullableOrRef)
		{
			return (target, reader, ordinal) =>
			{
				if (reader.IsDBNull(ordinal))
				{
					setter(target, null);
				}
				else
				{
					object val = reader.GetValue(ordinal);
					if (val.GetType() != targetType)
						val = ConvertValue(val, targetType);
					setter(target, val);
				}
			};
		}
		else
		{
			return (target, reader, ordinal) =>
			{
				if (!reader.IsDBNull(ordinal))
				{
					object val = reader.GetValue(ordinal);
					if (val.GetType() != targetType)
						val = ConvertValue(val, targetType);
					setter(target, val);
				}
			};
		}
	}

	private static object ConvertValue(object val, Type targetType)
	{
		if (targetType.IsEnum)
			return Enum.ToObject(targetType, val);
		if (targetType == typeof(Guid) && val is string str)
			return Guid.Parse(str);
		return Convert.ChangeType(val, targetType);
	}
}

internal static class ObjectReflector
{
	internal static ColumnMapping<T>[] GetColumnMappings<T>(this DbDataReader reader) where T : new()
	{
		int fieldCount = reader.FieldCount;
		var list = new List<ColumnMapping<T>>(fieldCount);
		for (int i = 0; i < fieldCount; i++)
		{
			var colReader = TypeCache<T>.GetColumnReader(reader.GetName(i));
			if (colReader != null)
				list.Add(new ColumnMapping<T>(i, colReader));
		}
		return list.ToArray();
	}

	internal static T GetObject<T>(this DbDataReader reader, ColumnMapping<T>[] mappings) where T : new()
	{
		var obj = new T();
		for (int i = 0; i < mappings.Length; i++)
			mappings[i].Reader(obj, reader, mappings[i].Ordinal);
		return obj;
	}

	internal static T GetObject<T>(this DbDataReader reader) where T : new()
	{
		var mappings = reader.GetColumnMappings<T>();
		return reader.GetObject(mappings);
	}

	internal static T GetObject<T>(this DbDataReader reader, Dictionary<string, PropertyInfo> props) where T : new()
	{
		int fieldCount = reader.FieldCount;
		var obj = new T();
		for (int i = 0; i < fieldCount; i++)
		{
			if (props.TryGetValue(reader.GetName(i), out var prop) && prop.CanWrite)
			{
				if (reader.IsDBNull(i))
				{
					if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null)
						prop.SetValue(obj, null);
				}
				else
				{
					var val = reader.GetValue(i);
					var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
					if (val.GetType() == targetType)
						prop.SetValue(obj, val);
					else
						prop.SetValue(obj, Convert.ChangeType(val, targetType));
				}
			}
		}
		return obj;
	}
}

public static partial class Db
{
	/// <summary>Asynchronously executes the query and maps the first row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of object to create and populate from the result set row.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an instance of <typeparamref name="T"/> populated from the first row, or default if no rows were returned.</returns>
	public static Task<T> Object<T>(string query, params (string name, object value)[] parameters) where T : new()
		=> Object<T>(query, false, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and maps the first row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of object to create and populate from the result set row.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning an instance of <typeparamref name="T"/> populated from the first row, or default if no rows were returned.</returns>
	public static Task<T> Object<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
		=> Object<T>(query, isStoredProc, Data.SqlParams(parameters));

	/// <summary>Asynchronously executes the query and maps the first row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of object to create and populate from the result set row.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning an instance of <typeparamref name="T"/> populated from the first row, or default if no rows were returned.</returns>
	public static Task<T> Object<T>(string query, bool isStoredProc = false) where T : new()
		=> Object<T>(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and maps the first row of the result set to a new instance of <typeparamref name="T"/>.</summary>
	/// <typeparam name="T">The type of object to create and populate from the result set row.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning an instance of <typeparamref name="T"/> populated from the first row, or default if no rows were returned.</returns>
	public static async Task<T> Object<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : new()
	{
		using var connection = new SqlConnection(Data.ConnectionString);
		using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddRange(parameters);
		await connection.OpenAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		if (reader.HasRows)
		{
			await reader.ReadAsync();
			return reader.GetObject<T>();
		}
		else
			return default;
	}
}
