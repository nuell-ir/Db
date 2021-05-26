# Introduction

Db is specifically written for front-end needs of an ASP.NET website. Unlike ORM libraries, the SQL Server data will not be cast as POCO and will be directly available on the fly for JavaScript use, saving development and processing time.

## Initialisation

Set the connection string to get started. This should be done only once and preferably at startup.

```c#
nuell.Data.ConnStr = mySqlServerConnectionString;
```

Depending on the use case, you may choose synchronous or asynchronous methods. Typically asynchronous methods are preferred.

```c#
using nuell.Sync;
using nuell.Async;
```

Asynchronous methods must be preceded with `await` keyword and put in `async` methods.

## Stored Procedures

Most of the methods have an overload to mark a query as a stored procedure.

```c#
string result = await Db.Csv("dbo.GetResults", isStoredProc: true);
```

 However, an alternative way of executing a stored procedure is using the SQL `exec` command:

```c#
string result = await Db.Csv("exec dbo.GetResults");
```

## Parameters

Most of the methods allow SQL parameters. 

```c#
string query = "select count(1) from Employees where City=@city";
int count = await Db.GetVal<int>(query, new SqlParameter("@city", "London"));
```

It is very important to pass user input as parameters in order to prevent SQL injection.

A shorthand for Nullable string parameters is the `nuell.Data.NS` function, which replaces empty strings with a `null` value.

```c#
await Db.Execute("update Customers set FullName=@name where Id=@id",
                Data.NS("@name", name), // nullable string parameter
                new SqlParameter("@id", id));
```

# Methods

All the methods are static.



## Csv

Returns the result of a given query as a CSV string. This drastically reduces response size, in comparison to JSON values.

For instance, let's retrieve the following data in a SQL Server table named Employees:

| Id   | FullName            | BirthDate  | IsMarried |
| ---- | ------------------- | ---------- | --------- |
| 1    | Loraine Bickerdicke | 1994-08-22 | true      |
| 2    | Shelley Askem       | 1992-12-07 | false     |

The `Csv` method returns the result of the given query as a CSV string.

```c#
string result = await Db.Csv("select * from Employees");
//Id~$FullName~#BirthDate~IsMarried|1~Loraine Bickerdicke~1994-08-22~true|2~Shelley Askem~1992-12-07~false
```

Please note that the standard comma and new line characters have been replaced by tilde (~) and pipe (|) respectively in order to avoid conflicts with typical texts.

This returned CSV value can parsed as a JavaScript array of objects in the client using the following function:

```javascript
function parseCsv(csv) {
    let output = [];
    if (!csv)
        return output;
    const rows = csv.split('|');
    const rowCount = rows.length;
    const headers = rows[0].split('~');
    const headerCount = headers.length;
    const parse = (header, val) => {
        switch (header[0]) {
            case '$':
                return { [header.slice(1)]: val };
            case '#':
                return { [header.slice(1)]: new Date(val) };
            default:
                return { [header]: eval(val) };
        }
    };
    for (let i = 1; i < rowCount; i++) {
        let obj = {};
        let values = rows[i].split('~');
        for (let j = 0; j < headerCount; j++)
            obj = Object.assign(obj, parse(headers[j], values[j]));
        output.push(obj);
    }
    return output;
}
```

For more convenience, the builtin minified parser function can be included an ASP.NET razor page:

```
<script>@Html.Raw(nuell.Data.ParseCsv())</script>
```

## MultiCsv

In case your query returns more than one result, use `MultiCsv` to return a string array containing CSV values of the results. For example:

```c#
string[] results = await Db.MultiCsv("select * from Employees; select * from Customers");
```

The returned array has two elements containing Employees and Customers CSV values.

## Json

Converts one data row into standard JSON.

```c#
string json = await Db.Json($"select * from Customers where Id={1}");
//{"Id":1,"FullName":"Loraine Bickerdicke","BirthDate":"1994-08-22","IsMarried":true}
```

## JObject

Converts one data row into [Json.net JObject](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JObject.htm).

```c#
var jobject = await Db.JObject("select * from Employees where Id=@id", new SqlParameter("@id", id));
```

## Table

Converts the query result into `System.Data.DataTable`.

## List< T >

Converts a one-field query into a `System.Collections.Generic.List< T >`. For example:

```c#
var idList = await Db.List<int>("select Id from Employees");
```

## Dictionary<K, V>

Converts a two-field query into a `System.Collections.Generic.Dictionary<K, V>`. For example:

```c#
var cities = await Db.Dictionary<int>("select ZipCode, City from Addresses");
```

## GetStr

Returns one string value. For example:

```c#
string s = await Db.GetStr("select FullName from Employees where Id=" + id);
```

## GetVal< T >

Returns one value with the specified primitive type T such as `int`, `boolean`, or `DateTime`.

```c#
int c = await Db.GetVal<int>("select count(1) from Employees");
```

## GetValues

Returns all the fields of all the rows as a `System.Object` array. These objects are boxed. For example:

```c#
var objects = await Db.GetValues($"select Id, BirthDate from Employees where Id={id}; select count(1) from Customers");

int id = (int)objects[0];
DateTime birth = (DateTime)objects[1];
int count = (int)objects[2];
```

## Get

Syntactic sugar to return a record with the specified **Id** from a given table.

```c#
JObject employee = await Db.Get(5, "Employees");
```

Which is the equivalent of:

```c#
var employee = await Db.JObject("select * from Employees where Id=5");
```

## Retrieve

If a query returns multiple results of various types and you need to mix `Csv`, `JObject`, `Json`, `GetStr`, and `GetVal` methods, you can use `Retrieve`.

It receives a tuple array that specifies the label and type of each result. For example:

```c#
string query = "select count(1) from Employees;"
    + "select * from Employees;"
    + "select * from Customers where Id=2;"
    + "select * from Customers";

var results = new [] {
    ("EmployeeCount", Results.Object),
    ("Employees", Results.Csv),
    ("SecondCustomer", Results.JObject),
    ("Customers", Results.Json),
};

JObject data = await Retrieve(query, results);
```

The returned value in the above example contains 4 properties, which can be accessed using the given labels, such as:

```c#
int count = (int)data["EmployeeCount"];
```

## Execute

Executes a query and returns the number of affected rows.

```c#
int rows = await Db.Execute("update Customers set Balance=0 where Balance>0");
```

## Delete

Deletes a record with the specified **Id** field from the given table and returns a boolean value to report the success of the operation.

```c#
bool success = await Db.Delete(5, "Customers");
```

## Transaction

Executes the query as a [transaction](https://docs.microsoft.com/en-us/sql/t-sql/language-elements/transactions-transact-sql), consisting of multiple operations and returns an array containing the number of affected rows for each operation.

```c#
string query1 = "delete from Orders where CustomerId=5;";
string query2 = "delete from Customers where Id=5;";
int[] rows = await Db.Transaction(query1 + query2);
```

## Save

Saves a `JObject` or `object` to the specified table and returns the **Id** of the saved record.

The `JObject` or `object` parameter must include an **Id** property (case insensitive), and the target table must have an **Id** field as the identity primary key.

If the value of **Id** is zero, it will be ignored and the rest of the properties will be inserted into the table. Then the new **Id** (created by table identity) will be returned. Otherwise, the record with the given **Id** will be updated. 

All the properties *must* match the table fields.

```c#
var employee = JObject.Parse("{ \"Id\": 0, \"FullName\": \"Shelley Askem\", \"Age\": 34, \"Balance\": 1520 }");
var employee = new { Id = 0, FullName = "Shelley Askem", Age = 34, Balance = 1520 }
int id = await Save(employee, "Employees");
```

## NewItem

Returns a JSON value containing a new record from the specified table. 

Default values of the table fields will be respected. If a table field has no default values, the values for nullable, numeric, boolean, and string fields will be `null`, 0, `false`, and empty string, respectively.

```c#
string json = await Db.NewItem("Employees");
//returns e.g. { "Id": 0, "FullName": "", "Married": false, "Address": null }
```

The returned value can used to initialise  a reactive form in the client, such as  React or Angular forms.