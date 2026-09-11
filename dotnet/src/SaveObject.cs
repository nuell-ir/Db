using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace nuel;

internal sealed class ObjectPropertyAccessor
{
	public readonly string Name;
	public readonly string ColumnName;
	public readonly string ParamName;
	public readonly string UpdateFragment;
	public readonly Func<object, object> Getter;

	public ObjectPropertyAccessor(PropertyInfo prop)
	{
		Name = prop.Name;
		ColumnName = $"[{prop.Name}]";
		ParamName = "@" + prop.Name;
		UpdateFragment = $"[{prop.Name}]=@{prop.Name}";
		Getter = CreateGetter(prop);
	}

	private static Func<object, object> CreateGetter(PropertyInfo prop)
	{
		var param = Expression.Parameter(typeof(object), "obj");
		Type declaringType = prop.DeclaringType ?? typeof(object);
		Expression instance = declaringType.IsValueType
			? Expression.Unbox(param, declaringType)
			: Expression.Convert(param, declaringType);
		Expression property = Expression.Property(instance, prop);
		Expression box = Expression.Convert(property, typeof(object));
		return Expression.Lambda<Func<object, object>>(box, param).Compile();
	}
}

internal static class ObjectSaveCache
{
	private static readonly ConcurrentDictionary<Type, ObjectPropertyAccessor[]> _cache = new();

	public static ObjectPropertyAccessor[] GetProperties(Type type)
	{
		return _cache.GetOrAdd(type, static t =>
		{
			var propInfos = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var list = new List<ObjectPropertyAccessor>(propInfos.Length);
			for (int i = 0; i < propInfos.Length; i++)
			{
				var p = propInfos[i];
				if (p.CanRead && p.GetIndexParameters().Length == 0)
					list.Add(new ObjectPropertyAccessor(p));
			}
			return list.ToArray();
		});
	}
}

internal static class ObjectSaveCache<T>
{
	public static readonly ObjectPropertyAccessor[] Properties = ObjectSaveCache.GetProperties(typeof(T));
}

internal static partial class SaveQuery
{
	internal static SaveParams Create<T>(T obj, string table, string idProp)
	{
		ArgumentNullException.ThrowIfNull(obj);
		ArgumentNullException.ThrowIfNull(table);
		idProp ??= "Id";

		var props = typeof(T) != typeof(object) && obj.GetType() == typeof(T)
			? ObjectSaveCache<T>.Properties
			: ObjectSaveCache.GetProperties(obj.GetType());

		int id = 0;
		for (int i = 0; i < props.Length; i++)
		{
			if (string.Equals(props[i].Name, idProp, StringComparison.OrdinalIgnoreCase))
			{
				object val = props[i].Getter(obj);
				if (val is int iVal)
					id = iVal;
				else if (val is not null)
				{
					if (!int.TryParse(val.ToString(), out id))
					{
						try
						{
							id = Convert.ToInt32(val);
						}
						catch
						{
							id = 0;
						}
					}
				}
				break;
			}
		}

		var sqlParams = new List<SqlParameter>(props.Length);
		var str = new StringBuilder(256);
		if (id == 0)
		{
			str.Append("INSERT INTO ").Append(table).Append(" (");
			var vals = new StringBuilder(128);
			bool first = true;

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				if (string.Equals(prop.Name, idProp, StringComparison.OrdinalIgnoreCase))
					continue;

				if (!first)
				{
					str.Append(',');
					vals.Append(',');
				}
				first = false;

				str.Append(prop.ColumnName);
				vals.Append(prop.ParamName);
				sqlParams.Add(CreateSqlParameter(prop.ParamName, prop.Getter(obj)));
			}

			str.Append(") VALUES (").Append(vals).Append("); SELECT CAST(SCOPE_IDENTITY() AS int);");
		}
		else
		{
			str.Append("UPDATE ").Append(table).Append(" SET ");
			bool first = true;

			for (int i = 0; i < props.Length; i++)
			{
				var prop = props[i];
				if (string.Equals(prop.Name, idProp, StringComparison.OrdinalIgnoreCase))
					continue;

				if (!first)
					str.Append(',');
				first = false;

				str.Append(prop.UpdateFragment);
				sqlParams.Add(CreateSqlParameter(prop.ParamName, prop.Getter(obj)));
			}

			string idParamName = "@" + idProp;
			str.Append(" WHERE [").Append(idProp).Append("]=").Append(idParamName);
			sqlParams.Add(new SqlParameter(idParamName, id));
		}

		return new SaveParams(id, str.ToString(), [.. sqlParams]);

		static SqlParameter CreateSqlParameter(string paramName, object value)
		{
			if (value is null)
				return new SqlParameter(paramName, DBNull.Value);

			if (value is Enum e)
				return new SqlParameter(paramName, Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType())));

			if (value is JsonElement je)
				return new SqlParameter(paramName, je.GetRawText());

			if (value is JsonNode jn)
				return new SqlParameter(paramName, jn.ToJsonString());

			if (value is JsonDocument jd)
				return new SqlParameter(paramName, jd.RootElement.GetRawText());

			return new SqlParameter(paramName, value);
		}
	}
}

public static partial class Db
{
	/// <summary>Asynchronously saves an object of type T via an insert or update operation.</summary>
	/// <remarks>If the value of the identity field is 0, the values will be inserted as a record; otherwise, a record with the specified identity will be updated.</remarks>
	/// <typeparam name="T">The type of the object to save.</typeparam>
	/// <param name="obj">The object to save.</param>
	/// <param name="table">The name of the table.</param>   
	/// <param name="idProp">The name of the identity field, the value of which decides whether to insert or update the record.</param>
	/// <returns>A task representing the asynchronous operation, returning the identity of the inserted/updated record, or 0 if no record was updated.</returns>
	public static Task<int> Save<T>(T obj, string table, string idProp = "Id")
		 => Save(SaveQuery.Create(obj, table, idProp));
}
