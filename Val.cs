using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
	public static partial class Db
	{
		/// <summary>Returns one value of the primitive type T</summary>
		public static T Val<T>(string query, params (string name, object value)[] parameters) where T : struct
			 => Val<T>(query, false, Data.SqlParams(parameters));

		/// <summary>Returns one value of the primitive type T</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public static T Val<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
			 => Val<T>(query, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Returns one value of the primitive type T</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public static T Val<T>(string query, bool isStoredProc = false) where T : struct
			 => Val<T>(query, isStoredProc, Data.NoParams);

		/// <summary>Returns one value of the primitive type T</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param> 
		public static T Val<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			connection.Open();
			var val = cmd.ExecuteScalar();
			return val is null || val is DBNull ? default : val is T t ? t : (T)Convert.ChangeType(val, typeof(T));
		}
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		/// <summary>Returns one value of the primitive type T</summary>
		public static Task<T> Val<T>(string query, params (string name, object value)[] parameters) where T : struct
			 => Val<T>(query, false, Data.SqlParams(parameters));

		/// <summary>Returns one value of the primitive type T</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public static Task<T> Val<T>(string query, bool isStoredProc, params (string name, object value)[] parameters) where T : struct
			 => Val<T>(query, isStoredProc, Data.SqlParams(parameters));

		/// <summary>Returns one value of the primitive type T</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public static Task<T> Val<T>(string query, bool isStoredProc = false) where T : struct
			 => Val<T>(query, isStoredProc, Data.NoParams);

		/// <summary>Returns one value of the primitive type T</summary>
		/// <param name="isStoredProc">is the query a stored procedure</param>   
		public async static Task<T> Val<T>(string query, bool isStoredProc, params SqlParameter[] parameters) where T : struct
		{
			using var connection = new SqlConnection(Data.ConnectionString);
			using var cmd = new SqlCommand(query, connection);
			if (isStoredProc)
				cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddRange(parameters);
			await connection.OpenAsync();
			var val = await cmd.ExecuteScalarAsync();
			return val is null || val is DBNull ? default : val is T t ? t : (T)Convert.ChangeType(val, typeof(T));
		}
	}
}