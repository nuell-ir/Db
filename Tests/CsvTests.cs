using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class CsvTests
{
	private class AllTypesModel
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public Guid UniqueId { get; set; }
		public DateTimeOffset Timestamp { get; set; }
		public TimeSpan Duration { get; set; }
		public byte[]? Data { get; set; }
		public float Rate { get; set; }
		public bool IsActive { get; set; }
		public DateTime Created { get; set; }
		public int? NullableInt { get; set; }
	}

	private class UnsupportedTypeModel
	{
		public int Id { get; set; }
		public object? BadField { get; set; }
	}

	[TestMethod]
	public void GetCsvTypeFlag_SupportedTypes_ReturnsExpectedMarkers()
	{
		Assert.AreEqual('!', CsvWriter.GetCsvTypeFlag(typeof(int)));
		Assert.AreEqual('!', CsvWriter.GetCsvTypeFlag(typeof(long)));
		Assert.AreEqual('!', CsvWriter.GetCsvTypeFlag(typeof(short)));
		Assert.AreEqual('!', CsvWriter.GetCsvTypeFlag(typeof(byte)));
		Assert.AreEqual('%', CsvWriter.GetCsvTypeFlag(typeof(float)));
		Assert.AreEqual('%', CsvWriter.GetCsvTypeFlag(typeof(double)));
		Assert.AreEqual('%', CsvWriter.GetCsvTypeFlag(typeof(decimal)));
		Assert.AreEqual('#', CsvWriter.GetCsvTypeFlag(typeof(DateTime)));
		Assert.AreEqual('#', CsvWriter.GetCsvTypeFlag(typeof(DateTimeOffset)));
		Assert.AreEqual('^', CsvWriter.GetCsvTypeFlag(typeof(bool)));
		Assert.AreEqual('$', CsvWriter.GetCsvTypeFlag(typeof(string)));
		Assert.AreEqual('$', CsvWriter.GetCsvTypeFlag(typeof(char)));
		Assert.AreEqual('$', CsvWriter.GetCsvTypeFlag(typeof(Guid)));
		Assert.AreEqual('$', CsvWriter.GetCsvTypeFlag(typeof(TimeSpan)));
		Assert.AreEqual('$', CsvWriter.GetCsvTypeFlag(typeof(byte[])));

		// Nullable variants
		Assert.AreEqual('!', CsvWriter.GetCsvTypeFlag(typeof(int?)));
		Assert.AreEqual('#', CsvWriter.GetCsvTypeFlag(typeof(DateTimeOffset?)));
		Assert.AreEqual('$', CsvWriter.GetCsvTypeFlag(typeof(Guid?)));
	}

	[TestMethod]
	public void GetCsvTypeFlag_UnsupportedType_ThrowsNotSupportedException()
	{
		Assert.ThrowsExactly<NotSupportedException>(() => CsvWriter.GetCsvTypeFlag(typeof(object)));
		Assert.ThrowsExactly<NotSupportedException>(() => CsvWriter.GetCsvTypeFlag(typeof(Exception)));
	}

	[TestMethod]
	public void Csv_Objects_SerializesGuidDateTimeOffsetTimeSpanByteArrayAndNulls()
	{
		var guid = Guid.Parse("11111111-2222-3333-4444-555555555555");
		var dto = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
		var ts = new TimeSpan(1, 30, 0);
		var bytes = new byte[] { 1, 2, 3, 4 };
		var created = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

		var items = new[]
		{
			new AllTypesModel
			{
				Id = 1,
				Name = "Item 1",
				UniqueId = guid,
				Timestamp = dto,
				Duration = ts,
				Data = bytes,
				Rate = 3.14f,
				IsActive = true,
				Created = created,
				NullableInt = null
			},
			new AllTypesModel
			{
				Id = 2,
				Name = null, // null string -> Ø
				UniqueId = Guid.Empty,
				Timestamp = dto,
				Duration = TimeSpan.Zero,
				Data = null, // null byte[] -> Ø
				Rate = 0.5f,
				IsActive = false,
				Created = created,
				NullableInt = 42
			}
		};

		var csv = nuel.Db.Csv(items);
		Assert.IsNotNull(csv);

		// Header should contain expected flags
		Assert.IsTrue(csv.Contains("!Id"));
		Assert.IsTrue(csv.Contains("$Name"));
		Assert.IsTrue(csv.Contains("$UniqueId"));
		Assert.IsTrue(csv.Contains("#Timestamp"));
		Assert.IsTrue(csv.Contains("$Duration"));
		Assert.IsTrue(csv.Contains("$Data"));
		Assert.IsTrue(csv.Contains("%Rate"));
		Assert.IsTrue(csv.Contains("^IsActive"));
		Assert.IsTrue(csv.Contains("#Created"));
		Assert.IsTrue(csv.Contains("!NullableInt"));

		// Row 1 checks
		Assert.IsTrue(csv.Contains("11111111-2222-3333-4444-555555555555"));
		Assert.IsTrue(csv.Contains("01:30:00"));
		Assert.IsTrue(csv.Contains(Convert.ToBase64String(bytes)));
		Assert.IsTrue(csv.Contains(dto.ToUnixTimeSeconds().ToString()));

		// Row 2 checks (nulls rendered as Ø)
		var rows = csv.Split('|');
		Assert.AreEqual(3, rows.Length); // Header + 2 rows
		var row2 = rows[2];
		Assert.IsTrue(row2.Contains("Ø")); // Should contain null character for Name and Data
	}

	[TestMethod]
	public void Csv_Objects_UnsupportedType_ThrowsNotSupportedException()
	{
		var items = new[]
		{
				new UnsupportedTypeModel { Id = 1, BadField = new object() }
		  };

		Assert.ThrowsExactly<NotSupportedException>(() => nuel.Db.Csv(items));
	}
}
