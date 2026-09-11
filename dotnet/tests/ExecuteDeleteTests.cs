using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nuel;

namespace Db.Tests;

[TestClass]
public class ExecuteDeleteTests
{
	[TestMethod]
	public void EscapeIdentifier_SimpleName_WrapsInBrackets()
	{
		Assert.AreEqual("[Users]", Data.EscapeIdentifier("Users"));
	}

	[TestMethod]
	public void EscapeIdentifier_SchemaQualifiedName_WrapsEachPart()
	{
		Assert.AreEqual("[dbo].[Users]", Data.EscapeIdentifier("dbo.Users"));
		Assert.AreEqual("[Catalog].[dbo].[Users]", Data.EscapeIdentifier("Catalog.dbo.Users"));
	}

	[TestMethod]
	public void EscapeIdentifier_AlreadyBracketed_PreservesBracketsWithoutDoubling()
	{
		Assert.AreEqual("[Users]", Data.EscapeIdentifier("[Users]"));
		Assert.AreEqual("[dbo].[Users]", Data.EscapeIdentifier("[dbo].[Users]"));
	}

	[TestMethod]
	public void EscapeIdentifier_ClosingBracketInName_EscapesBracket()
	{
		Assert.AreEqual("[User]]Name]", Data.EscapeIdentifier("User]Name"));
	}

	[TestMethod]
	public void EscapeIdentifier_InjectionAttempt_NeutralizesWithinIdentifier()
	{
		Assert.AreEqual("[Users; DROP TABLE Orders; --]", Data.EscapeIdentifier("Users; DROP TABLE Orders; --"));
		Assert.AreEqual("[Id]] = 1 OR 1=1; --]", Data.EscapeIdentifier("Id] = 1 OR 1=1; --"));
	}

	[TestMethod]
	public void EscapeIdentifier_Null_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => Data.EscapeIdentifier(null!));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("   ")]
	public void EscapeIdentifier_Whitespace_ThrowsArgumentException(string invalidIdentifier)
	{
		Assert.ThrowsExactly<ArgumentException>(() => Data.EscapeIdentifier(invalidIdentifier));
	}

	[TestMethod]
	public void AttachParams_Tuples_AttachesCorrectly()
	{
		using var cmd = new SqlCommand();
		(string name, object? value)[] parameters = [("@id", 123), ("@name", "Alice"), ("@nullVal", null)];

		Data.AttachParams(cmd, parameters!);

		Assert.AreEqual(3, cmd.Parameters.Count);
		Assert.AreEqual("@id", cmd.Parameters[0].ParameterName);
		Assert.AreEqual(123, cmd.Parameters[0].Value);
		Assert.AreEqual("@name", cmd.Parameters[1].ParameterName);
		Assert.AreEqual("Alice", cmd.Parameters[1].Value);
		Assert.AreEqual("@nullVal", cmd.Parameters[2].ParameterName);
		Assert.AreEqual(DBNull.Value, cmd.Parameters[2].Value);
	}

	[TestMethod]
	public void AttachParams_SqlParameters_ReusedAcrossCommands_ClonesSafely()
	{
		using var cmd1 = new SqlCommand();
		using var cmd2 = new SqlCommand();

		var param = new SqlParameter("@shared", 42);
		SqlParameter?[] parameters = [param, null];

		// Attach to first command
		Data.AttachParams(cmd1, parameters!);
		Assert.AreEqual(1, cmd1.Parameters.Count);
		Assert.AreEqual(42, cmd1.Parameters[0].Value);

		// Attach to second command - should clone safely without throwing ArgumentException
		Data.AttachParams(cmd2, parameters!);
		Assert.AreEqual(1, cmd2.Parameters.Count);
		Assert.AreEqual(42, cmd2.Parameters[0].Value);
		Assert.AreNotSame(cmd1.Parameters[0], cmd2.Parameters[0]);
	}

	[TestMethod]
	public async Task Execute_NullQuery_ThrowsArgumentNullException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await nuel.Db.Execute(null!));
	}

	[TestMethod]
	public async Task Execute_WhitespaceQuery_ThrowsArgumentException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
			await nuel.Db.Execute("   "));
	}

	[TestMethod]
	public async Task Delete_NullId_ThrowsArgumentNullException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await nuel.Db.Delete<string>(null!, "Users"));
	}

	[TestMethod]
	public async Task Delete_InvalidTableOrPk_ThrowsArgumentException()
	{
		await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
			await nuel.Db.Delete(1, "   "));
		await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
			await nuel.Db.Delete(1, "Users", "   "));
	}
}
