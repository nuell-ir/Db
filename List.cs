using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
	public static partial class Db
	{
		public static List<T> List<T>(string query, params (string name, object value)[] parameters) where T : struct
			=> List<T>(query, false, Data.SqlParams(parameters));

		public static List<T> List<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
			=> List<T>(query, isStoredProc, Data.SqlParams(parameters));

		public static List<T> List<T>(string query, bool isStoredProc = false) where T : struct
			=> List<T>(query, isStoredProc, Data.NoParams);

		public static List<T> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			connection.Open();
			using var reader = cmd.ExecuteReader();
			if (!reader.HasRows)
				return null;

			var list = new List<T>();
			while (reader.Read())
				list.Add(reader.GetFieldValue<T>(0));
			return list;
		}
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		public static Task<List<T>> List<T>(string query, params (string name, object value)[] parameters) where T : struct
			=> List<T>(query, false, Data.SqlParams(parameters));

		public static Task<List<T>> List<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
			=> List<T>(query, isStoredProc, Data.SqlParams(parameters));

		public static Task<List<T>> List<T>(string query, bool isStoredProc = false) where T : struct
			=> List<T>(query, isStoredProc, Data.NoParams);

		public static async Task<List<T>> List<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			if (!reader.HasRows)
				return null;

			var list = new List<T>();
			while (await reader.ReadAsync())
				list.Add(reader.GetFieldValue<T>(0));
			return list;
		}
	}
}