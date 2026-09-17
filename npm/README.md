# @nuell/db

Ultra-fast, zero-dependency TypeScript/JavaScript parser for the compact delimiter CSV format produced by the C# [nuel.Db](https://github.com/pellk/Db) library.

[![npm version](https://img.shields.io/npm/v/@nuell/db.svg)](https://www.npmjs.com/package/@nuell/db)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## ⚡ Why Use This Format?

Standard JSON payloads from SQL queries include redundant keys repeated on every row and heavy quote formatting. Standard RFC 4180 CSV lacks column type metadata and has escaping overhead.

The `nuel.Db` format solves this by:
- Using `~` as column delimiter and `|` as row delimiter (avoiding newline issues in transit).
- Encoding the column datatype as the first character in each header column.
- Representing SQL `NULL` with the single character `Ø`.
- Reducing network payload size by up to **60-80%** compared to JSON while parsing significantly faster in the browser.

### Type Marker Reference

| Marker | C# Backend Types | TypeScript / JavaScript Result |
| :---: | :--- | :--- |
| `!` | `byte`, `short`, `int`, `long` | `number` (integer) |
| `%` | `float`, `double`, `decimal` | `number` (float) |
| `^` | `bool` (`1` / `0`) | `boolean` (`true` / `false`) |
| `$` | `string`, `char`, `Guid`, `TimeSpan`, `byte[]` (base64) | `string` |
| `#` | `DateTime`, `DateTimeOffset` (ISO 8601 string) | `Date` |
| `Ø` | `DBNull` / `null` | `null` |

---

## 📦 Installation

```bash
npm install @nuell/db
```

Or using yarn/pnpm:
```bash
pnpm add @nuell/db
# or
yarn add @nuell/db
```

---

## 🚀 Quick Start

### 1. Parsing Single Resultset (`parseCsv`)

```typescript
import { parseCsv } from '@nuell/db';

interface User {
  Id: number;
  Name: string;
  Balance: number;
  IsActive: boolean;
  CreatedAt: Date;
}

// Fetch CSV from ASP.NET Core backend
const response = await fetch('/api/users');
const csv = await response.text();

const users = parseCsv<User>(csv);
console.log(users);
// Example values for offset-free CreatedAt fields:
// [
//   { Id: 1, Name: 'Alice', Balance: 150.5, IsActive: true, CreatedAt: new Date('2025-09-09T12:00:00') },
//   { Id: 2, Name: 'Bob', Balance: 0.0, IsActive: false, CreatedAt: new Date('2025-09-09T12:00:00') }
// ]
```

### 2. Parsing into a Keyed Map (`mapFromCsv`)

Ideal for lookup tables, caches, or state stores:

```typescript
import { mapFromCsv } from '@nuell/db';

interface Product {
  Id: number;
  Sku: string;
  Price: number;
}

// Keyed by first column (Id) by default:
const productMap = mapFromCsv<Product>(csv);
console.log(productMap.get(101));

// Or specify a key column by name or index:
const skuMap = mapFromCsv<Product, string>(csv, 'Sku');
console.log(skuMap.get('PROD-A1'));
```

---

## API

```typescript
parseCsv<T = Record<string, unknown>>(csv?: string | null): T[]
mapFromCsv<T = Record<string, unknown>, K = number | string>(
  csv?: string | null,
  keyColumn?: string | number
): Map<K, T>
```

Both functions return their results directly and accept no parsing options or row callback. `mapFromCsv` uses the first column as its key by default; `keyColumn` can be a column name or zero-based index. Empty input returns an empty array or map.

### Date handling

The `#` fields contain ISO 8601 strings with second precision (fractional seconds are omitted, not rounded). The backend emits `DateTime` without a timezone suffix and `DateTimeOffset` with its stored offset, without timezone conversion. The parser always converts valid values to JavaScript `Date` objects; invalid, missing, or null values become `null`. Offset-free date/time strings are interpreted in the browser's local timezone; explicit offsets identify an instant. JavaScript dates retain only millisecond precision and do not retain the original offset.

```typescript
const users = parseCsv<User>(csv);
console.log(users[0].CreatedAt instanceof Date); // true
```

JSON responses differ: `response.json()` leaves ISO dates as strings. CSV responses should be read with `response.text()` and passed to this parser.

When upgrading from the numeric date format, update the backend and parser together. Remove `ParseCsvOptions`, `dateMode`, and options arguments from calling code. There is no Unix timestamp mode; model date fields as `Date` (or `Date | null` for nullable fields).

---

## 🖥️ C# Backend Integration Example

In your ASP.NET Core Web API:

```csharp
using Microsoft.AspNetCore.Mvc;
using nuel;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        // Generates compact typed CSV directly from SQL Server
        string csv = await Db.Csv("SELECT Id, Name, Balance, IsActive, CreatedAt FROM Users");
        return Content(csv ?? string.Empty, "text/plain; charset=utf-8");
    }

    [HttpGet("stream")]
    public async Task GetUsersStream()
    {
        Response.ContentType = "text/plain; charset=utf-8";
        // Stream directly to the HTTP response with zero intermediate string allocation
        await Db.Csv("SELECT Id, Name, Balance, IsActive, CreatedAt FROM Users", Response.Body);
    }
}
```

---

## ⚛️ React Example

```tsx
import { useEffect, useState } from 'react';
import { parseCsv } from '@nuell/db';

interface Customer {
  CustomerId: number;
  CompanyName: string;
  Balance: number;
}

export function CustomerList() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/customers')
      .then((res) => res.text())
      .then((csv) => {
        setCustomers(parseCsv<Customer>(csv));
        setLoading(false);
      });
  }, []);

  if (loading) return <div>Loading...</div>;

  return (
    <ul>
      {customers.map((c) => (
        <li key={c.CustomerId}>
          {c.CompanyName} - ${c.Balance.toFixed(2)}
        </li>
      ))}
    </ul>
  );
}
```

---

## 📄 License

MIT © [Ako Mahmoodi](https://github.com/pellk)
