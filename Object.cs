using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

using System.Reflection;

namespace nuell
{
	internal static class ObjectReflector
	{
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

		internal static T GetObject<T>(this DbDataReader reader) where T : new()
		{
			var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
			return reader.GetObject<T>(props);
		}
	}
}

namespace nuell.Sync
{
	public static partial class Db
	{
		public static T Object<T>(string query, params (string name, object value)[] parameters) where T : new()
			=> Object<T>(query, false, Data.SqlParams(parameters));

		public static T Object<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
			=> Object<T>(query, isStoredProc, Data.SqlParams(parameters));

		public static T Object<T>(string query, bool isStoredProc = false) where T : new()
			=> Object<T>(query, isStoredProc, Data.NoParams);

		public static T Object<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : new()
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			connection.Open();
			using var reader = cmd.ExecuteReader();
			if (reader.HasRows)
			{
				reader.Read();
				return reader.GetObject<T>();
			}
			else
				return default;
		}
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		public static Task<T> Object<T>(string query, params (string name, object value)[] parameters) where T : new()
			=> Object<T>(query, false, Data.SqlParams(parameters));

		public static Task<T> Object<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : new()
			=> Object<T>(query, isStoredProc, Data.SqlParams(parameters));

		public static Task<T> Object<T>(string query, bool isStoredProc = false) where T : new()
			=> Object<T>(query, isStoredProc, Data.NoParams);

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
}