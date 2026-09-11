using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class SaveTests
{
	[TestMethod]
	public void SaveQuery_Create_JsonElement_Insert_GeneratesParameterizedQuery()
	{
		string jsonText = """
		{
			"Id": 0,
			"Name": "Alice",
			"Age": 30,
			"Price": 19.95,
			"IsActive": true,
			"Notes": null
		}
		""";

		using var doc = JsonDocument.Parse(jsonText);
		var result = SaveQuery.Create(doc.RootElement, "Users", "Id");

		Assert.AreEqual(0, result.Id);
		Assert.AreEqual("INSERT INTO Users ([Name],[Age],[Price],[IsActive],[Notes]) VALUES (@Name,@Age,@Price,@IsActive,@Notes); SELECT CAST(SCOPE_IDENTITY() AS int);", result.Query);
		Assert.AreEqual(5, result.SqlParams.Length);

		Assert.AreEqual("@Name", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Alice", result.SqlParams[0].Value);

		Assert.AreEqual("@Age", result.SqlParams[1].ParameterName);
		Assert.AreEqual(30, result.SqlParams[1].Value);

		Assert.AreEqual("@Price", result.SqlParams[2].ParameterName);
		Assert.AreEqual(19.95m, result.SqlParams[2].Value);

		Assert.AreEqual("@IsActive", result.SqlParams[3].ParameterName);
		Assert.AreEqual(true, result.SqlParams[3].Value);

		Assert.AreEqual("@Notes", result.SqlParams[4].ParameterName);
		Assert.AreEqual(DBNull.Value, result.SqlParams[4].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_JsonElement_Update_GeneratesParameterizedQuery()
	{
		string jsonText = """
		{
			"Id": 42,
			"Name": "Bob",
			"Age": 25,
			"IsActive": false
		}
		""";

		using var doc = JsonDocument.Parse(jsonText);
		var result = SaveQuery.Create(doc.RootElement, "Users", "Id");

		Assert.AreEqual(42, result.Id);
		Assert.AreEqual("UPDATE Users SET [Name]=@Name,[Age]=@Age,[IsActive]=@IsActive WHERE [Id]=@Id", result.Query);
		Assert.AreEqual(4, result.SqlParams.Length);

		Assert.AreEqual("@Name", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Bob", result.SqlParams[0].Value);

		Assert.AreEqual("@Age", result.SqlParams[1].ParameterName);
		Assert.AreEqual(25, result.SqlParams[1].Value);

		Assert.AreEqual("@IsActive", result.SqlParams[2].ParameterName);
		Assert.AreEqual(false, result.SqlParams[2].Value);

		Assert.AreEqual("@Id", result.SqlParams[3].ParameterName);
		Assert.AreEqual(42, result.SqlParams[3].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_JsonObject_Insert_GeneratesParameterizedQuery()
	{
		var obj = new JsonObject
		{
			["Id"] = 0,
			["Title"] = "Book",
			["Qty"] = 10,
			["Price"] = 9.99,
			["Available"] = true,
			["Description"] = null
		};

		var result = SaveQuery.Create(obj, "Products", "Id");

		Assert.AreEqual(0, result.Id);
		Assert.AreEqual("INSERT INTO Products ([Title],[Qty],[Price],[Available],[Description]) VALUES (@Title,@Qty,@Price,@Available,@Description); SELECT CAST(SCOPE_IDENTITY() AS int);", result.Query);
		Assert.AreEqual(5, result.SqlParams.Length);

		Assert.AreEqual("@Title", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Book", result.SqlParams[0].Value);

		Assert.AreEqual("@Qty", result.SqlParams[1].ParameterName);
		Assert.AreEqual(10, result.SqlParams[1].Value);

		Assert.AreEqual("@Available", result.SqlParams[3].ParameterName);
		Assert.AreEqual(true, result.SqlParams[3].Value);

		Assert.AreEqual("@Description", result.SqlParams[4].ParameterName);
		Assert.AreEqual(DBNull.Value, result.SqlParams[4].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_JsonObject_Update_GeneratesParameterizedQuery()
	{
		var obj = new JsonObject
		{
			["Id"] = 101,
			["Title"] = "Updated Book",
			["Available"] = false
		};

		var result = SaveQuery.Create(obj, "Products", "Id");

		Assert.AreEqual(101, result.Id);
		Assert.AreEqual("UPDATE Products SET [Title]=@Title,[Available]=@Available WHERE [Id]=@Id", result.Query);
		Assert.AreEqual(3, result.SqlParams.Length);

		Assert.AreEqual("@Title", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Updated Book", result.SqlParams[0].Value);

		Assert.AreEqual("@Available", result.SqlParams[1].ParameterName);
		Assert.AreEqual(false, result.SqlParams[1].Value);

		Assert.AreEqual("@Id", result.SqlParams[2].ParameterName);
		Assert.AreEqual(101, result.SqlParams[2].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_StringifiedId_HandledProperly()
	{
		string jsonText = """
		{
			"Id": "99",
			"Name": "Test"
		}
		""";

		using var doc = JsonDocument.Parse(jsonText);
		var result = SaveQuery.Create(doc.RootElement, "Users", "Id");

		Assert.AreEqual(99, result.Id);
		Assert.AreEqual("UPDATE Users SET [Name]=@Name WHERE [Id]=@Id", result.Query);
		Assert.AreEqual(2, result.SqlParams.Length);
		Assert.AreEqual("@Id", result.SqlParams[1].ParameterName);
		Assert.AreEqual(99, result.SqlParams[1].Value);
	}

	private enum UserRole
	{
		User = 1,
		Admin = 2
	}

	private class UserModel
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public int Age { get; set; }
		public decimal Price { get; set; }
		public bool IsActive { get; set; }
		public string? Notes { get; set; }
	}

	private class AdvancedUserModel
	{
		public int UserId { get; set; }
		public string? Name { get; set; }
		public UserRole Role { get; set; }
		public int? NullableCount { get; set; }
	}

	[TestMethod]
	public void SaveQuery_Create_Object_Insert_GeneratesParameterizedQuery()
	{
		var user = new UserModel
		{
			Id = 0,
			Name = "Alice",
			Age = 30,
			Price = 19.95m,
			IsActive = true,
			Notes = null
		};

		var result = SaveQuery.Create(user, "Users", "Id");

		Assert.AreEqual(0, result.Id);
		Assert.AreEqual("INSERT INTO Users ([Name],[Age],[Price],[IsActive],[Notes]) VALUES (@Name,@Age,@Price,@IsActive,@Notes); SELECT CAST(SCOPE_IDENTITY() AS int);", result.Query);
		Assert.AreEqual(5, result.SqlParams.Length);

		Assert.AreEqual("@Name", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Alice", result.SqlParams[0].Value);

		Assert.AreEqual("@Age", result.SqlParams[1].ParameterName);
		Assert.AreEqual(30, result.SqlParams[1].Value);

		Assert.AreEqual("@Price", result.SqlParams[2].ParameterName);
		Assert.AreEqual(19.95m, result.SqlParams[2].Value);

		Assert.AreEqual("@IsActive", result.SqlParams[3].ParameterName);
		Assert.AreEqual(true, result.SqlParams[3].Value);

		Assert.AreEqual("@Notes", result.SqlParams[4].ParameterName);
		Assert.AreEqual(DBNull.Value, result.SqlParams[4].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_Object_Update_GeneratesParameterizedQuery()
	{
		var user = new UserModel
		{
			Id = 42,
			Name = "Bob",
			Age = 25,
			Price = 50.0m,
			IsActive = false,
			Notes = "Updated"
		};

		var result = SaveQuery.Create(user, "Users", "Id");

		Assert.AreEqual(42, result.Id);
		Assert.AreEqual("UPDATE Users SET [Name]=@Name,[Age]=@Age,[Price]=@Price,[IsActive]=@IsActive,[Notes]=@Notes WHERE [Id]=@Id", result.Query);
		Assert.AreEqual(6, result.SqlParams.Length);

		Assert.AreEqual("@Name", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Bob", result.SqlParams[0].Value);

		Assert.AreEqual("@Age", result.SqlParams[1].ParameterName);
		Assert.AreEqual(25, result.SqlParams[1].Value);

		Assert.AreEqual("@Price", result.SqlParams[2].ParameterName);
		Assert.AreEqual(50.0m, result.SqlParams[2].Value);

		Assert.AreEqual("@IsActive", result.SqlParams[3].ParameterName);
		Assert.AreEqual(false, result.SqlParams[3].Value);

		Assert.AreEqual("@Notes", result.SqlParams[4].ParameterName);
		Assert.AreEqual("Updated", result.SqlParams[4].Value);

		Assert.AreEqual("@Id", result.SqlParams[5].ParameterName);
		Assert.AreEqual(42, result.SqlParams[5].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_Object_CaseInsensitiveIdProp_CustomIdName_AndEnums()
	{
		var user = new AdvancedUserModel
		{
			UserId = 10,
			Name = "Charlie",
			Role = UserRole.Admin,
			NullableCount = null
		};

		// "userid" lowercase testing case-insensitivity
		var result = SaveQuery.Create(user, "AdvancedUsers", "userid");

		Assert.AreEqual(10, result.Id);
		Assert.AreEqual("UPDATE AdvancedUsers SET [Name]=@Name,[Role]=@Role,[NullableCount]=@NullableCount WHERE [userid]=@userid", result.Query);
		Assert.AreEqual(4, result.SqlParams.Length);

		Assert.AreEqual("@Name", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Charlie", result.SqlParams[0].Value);

		Assert.AreEqual("@Role", result.SqlParams[1].ParameterName);
		Assert.AreEqual((int)UserRole.Admin, result.SqlParams[1].Value);

		Assert.AreEqual("@NullableCount", result.SqlParams[2].ParameterName);
		Assert.AreEqual(DBNull.Value, result.SqlParams[2].Value);

		Assert.AreEqual("@userid", result.SqlParams[3].ParameterName);
		Assert.AreEqual(10, result.SqlParams[3].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_Object_AnonymousType_And_StringId()
	{
		var anon = new
		{
			Id = "77",
			Title = "Report",
			Score = 98.5
		};

		var result = SaveQuery.Create(anon, "Reports", "Id");

		Assert.AreEqual(77, result.Id);
		Assert.AreEqual("UPDATE Reports SET [Title]=@Title,[Score]=@Score WHERE [Id]=@Id", result.Query);
		Assert.AreEqual(3, result.SqlParams.Length);

		Assert.AreEqual("@Title", result.SqlParams[0].ParameterName);
		Assert.AreEqual("Report", result.SqlParams[0].Value);

		Assert.AreEqual("@Score", result.SqlParams[1].ParameterName);
		Assert.AreEqual(98.5, result.SqlParams[1].Value);

		Assert.AreEqual("@Id", result.SqlParams[2].ParameterName);
		Assert.AreEqual(77, result.SqlParams[2].Value);
	}

	[TestMethod]
	public void SaveQuery_Create_Object_CachesMetadata_ReturnsConsistentResults()
	{
		var user1 = new UserModel { Id = 0, Name = "First" };
		var user2 = new UserModel { Id = 1, Name = "Second" };

		var result1 = SaveQuery.Create(user1, "Users", "Id");
		var result2 = SaveQuery.Create(user2, "Users", "Id");

		Assert.AreEqual(0, result1.Id);
		Assert.AreEqual(1, result2.Id);
		Assert.AreEqual("First", result1.SqlParams[0].Value);
		Assert.AreEqual("Second", result2.SqlParams[0].Value);
	}
}

