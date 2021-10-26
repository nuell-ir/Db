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

Returns the result of a given query as a CSV string. This drastically reduces response size, in comparison to JSON values.

For instance, let's retrieve the following data in a SQL Server table named Employees:

| Id   | FullName            | BirthDate  | IsMarried |
| ---- | ------------------- | ---------- | --------- |
| 1    | Loraine Bickerdicke | 1994-08-22 | true      |
| 2    | Shelley Askem       | 1992-12-07 | false     |

The `Csv` method returns the result of the given query as a CSV string.

```c#
string csv = await Db.Csv("select * from Employees");
//!Id~$FullName~#BirthDate~^IsMarried|1~Loraine Bickerdicke~1994-08-22~1|2~Shelley Askem~1992-12-07~0
```

Please note that the standard comma and new line characters have been replaced by tilde (~) and pipe (|) respectively in order to avoid conflicts with typical texts. 

Moreover, column names begin type flags which are as follows:

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
    '#': (val: string) => new Date(parseInt(val)),
};
```

## `MultiCsv`

In case your query returns more than one result, use `MultiCsv` to return a string array containing CSV values of the results. For example:

```c#
string[] results = await Db.MultiCsv("select * from Employees; select * from Customers");
```

The returned array has two elements containing Employees and Customers CSV values.

## `Json`

Converts one data row to standard JSON.

```c#
string json = await Db.Json($"select * from Customers where Id={id}");
//{"Id":1,"FullName":"Loraine Bickerdicke","BirthDate":"1994-08-22","IsMarried":true}
```

## `JObject`

Converts one data row to a [Json.net](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JObject.htm) `JObject`.

```c#
JObject jobject = await Db.JObject($"select * from Employees where Id={id}");
```

## `Table`

Converts the query result to `System.Data.DataTable`. For example:

```c#
DataTable idList = await Db.Table("select * from Employees");
```

## `List<T>`

Converts a one-field query result to a `System.Collections.Generic.List<T>`. For example:

```c#
List<int> idList = await Db.List<int>("select Id from Employees");
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
var cities = await Db.Dictionary<int>("select ZipCode, City from Addresses");
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

## `Retrieve`

If a query returns multiple results of various types and you need to mix `Csv`, `JObject`, `Json`, `GetStr`, and `GetVal` methods, you can use `Retrieve`.

It receives a tuple array that specifies the label and type of each result. For example:

```c#
string query = "select count(1) from Employees;"
    + "select * from Employees;"
    + $"select * from Customers where Id={id};"
    + "select * from Customers";

JObject results = new [] {
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

Saves a `JObject` or `object` to the specified table and returns the **Id** of the saved record.

The `JObject` or `object` parameter must include an **Id** property (case insensitive), and the target table must have an **Id** field as the identity primary key.

If the value of **Id** is zero, it will be ignored and the rest of the properties will be inserted into the table. Then the new **Id** (created by table identity) will be returned. Otherwise, the record with the given **Id** will be updated. 

All the properties *must* match the table fields.

```c#
var employee = JObject.Parse("{ \"Id\": 0, \"FullName\": \"Shelley Askem\", \"Age\": 34, \"Balance\": 1520 }");
var employee = new { Id = 0, FullName = "Shelley Askem", Age = 34, Balance = 1520 }
int id = await Save(employee, "Employees");
```

## `NewItem`

Returns a JSON value containing a new record from the specified table. 

Default values of the table fields will be respected. If a table field has no default value, the values for nullable, numeric, boolean, and string fields will be `null`, 0, `false`, and empty string, respectively.

```c#
string json = await Db.NewItem("Employees");
//returns e.g. { "Id": 0, "FullName": "", "Married": false, "Address": null }
```

The returned value may be used to initialise  a reactive front-end form.