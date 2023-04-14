using System.Data;
using Microsoft.Data.SqlClient;

namespace nuell.Sync
{
	public static partial class Db
	{
		public static DataTable Table(string query, params (string name, object value)[] parameters)
			 => Table(query, false, Data.SqlParams(parameters));

		public static DataTable Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => Table(query, isStoredProc, Data.SqlParams(parameters));

		public static DataTable Table(string query, bool isStoredProc = false)
			 => Table(query, isStoredProc, Data.NoParams);

		public static DataTable Table(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			var dt = new DataTable();
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var adapter = new SqlDataAdapter(query, cnnct);
			if (isStoredProc)
				adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
			adapter.SelectCommand.Parameters.AddRange(parameters);
			adapter.Fill(dt);
			return dt;
		}
	}
}

namespace nuell.Async
{
	public static partial class Db
	{
		public static Task<DataTable> Table(string query, params (string name, object value)[] parameters)
			 => Table(query, false, Data.SqlParams(parameters));

		public static Task<DataTable> Table(string query, bool isStoredProc, params (string name, object value)[] parameters)
			 => Table(query, isStoredProc, Data.SqlParams(parameters));

		public static Task<DataTable> Table(string query, bool isStoredProc = false)
			 => Table(query, isStoredProc, Data.NoParams);

		public static async Task<DataTable> Table(string query, bool isStoredProc, params SqlParameter[] parameters)
		{
			using var cnnct = new SqlConnection(Data.ConnectionString);
			using var bulkCopy = new SqlBulkCopy(cnnct);
			using var cmnd = new SqlCommand(query, cnnct);
			if (isStoredProc)
				cmnd.CommandType = CommandType.StoredProcedure;
			cmnd.Parameters.AddRange(parameters);
			await cnnct.OpenAsync();
			using var reader = await cmnd.ExecuteReaderAsync();
			if (!reader.HasRows)
				return null;
			var dt = new DataTable();
			await reader.ReadAsync();
			int columns = reader.FieldCount;
			for (int i = 0; i < columns; i++)
				dt.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
			var values = new object[columns];
			reader.GetValues(values);
			dt.Rows.Add(values);
			while (await reader.ReadAsync())
			{
				reader.GetValues(values);
				dt.Rows.Add(values);
			}
			return dt;
		}
	}
}