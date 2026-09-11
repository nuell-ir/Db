using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class CsvCultureParityTests
{
	private static readonly string[] CulturesToTest =
	[
		"fa-IR", // Persian (Iran) - historically uses U+2212 minus sign
		"sv-SE", // Swedish (Sweden) - uses U+2212 minus sign in many ICU / .NET setups
		"fr-FR", // French - uses comma as decimal separator and space as group separator
		"de-DE", // German - uses comma as decimal separator and dot as group separator
		"ar-SA", // Arabic (Saudi Arabia) - RTL and non-standard number symbols
		"en-US", // US English
		""       // InvariantCulture
	];

	private class CultureTestModel
	{
		public int IntVal { get; set; }
		public long LongVal { get; set; }
		public short ShortVal { get; set; }
		public byte ByteVal { get; set; }
		public float FloatVal { get; set; }
		public double DoubleVal { get; set; }
		public decimal DecimalVal { get; set; }
		public DateTime PastDate { get; set; }
		public DateTimeOffset PastDateOffset { get; set; }
		public TimeSpan Duration { get; set; }
		public Guid GuidVal { get; set; }
		public byte[]? Data { get; set; }
		public bool IsActive { get; set; }
		public string? Text { get; set; }
	}

	private static DataTable CreateParityDataTable()
	{
		var dt = new DataTable();
		dt.Columns.Add("IntCol", typeof(int));
		dt.Columns.Add("LongCol", typeof(long));
		dt.Columns.Add("ShortCol", typeof(short));
		dt.Columns.Add("ByteCol", typeof(byte));
		dt.Columns.Add("FloatCol", typeof(float));
		dt.Columns.Add("DoubleCol", typeof(double));
		dt.Columns.Add("DecimalCol", typeof(decimal));
		dt.Columns.Add("DateTimeCol", typeof(DateTime));
		dt.Columns.Add("DateTimeOffsetCol", typeof(DateTimeOffset));
		dt.Columns.Add("TimeSpanCol", typeof(TimeSpan));
		dt.Columns.Add("GuidCol", typeof(Guid));
		dt.Columns.Add("BytesCol", typeof(byte[]));
		dt.Columns.Add("BoolCol", typeof(bool));
		dt.Columns.Add("StringCol", typeof(string));

		// Row 1: Negative numbers and pre-1970 dates (negative Unix timestamps)
		var preEpochDate = new DateTime(1965, 3, 14, 12, 0, 0, DateTimeKind.Utc);
		var preEpochDto = new DateTimeOffset(1950, 1, 1, 0, 0, 0, TimeSpan.Zero);
		var negativeTs = TimeSpan.FromHours(-2.5);
		var guid1 = Guid.Parse("11111111-2222-3333-4444-555555555555");
		var bytes1 = new byte[] { 1, 2, 3, 4 };

		dt.Rows.Add(
			 -42,
			 -1234567890123L,
			 (short)-32000,
			 (byte)255,
			 -3.14159f,
			 -2.718281828459,
			 -99999.99m,
			 preEpochDate,
			 preEpochDto,
			 negativeTs,
			 guid1,
			 bytes1,
			 true,
			 "Negative ~ Row | Data"
		);

		// Row 2: Extreme values (MinValue / MaxValue)
		dt.Rows.Add(
			 int.MinValue,
			 long.MinValue,
			 short.MinValue,
			 byte.MinValue,
			 float.MinValue,
			 double.MinValue,
			 decimal.MinValue,
			 new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
			 new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero),
			 TimeSpan.Zero,
			 Guid.Empty,
			 Array.Empty<byte>(),
			 false,
			 "Min Values"
		);

		// Row 3: Positive values and DBNulls
		var postEpochDate = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
		var postEpochDto = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
		var positiveTs = new TimeSpan(3, 45, 10);

		dt.Rows.Add(
			 int.MaxValue,
			 long.MaxValue,
			 short.MaxValue,
			 byte.MaxValue,
			 3.14159f,
			 2.718281828459,
			 99999.99m,
			 postEpochDate,
			 postEpochDto,
			 positiveTs,
			 guid1,
			 DBNull.Value,
			 true,
			 DBNull.Value
		);

		return dt;
	}

	[TestMethod]
	public async Task ReadCsv_StringAndStreaming_ProduceIdenticalOutput_AcrossCultures()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;

		try
		{
			// First capture the invariant baseline
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

			var baselineDt = CreateParityDataTable();
			using var baselineStringReader = baselineDt.CreateDataReader();
			string invariantExpected = await baselineStringReader.ReadCsv(null);

			Assert.IsNotNull(invariantExpected);
			Assert.IsFalse(invariantExpected.Contains('\u2212'), "Invariant string should not contain U+2212 minus sign.");
			Assert.IsTrue(invariantExpected.Contains("-42"), "Invariant string should contain ASCII '-' for -42.");

			foreach (var cultureName in CulturesToTest)
			{
				var culture = CultureInfo.GetCultureInfo(cultureName);
				CultureInfo.CurrentCulture = culture;
				CultureInfo.CurrentUICulture = culture;

				var dt = CreateParityDataTable();

				// 1. String path
				using var stringReader = dt.CreateDataReader();
				string stringOutput = await stringReader.ReadCsv(null);

				// 2. Stream path
				using var streamReader = dt.CreateDataReader();
				using var stream = new MemoryStream();
				await streamReader.ReadCsv(stream);
				string streamOutput = Encoding.UTF8.GetString(stream.ToArray());

				// Parity check between string and streaming under this culture
				Assert.AreEqual(streamOutput, stringOutput,
					 $"String and stream output differed under culture '{cultureName}'.");

				// Invariance check: output under this culture must match invariant baseline exactly
				Assert.AreEqual(invariantExpected, stringOutput,
					 $"Output under culture '{cultureName}' did not match invariant baseline.");

				// Verify negative sign is standard ASCII '-' (0x2D) and never U+2212
				Assert.IsFalse(stringOutput.Contains('\u2212'),
					 $"Output under culture '{cultureName}' contains Unicode U+2212 minus sign instead of ASCII '-'.");
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}

	[TestMethod]
	public void DbCsv_Objects_ProducesInvariantOutput_AcrossCultures()
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;

		try
		{
			var preEpochDate = new DateTime(1965, 3, 14, 12, 0, 0, DateTimeKind.Utc);
			var preEpochDto = new DateTimeOffset(1950, 1, 1, 0, 0, 0, TimeSpan.Zero);
			var guid = Guid.Parse("11111111-2222-3333-4444-555555555555");
			var bytes = new byte[] { 1, 2, 3, 4 };

			var items = new[]
			{
					 new CultureTestModel
					 {
						  IntVal = -42,
						  LongVal = -1234567890123L,
						  ShortVal = -32000,
						  ByteVal = 255,
						  FloatVal = -3.14f,
						  DoubleVal = -2.718,
						  DecimalVal = -99.99m,
						  PastDate = preEpochDate,
						  PastDateOffset = preEpochDto,
						  Duration = TimeSpan.FromHours(-1.5),
						  GuidVal = guid,
						  Data = bytes,
						  IsActive = true,
						  Text = "Sample"
					 }
				};

			// Baseline under invariant
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
			string invariantCsv = nuel.Db.Csv(items);

			Assert.IsNotNull(invariantCsv);
			Assert.IsFalse(invariantCsv.Contains('\u2212'), "Invariant CSV should not contain U+2212 minus sign.");
			Assert.IsTrue(invariantCsv.Contains("-42"), "Invariant CSV should contain ASCII '-' for -42.");
			Assert.IsTrue(invariantCsv.Contains("-1234567890123"), "Invariant CSV should contain ASCII '-' for long.");
			Assert.IsTrue(invariantCsv.Contains(new DateTimeOffset(preEpochDate).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
				 "Invariant CSV should contain pre-1970 Unix timestamp with ASCII '-'.");

			foreach (var cultureName in CulturesToTest)
			{
				var culture = CultureInfo.GetCultureInfo(cultureName);
				CultureInfo.CurrentCulture = culture;
				CultureInfo.CurrentUICulture = culture;

				string cultureCsv = nuel.Db.Csv(items);

				Assert.AreEqual(invariantCsv, cultureCsv,
					 $"Db.Csv(objects) output differed under culture '{cultureName}'.");
				Assert.IsFalse(cultureCsv.Contains('\u2212'),
					 $"Db.Csv(objects) under culture '{cultureName}' contains Unicode U+2212 minus sign.");
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}
}
