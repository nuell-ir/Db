using System.Text.Json;
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
            SaveAllQuery.Create(default, null, "MyTable", "Id"));

        Assert.AreEqual("json", ex.ParamName);
    }

    [TestMethod]
    public void SaveAllQuery_Create_EmptyArrayWithDeleteIds_GeneratesDeleteQuery()
    {
        using var doc = JsonDocument.Parse("[]");
        var query = SaveAllQuery.Create(doc.RootElement, "1,2,3", "MyTable", "Id");

        StringAssert.Contains(query, "DELETE FROM MyTable WHERE [Id] in (1,2,3);");
    }
}

