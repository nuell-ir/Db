using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class SaveAllTests
{
	[TestMethod]
	[DataRow("{}")]
	[DataRow("\"test\"")]
	[DataRow("123")]
	[DataRow("true")]
	[DataRow("null")]
	public void SaveAllQuery_Create_NonArrayJson_ThrowsArgumentException(string jsonText)
	{
		using var doc = JsonDocument.Parse(jsonText);
		var ex = Assert.ThrowsExactly<ArgumentException>(() =>
			 SaveAllQuery.Create(doc.RootElement, null, "MyTable", "Id"));

		Assert.AreEqual("json", ex.ParamName);
	}

	[TestMethod]
	public void SaveAllQuery_Create_UndefinedJson_ThrowsArgumentException()
	{
		var ex = Assert.ThrowsExactly<ArgumentException>(() =>
			 SaveAllQuery.Create(default(JsonElement), null, "MyTable", "Id"));

		Assert.AreEqual("json", ex.ParamName);
	}

	[TestMethod]
	public void SaveAllQuery_Create_EmptyArrayWithDeleteIds_GeneratesDeleteQuery()
	{
		using var doc = JsonDocument.Parse("[]");
		var query = SaveAllQuery.Create(doc.RootElement, "1,2,3", "MyTable", "Id");

		StringAssert.Contains(query, "DELETE FROM MyTable WHERE [Id] in (1,2,3);");
	}

	[TestMethod]
	public void SaveAllQuery_Create_InvalidDeleteIds_ThrowsInvalidDataException()
	{
		using var doc = JsonDocument.Parse("[]");
		Assert.ThrowsExactly<InvalidDataException>(() =>
			 SaveAllQuery.Create(doc.RootElement, "1,2,abc", "MyTable", "Id"));
	}

	[TestMethod]
	public void SaveAllQuery_Create_JsonElement_MixedInsertAndUpdate_GeneratesCorrectBatchSql()
	{
		string jsonText = """
		[
			{ "Id": 0, "Name": "Alice", "Age": 30, "Price": 19.95, "Active": true, "Notes": null },
			{ "Id": 42, "Name": "Bob", "Age": 25, "Price": 9.99, "Active": false, "Notes": "existing" },
			{ "Id": 0, "Name": "Charlie", "Age": 35, "Price": 50, "Active": true, "Notes": "new" }
		]
		""";

		using var doc = JsonDocument.Parse(jsonText);
		var query = SaveAllQuery.Create(doc.RootElement, "10,20", "Users", "Id");

		StringAssert.StartsWith(query, "SET XACT_ABORT ON; BEGIN TRY BEGIN TRAN;");
		StringAssert.Contains(query, "DELETE FROM Users WHERE [Id] in (10,20);");
		StringAssert.Contains(query, "UPDATE Users SET [Name]=N'Bob',[Age]=25,[Price]=9.99,[Active]=0,[Notes]=N'existing' WHERE [Id]=42;");
		StringAssert.Contains(query, "INSERT INTO Users ([Name],[Age],[Price],[Active],[Notes]) VALUES (N'Alice',30,19.95,1,NULL),(N'Charlie',35,50,1,N'new');");
		StringAssert.EndsWith(query, "COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;");
	}

	[TestMethod]
	public void SaveAllQuery_Create_JsonElement_StringifiedId_HandledAsUpdate()
	{
		string jsonText = """
		[
			{ "Id": "99", "Name": "Test" }
		]
		""";

		using var doc = JsonDocument.Parse(jsonText);
		var query = SaveAllQuery.Create(doc.RootElement, null, "Users", "Id");

		StringAssert.Contains(query, "UPDATE Users SET [Name]=N'Test' WHERE [Id]=99;");
	}

	[TestMethod]
	public void SaveAllQuery_Create_JsonElement_EscapesSingleQuotesInStrings()
	{
		string jsonText = """
		[
			{ "Id": 1, "Name": "O'Connor" },
			{ "Id": 0, "Name": "D'Angelo" }
		]
		""";

		using var doc = JsonDocument.Parse(jsonText);
		var query = SaveAllQuery.Create(doc.RootElement, null, "Artists", "Id");

		StringAssert.Contains(query, "UPDATE Artists SET [Name]=N'O''Connor' WHERE [Id]=1;");
		StringAssert.Contains(query, "INSERT INTO Artists ([Name]) VALUES (N'D''Angelo');");
	}

	[TestMethod]
	public void SaveAllQuery_Create_JsonElement_CultureInvariantDecimalFormatting()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			// Culture that uses comma as decimal separator
			CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

			string jsonText = """
			[
				{ "Id": 1, "Price": 12.34 },
				{ "Id": 0, "Price": 56.78 }
			]
			""";

			using var doc = JsonDocument.Parse(jsonText);
			var query = SaveAllQuery.Create(doc.RootElement, null, "Products", "Id");

			StringAssert.Contains(query, "UPDATE Products SET [Price]=12.34 WHERE [Id]=1;");
			StringAssert.Contains(query, "INSERT INTO Products ([Price]) VALUES (56.78);");
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	[TestMethod]
	public void SaveAllQuery_Create_JsonArray_GeneratesCorrectBatchSql()
	{
		var array = new JsonArray
		{
			new JsonObject { ["Id"] = 0, ["Title"] = "New Item", ["Qty"] = 5, ["Price"] = 10.50m, ["Available"] = true, ["Extra"] = null },
			new JsonObject { ["Id"] = 7, ["Title"] = "Updated Item", ["Qty"] = 12, ["Price"] = 25.00m, ["Available"] = false, ["Extra"] = "note" }
		};

		var query = SaveAllQuery.Create(array, "3,4", "Items", "Id");

		StringAssert.StartsWith(query, "SET XACT_ABORT ON; BEGIN TRY BEGIN TRAN;");
		StringAssert.Contains(query, "DELETE FROM Items WHERE [Id] in (3,4);");
		StringAssert.Contains(query, "UPDATE Items SET [Title]=N'Updated Item',[Qty]=12,[Price]=25.00,[Available]=0,[Extra]=N'note' WHERE [Id]=7;");
		StringAssert.Contains(query, "INSERT INTO Items ([Title],[Qty],[Price],[Available],[Extra]) VALUES (N'New Item',5,10.50,1,NULL);");
		StringAssert.EndsWith(query, "COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;");
	}

	[TestMethod]
	public void SaveAllQuery_Create_JsonArray_EscapesQuotesAndHandlesCulture()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

			var array = new JsonArray
			{
				new JsonObject { ["Id"] = 1, ["Name"] = "O'Brien", ["Value"] = 99.99m }
			};

			var query = SaveAllQuery.Create(array, null, "Entities", "Id");
			StringAssert.Contains(query, "UPDATE Entities SET [Name]=N'O''Brien',[Value]=99.99 WHERE [Id]=1;");
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	[TestMethod]
	public void SaveAllQuery_Create_JsonNode_NonArray_ThrowsArgumentException()
	{
		JsonNode node = JsonNode.Parse("{\"key\": \"val\"}")!;
		var ex = Assert.ThrowsExactly<ArgumentException>(() =>
			 SaveAllQuery.Create(node, null, "Table", "Id"));

		Assert.AreEqual("json", ex.ParamName);
	}

	[TestMethod]
	public void SaveAllQuery_Create_InsertChunking_GeneratesSeparateInsertsPer1000Rows()
	{
		// Create an array with 1005 items with Id = 0
		var array = new JsonArray();
		for (int i = 1; i <= 1005; i++)
		{
			array.Add(new JsonObject { ["Id"] = 0, ["Num"] = i });
		}

		var query = SaveAllQuery.Create(array, null, "LotsOfRows", "Id");

		// Should have two INSERT statements
		int insertCount = 0;
		int index = 0;
		while ((index = query.IndexOf("INSERT INTO LotsOfRows ([Num]) VALUES ", index)) != -1)
		{
			insertCount++;
			index += "INSERT INTO LotsOfRows ([Num]) VALUES ".Length;
		}

		Assert.AreEqual(2, insertCount);
	}
}
