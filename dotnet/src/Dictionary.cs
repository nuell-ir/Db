using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace nuel;

public static partial class Db
{
	/// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
	/// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
	/// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
	public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, params (string name, object value)[] parameters) where K : notnull
		 => Dictionary<K, V>(query, false, parameters);

	/// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
	/// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
	/// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The parameters for the SQL query.</param>
	/// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
	public async static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params (string name, object value)[] parameters) where K : notnull
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(cmd, parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess).ConfigureAwait(false);
		return await ReadDictionaryAsync<K, V>(reader).ConfigureAwait(false);
	}

	/// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
	/// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
	/// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
	public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc = false) where K : notnull
		 => Dictionary<K, V>(query, isStoredProc, Data.NoParams);

	/// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
	/// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
	/// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
	public static Task<Dictionary<K, V>> Dictionary<K, V>(string query, params SqlParameter[] parameters) where K : notnull
		 => Dictionary<K, V>(query, false, parameters);

	/// <summary>Asynchronously executes the query and maps the first two columns of the result set into a <see cref="System.Collections.Generic.Dictionary{K, V}"/>.</summary>
	/// <typeparam name="K">The type of the dictionary keys, read from the first column.</typeparam>
	/// <typeparam name="V">The type of the dictionary values, read from the second column.</typeparam>
	/// <param name="query">The SQL query or stored procedure name to execute.</param>
	/// <param name="isStoredProc">Whether the query is a stored procedure.</param>
	/// <param name="parameters">The SQL parameters to apply to the command.</param>
	/// <returns>A task representing the asynchronous operation, returning a dictionary of keys and values, or null if no rows were returned.</returns>
	public async static Task<Dictionary<K, V>> Dictionary<K, V>(string query, bool isStoredProc, params SqlParameter[] parameters) where K : notnull
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(query);

		await using var connection = new SqlConnection(Data.ConnectionString);
		await using var cmd = new SqlCommand(query, connection);
		if (isStoredProc)
			cmd.CommandType = CommandType.StoredProcedure;
		Data.AttachParams(cmd, parameters);
		await connection.OpenAsync().ConfigureAwait(false);
		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess).ConfigureAwait(false);
		return await ReadDictionaryAsync<K, V>(reader).ConfigureAwait(false);
	}

	internal static async Task<Dictionary<K, V>> ReadDictionaryAsync<K, V>(DbDataReader reader) where K : notnull
	{
		if (!reader.HasRows || reader.FieldCount < 2)
			return null;

		var dictionary = new Dictionary<K, V>();
		Type targetKeyType = Nullable.GetUnderlyingType(typeof(K)) ?? typeof(K);
		Type targetValType = Nullable.GetUnderlyingType(typeof(V)) ?? typeof(V);
		bool sameKeyType = reader.GetFieldType(0) == typeof(K);
		bool sameValType = reader.GetFieldType(1) == typeof(V);

		while (await reader.ReadAsync().ConfigureAwait(false))
		{
			if (reader.IsDBNull(0))
				continue;

			K key = ReadColumn<K>(reader, 0, targetKeyType, sameKeyType);
			V val = reader.IsDBNull(1)
				? default
				: ReadColumn<V>(reader, 1, targetValType, sameValType);

			dictionary[key] = val;
		}

		return dictionary;
	}

	private static T ReadColumn<T>(DbDataReader reader, int ordinal, Type targetType, bool sameType)
	{
		if (sameType)
			return reader.GetFieldValue<T>(ordinal);

		object val = reader.GetValue(ordinal);
		if (val is T t)
			return t;

		if (targetType.IsEnum)
			return (T)Enum.ToObject(targetType, val);
		if (targetType == typeof(Guid) && val is string str)
			return (T)(object)Guid.Parse(str);

		return (T)Convert.ChangeType(val, targetType);
	}
}
