using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

public enum TestStatus
{
	Inactive = 0,
	Active = 1,
	Suspended = 2
}

[TestClass]
public class DictionaryTests
{
	[TestMethod]
	public async Task Dictionary_NullQuery_ThrowsArgumentNullException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await nuel.Db.Dictionary<int, string>(null!));
	}

	[TestMethod]
	public async Task Dictionary_WhitespaceQuery_ThrowsArgumentException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
			await nuel.Db.Dictionary<int, string>("   "));
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_EmptyTable_ReturnsNull()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<int, string>(reader);

		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_SingleColumn_ReturnsNull()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Rows.Add(1);

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<int, string>(reader);

		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_ValidRows_MapsCorrectly()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Rows.Add(1, "Alice");
		dt.Rows.Add(2, "Bob");

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<int, string>(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("Alice", result[1]);
		Assert.AreEqual("Bob", result[2]);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_NullValue_MapsToDefaultWithoutThrowing()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Rows.Add(1, DBNull.Value);
		dt.Rows.Add(2, "Bob");

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<int, string?>(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(2, result.Count);
		Assert.IsNull(result[1]);
		Assert.AreEqual("Bob", result[2]);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_NullKey_SkipsRowSafely()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Rows.Add(DBNull.Value, "NoKey");
		dt.Rows.Add(1, "HasKey");

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<int, string>(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("HasKey", result[1]);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_TypeConversion_ConvertsKeysAndValues()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int)); // Int32 in DB
		dt.Columns.Add("Amount", typeof(decimal)); // Decimal in DB
		dt.Rows.Add(100, 25.50m);

		using var reader = dt.CreateDataReader();
		// Mapping to long key and double value
		var result = await nuel.Db.ReadDictionaryAsync<long, double>(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(1, result.Count);
		Assert.IsTrue(result.ContainsKey(100L));
		Assert.AreEqual(25.50, result[100L], 0.001);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_EnumConversion_ConvertsEnumValue()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Status", typeof(int));
		dt.Rows.Add(1, 1); // Active
		dt.Rows.Add(2, 2); // Suspended

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<int, TestStatus>(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual(TestStatus.Active, result[1]);
		Assert.AreEqual(TestStatus.Suspended, result[2]);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_GuidConversion_ConvertsStringValueToGuid()
	{
		var guid = Guid.NewGuid();
		using var dt = new DataTable();
		dt.Columns.Add("Name", typeof(string));
		dt.Columns.Add("GuidStr", typeof(string));
		dt.Rows.Add("Item1", guid.ToString());

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<string, Guid>(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(1, result.Count);
		Assert.AreEqual(guid, result["Item1"]);
	}

	[TestMethod]
	public async Task ReadDictionaryAsync_DuplicateKeys_UpdatesLastValueWithoutThrowing()
	{
		using var dt = new DataTable();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		dt.Rows.Add(1, "First");
		dt.Rows.Add(1, "Second");

		using var reader = dt.CreateDataReader();
		var result = await nuel.Db.ReadDictionaryAsync<int, string>(reader);

		Assert.IsNotNull(result);
		Assert.AreEqual(1, result.Count);
		Assert.AreEqual("Second", result[1]);
	}
}

