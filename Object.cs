using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

using System.Reflection;

namespace nuel;

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