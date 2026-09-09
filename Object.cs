using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell
{
	internal static class ObjectReflector
	{
		internal static T GetObject<T>(this SqlDataReader reader) where T : new()
		{
			int fieldCount = reader.FieldCount;
			var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
			var obj = new T();
			for (int i = 0; i < fieldCount; i++)
				props[reader.GetName(i)].SetValue(obj, reader.GetValue(i));
			return obj;
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