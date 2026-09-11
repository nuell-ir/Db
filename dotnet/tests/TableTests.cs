using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class TableTests
{
	[TestMethod]
	public async Task Table_NullQuery_ThrowsArgumentNullException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await nuel.Db.Table(null!));
	}

	[TestMethod]
	public async Task Table_WhitespaceQuery_ThrowsArgumentException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
			await nuel.Db.Table("   "));
	}

	[TestMethod]
	public async Task ReadTableAsync_EmptyReader_ReturnsNull()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadTableAsync(reader);

		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task ReadTableAsync_ValidRows_PopulatesDataTableCorrectly()
	{
		using var sourceDt = new DataTable();
		sourceDt.Columns.Add("Id", typeof(int));
		sourceDt.Columns.Add("Name", typeof(string));
		sourceDt.Columns.Add("Price", typeof(decimal));
		sourceDt.Columns.Add("Created", typeof(DateTime));
		sourceDt.Columns.Add("IsActive", typeof(bool));

		var now = new DateTime(2026, 9, 11, 12, 0, 0);
		sourceDt.Rows.Add(1, "Item1", 19.99m, now, true);
		sourceDt.Rows.Add(2, "Item2", 49.50m, now.AddDays(1), false);
		sourceDt.Rows.Add(3, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

		using var reader = sourceDt.CreateDataReader();
		var result = await nuel.Db.ReadTableAsync(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(5, result.Columns.Count);
		Assert.AreEqual(3, result.Rows.Count);

		Assert.AreEqual("Id", result.Columns[0].ColumnName);
		Assert.AreEqual(typeof(int), result.Columns[0].DataType);
		Assert.AreEqual("Name", result.Columns[1].ColumnName);
		Assert.AreEqual(typeof(string), result.Columns[1].DataType);
		Assert.AreEqual("Price", result.Columns[2].ColumnName);
		Assert.AreEqual(typeof(decimal), result.Columns[2].DataType);
		Assert.AreEqual("Created", result.Columns[3].ColumnName);
		Assert.AreEqual(typeof(DateTime), result.Columns[3].DataType);
		Assert.AreEqual("IsActive", result.Columns[4].ColumnName);
		Assert.AreEqual(typeof(bool), result.Columns[4].DataType);

		Assert.AreEqual(1, result.Rows[0]["Id"]);
		Assert.AreEqual("Item1", result.Rows[0]["Name"]);
		Assert.AreEqual(19.99m, result.Rows[0]["Price"]);
		Assert.AreEqual(now, result.Rows[0]["Created"]);
		Assert.AreEqual(true, result.Rows[0]["IsActive"]);

		Assert.AreEqual(2, result.Rows[1]["Id"]);
		Assert.AreEqual("Item2", result.Rows[1]["Name"]);
		Assert.AreEqual(49.50m, result.Rows[1]["Price"]);
		Assert.AreEqual(false, result.Rows[1]["IsActive"]);

		Assert.AreEqual(3, result.Rows[2]["Id"]);
		Assert.AreEqual(DBNull.Value, result.Rows[2]["Name"]);
		Assert.AreEqual(DBNull.Value, result.Rows[2]["Price"]);
		Assert.AreEqual(DBNull.Value, result.Rows[2]["Created"]);
		Assert.AreEqual(DBNull.Value, result.Rows[2]["IsActive"]);
	}

	[TestMethod]
	public async Task ReadTableAsync_DuplicateColumnNames_DisambiguatesGracefully()
	{
		using var sourceDt = new DataTable();
		sourceDt.Columns.Add("Id", typeof(int));
		sourceDt.Columns.Add("Id_Original", typeof(int));
		sourceDt.Rows.Add(10, 20);

		using var reader = new DuplicateColumnDataReader(
			new[] { "Id", "Id", "Id" },
			new[] { typeof(int), typeof(int), typeof(int) },
			new object[][] { new object[] { 1, 2, 3 } }
		);

		var result = await nuel.Db.ReadTableAsync(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(3, result.Columns.Count);
		Assert.AreEqual("Id", result.Columns[0].ColumnName);
		Assert.AreEqual("Id_1", result.Columns[1].ColumnName);
		Assert.AreEqual("Id_2", result.Columns[2].ColumnName);

		Assert.AreEqual(1, result.Rows[0][0]);
		Assert.AreEqual(2, result.Rows[0][1]);
		Assert.AreEqual(3, result.Rows[0][2]);
	}

	[TestMethod]
	public async Task ReadTableAsync_UnnamedColumns_AssignsDefaultColumnNames()
	{
		using var reader = new DuplicateColumnDataReader(
			new[] { "", "" },
			new[] { typeof(int), typeof(string) },
			new object[][] { new object[] { 42, "hello" } }
		);

		var result = await nuel.Db.ReadTableAsync(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(2, result.Columns.Count);
		Assert.AreEqual("Column1", result.Columns[0].ColumnName);
		Assert.AreEqual("Column2", result.Columns[1].ColumnName);

		Assert.AreEqual(42, result.Rows[0][0]);
		Assert.AreEqual("hello", result.Rows[0][1]);
	}

	private sealed class DuplicateColumnDataReader : System.Data.Common.DbDataReader
	{
		private readonly string[] _names;
		private readonly Type[] _types;
		private readonly object[][] _rows;
		private int _currentRow = -1;

		public DuplicateColumnDataReader(string[] names, Type[] types, object[][] rows)
		{
			_names = names;
			_types = types;
			_rows = rows;
		}

		public override int FieldCount => _names.Length;
		public override bool HasRows => _rows.Length > 0;
		public override bool IsClosed => false;
		public override int RecordsAffected => -1;
		public override int Depth => 0;

		public override string GetName(int ordinal) => _names[ordinal];
		public override Type GetFieldType(int ordinal) => _types[ordinal];
		public override string GetDataTypeName(int ordinal) => _types[ordinal].Name;
		public override object GetValue(int ordinal) => _rows[_currentRow][ordinal];
		public override int GetValues(object[] values)
		{
			int count = Math.Min(values.Length, _names.Length);
			for (int i = 0; i < count; i++)
				values[i] = _rows[_currentRow][i];
			return count;
		}

		public override bool Read()
		{
			_currentRow++;
			return _currentRow < _rows.Length;
		}

		public override Task<bool> ReadAsync(CancellationToken cancellationToken)
			=> Task.FromResult(Read());

		public override bool NextResult() => false;
		public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
			=> Task.FromResult(false);

		public override bool IsDBNull(int ordinal) => _rows[_currentRow][ordinal] == DBNull.Value || _rows[_currentRow][ordinal] == null;
		public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
		public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
		public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
		public override char GetChar(int ordinal) => (char)GetValue(ordinal);
		public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
		public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);
		public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);
		public override double GetDouble(int ordinal) => (double)GetValue(ordinal);
		public override float GetFloat(int ordinal) => (float)GetValue(ordinal);
		public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
		public override short GetInt16(int ordinal) => (short)GetValue(ordinal);
		public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
		public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
		public override string GetString(int ordinal) => (string)GetValue(ordinal);
		public override object this[int ordinal] => GetValue(ordinal);
		public override object this[string name] => GetValue(GetOrdinal(name));
		public override int GetOrdinal(string name) => Array.IndexOf(_names, name);
		public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
	}
}
