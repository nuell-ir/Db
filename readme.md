# Introduction

Generally speaking, `Db` aims to minimise the SQL Server boilerplate code. In addition, several methods target front-end needs of an ASP.NET website. Using methods such as `Json` or `Csv`, the SQL Server data will not be cast as POCO and will be directly available for JavaScript use, saving development and processing time.

## Initialisation

Set the connection string to get started. This should be done only once and preferably at startup.

```c#
nuell.Data.ConnectionString = mySqlServerConnectionString;
```

Depending on the use case, you may choose synchronous or asynchronous methods. Typically asynchronous methods are preferred for non-blocking purposes.

```c#
using nuell.Sync;
using nuell.Async;
```

Asynchronous methods must be preceded with the `await` keyword and put in `async` methods.

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

Since it is very important to pass user input as parameters in order to prevent SQL injection, most of the methods allow SQL parameters. 

```c#
string query = "select count(1) from Employees where City=@city";
int count = await Db.Val<int>(query, isStoredProc: false, new SqlParameter("@city", "London"));
```

Simpler overloads accepting `ValueTuple(name, value)` parameters can also be used to pass the SQL parameters.

```c#
int count = await Db.Val<int>(query, ("@city", "London"));
```

A shorthand for Nullable string parameters is the `nuell.Data.NS` function, which replaces empty strings with a `null` value.

```c#
await Db.Execute("update Customers set FullName=@name where Id=@id",
                Data.NS("@name", name), // nullable string parameter
                new SqlParameter("@id", id));
```

# Methods

All the methods are static.



## `Csv`

Converts the query result or an object array to a CSV string, which drastically reduces response size, in comparison to JSON values.

For instance, let's retrieve the following data in a SQL Server table named Employees:

| Id   | FullName            | BirthDate  | IsMarried |
| ---- | ------------------- | ---------- | --------- |
| 1    | Loraine Bickerdicke | 1994-08-22 | true      |
| 2    | Shelley Askem       | 1992-12-07 | false     |

The same data may be passed via an array, provided that all the elements have the same properties:

```c#
new[] {
    new { 
        Id = 1, 
        FullName = "Loraine Bickerdicke", 
        BirthDate = new DateTime(1994, 8, 22), 
        IsMarried = true
    },
    new { 
        Id = 2, 
        FullName = "Shelley Askem", 
        BirthDate = new DateTime(1992, 12, 7), 
        IsMarried = false
    },
}
```

The `Csv` method returns the result of the given query as a CSV string.

```c#
string csv = await Db.Csv("select * from Employees");
//!Id~$FullName~#BirthDate~^IsMarried|1~Loraine Bickerdicke~777497400~1|2~Shelley Askem~723673800~0
```

Please note that the standard comma and new line characters have been replaced by tilde (~) and vertical line (|) respectively in order to avoid conflicts with typical texts. 

Moreover, column names have been flagged with the following type markers:

| Flag   | Value Type |
| ------ | ---------- |
| $      | string     |
| !      | integer    |
| %      | float      |
| ^      | boolean    |
| #      | date/time  |

The returned CSV value may be parsed in the front-end as a JavaScript array of objects using the following function:

```typescript
function parseCsv<T>(csv: string): T[] {
    const output: T[] = [];
    if (!csv)
        return output;
    const rows = csv.split('|');
    const rowCount = rows.length;
    const headers = rows[0].split('~');
    const headerCount = headers.length;
    for (let i = 1; i < rowCount; i++) {
        const obj: T = {} as T;
        const values = rows[i].split('~');
        for (let h = 0; h < headerCount; h++)
            obj[headers[h].slice(1)] = values[h] == 'Ø' ? null : parser[headers[h][0]](values[h]);
        output.push(obj);
    }
    return output;
}

const parser = {
    '$': (val: string) => val,
    '!': (val: string) => parseInt(val),
    '%': (val: string) => parseFloat(val),
    '^': (val: string) => val == '1',
    '#': (val: string) => new Date(parseInt(val) * 1000),
};
```

## `MultiCsv`

In case your query returns more than one result, use `MultiCsv` to return a string array containing CSV values of the results. For example:

```c#
string[] results = await Db.MultiCsv("select * from Employees; select * from Customers");
```

The returned array has two elements containing Employees and Customers CSV values.

## `Json`

Converts the query result to JSON.

```c#
string json = await Db.Json($"select * from Customers where Id={id}");
//{"Id":1,"FullName":"Loraine Bickerdicke","BirthDate":"1994-08-22","IsMarried":true}
```

The default result is a JSON object. However, using an optional parameter you may require a JSON array result.

```c#
string json = await Db.Json($"select Id, Age from Customers", JsonValueType.Array);
//[{"Id":1,"Age":24},{"Id":2,"Age":36},{"Id":3,"Age":31}]
```

## `JsonObject`

Converts one data row to `System.Text.Json.Nodes.JsonObject`.

```c#
JsonObject jobj = await Db.JsonObject($"select * from Customers where Id={id}");
```

## `Table`

Converts the query result to `System.Data.DataTable`. For example:

```c#
DataTable employees = await Db.Table("select * from Employees");
```

## `List<T>`

Converts a one-field query result to `System.Collections.Generic.List<T>`. For example:

```c#
List<int> idList = await Db.List<int>("select Id from Employees");
```

## `StrList`

Converts a one-field query result to `List<string>`. For example:

```c#
List<string> names = await Db.StrList("select FullName from Employees");
```

## `Object<T>`

Converts a one-row query result to an object of the class `T`. For example:

```c#
Employee employee = await Db.Object<Employee>($"select * from Employees where Id={id}");
```

The names and types of the class properties must match the query fields. Query field name matching is case-sensitive.

## `ObjList<T>`

Converts the query result to a `System.Collections.Generic.List<T>`, where T is a class. For example:

```c#
List<Employee> employeeList = await Db.ObjList<Employee>("select * from Employees");
```

The names and types of the class properties must match the query fields. Query field name matching is case-sensitive.

## `Dictionary<K, V>`

Converts a two-field query to a `System.Collections.Generic.Dictionary<K, V>`. For example:

```c#
Dictionary<int, string> cities = await Db.Dictionary<int, string>("select ZipCode, City from Addresses");
```

## `Str`

Returns one string value. For example:

```c#
string s = await Db.Str($"select FullName from Employees where Id={id}");
```

## `Val<T>`

Returns one value of the primitive type `T`.

```c#
int c = await Db.Val<int>("select count(1) from Employees");
```

## `Values`

Returns all the fields of all the rows as a boxed `System.Object` array. For example:

```c#
object[] values = await Db.Values($"select Id, BirthDate from Employees where Id={id}; select count(1) from Customers");

int id = (int)values[0];
DateTime birth = (DateTime)values[1];
int count = (int)values[2];
```

## `ComplexJson`

If the query returns multiple results of CSV, JSON array, JSON object, and simple values, consider using the `ComplexJson` method.

It receives a tuple array that specifies the label and type of each result and returns a JSON object. For example:

```c#
string query = "select count(1) from Employees;"
    + "select * from Employees where Id=1;"
    + "select * from Employees;"
    + "select * from Customers";

var resultTypes = new [] {
    ("employeeCount", JsonValueType.Value),
    ("oneEmployee", JsonValueType.Object),
    ("employeeCsv", JsonValueType.Csv),
    ("customersArray", JsonValueType.Array),
};

string json = await ComplexJson(query, resultTypes);
//{"employeeCount":1200,"oneEmployee":{...},"employeeCsv":"...","customersArray":[...]}
```

The returned JSON object in the above example includes 4 properties with the names corresponding to those specified in the tuple.

## `Execute`

Executes a query and returns the number of affected rows.

```c#
int rows = await Db.Execute("update Customers set Balance=0 where Balance>0");
```

## `Delete`

Deletes a record with the specified **Id** field from the given table and returns a boolean value to report the success of the operation.

```c#
bool success = await Db.Delete(5, "Customers");
```

## `Transaction`

Executes the query as a [transaction](https://docs.microsoft.com/en-us/sql/t-sql/language-elements/transactions-transact-sql), consisting of multiple operations and returns an array containing the number of affected rows for each operation.

```c#
string query1 = $"delete from Orders where CustomerId={id};";
string query2 = $"delete from Customers where Id={id};";
int[] rows = await Db.Transaction(query1 + query2);
```

## `Save`

Saves a `JsonElement`, `JsonObject`, `JsonNode`, or an object of type `<T>` to the specified table and returns the identity of the saved record.

The `JsonElement`, `JsonObject`, `JsonNode`, or `<T>` object data must include an identity property specified in the case-sensitive `idProp` parameter (default is `"Id"`), and the target table must contain an identity primary key with the same name.

If the value of the identity property is zero, it will be ignored and the rest of the properties will be *inserted* into the table. Then the newly created identity will be returned. Otherwise, the record with the specified identity will be *updated*. 

All the properties *must* match the table fields.

```c#
var employee = JsonNode.Parse("{ \"Id\": 0, \"FullName\": \"Shelley Askem\", \"Age\": 34, \"Balance\": 1520 }");
int id = await Save(employee, "Employees");
```

```c#
var employee = new Employee { Id = 0, FullName = "Shelley Askem", Age = 34, Balance = 1520 };
int id = await Save(employee, "Employees");
```

## `Insert` ##

Inserts `JsonElement`, `JsonObject`, or `JsonNode` data into the specified table. All the properties *must* match the table fields.

```c#
var employee = JsonNode.Parse("{\"FullName\":\"Shelley Askem\",\"Age\":34, \"Balance\":1520}");
await Insert(employee, "Employees");
```

## `Update` ##

Updates a row in the spcified table with `JsonElement`, `JsonObject`, or `JsonNode` data. The primary key field must be passed. All the properties *must* match the table fields.

```c#
var employee = JsonNode.Parse("{\"Id\":3,\"FullName\":\"Shelley Askem\",\"Age\":34, \"Balance\":1520}");
await Update(employee, "Employees", "Id");
```

## `NewItem`

Returns a JSON value containing a new record from the specified table. 

Default values of the table fields will be respected. If a table field has no default value, the values for nullable, numeric, boolean, and string fields will be `null`, 0, `false`, and empty string, respectively.

```c#
string json = await Db.NewItem("Employees");
//returns e.g. { "Id": 0, "FullName": "", "Married": false, "Address": null }
```

The returned value may be used to initialise  a reactive front-end form.