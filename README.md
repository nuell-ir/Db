# nuel.Db

A high-performance, developer-friendly SQL Server data access ecosystem designed to eliminate boilerplate, stream compact data directly from SQL Server, and parse it in client-side applications with zero overhead.

This repository is a polyglot monorepo containing two independently published packages:

| Package | Ecosystem | Directory | Description |
| :--- | :--- | :--- | :--- |
| **`nuel_Db`** | [.NET / NuGet](https://www.nuget.org/packages/nuel_Db) | [`dotnet/`](dotnet/) | Lightweight C# SQL Server library with streaming JSON/CSV support |
| **`@nuell/db`** | [npm](https://www.npmjs.com/package/@nuell/db) | [`npm/`](npm/) | Ultra-fast, zero-dependency TypeScript/JavaScript client parser |

---

## ⚡ Architecture & How They Work Together

Standard JSON SQL responses repeatedly transmit property keys for every single row, inflating payload sizes and browser parse times. Standard RFC 4180 CSV lacks column data types.

`nuel.Db` pairs a specialized backend C# serializer with a client-side TypeScript parser:

1. **Backend (ASP.NET Core / C#)**:
   SQL Server results are formatted directly into a compact delimiter CSV stream (`~` column separator, `|` row separator, single-character type markers in the header, and `Ø` for `NULL`).

   ```csharp
   using nuel;

   [HttpGet("users")]
   public async Task<IActionResult> GetUsers()
   {
       string csv = await Db.Csv("SELECT Id, Name, Balance, IsActive, CreatedAt FROM Users");
       return Content(csv, "text/plain; charset=utf-8");
   }
   ```

2. **Frontend / Client (TypeScript / JavaScript)**:
   The client consumes the compact stream with `@nuell/db`, reconstructing fully-typed objects up to **60–80% smaller** than standard JSON.

   ```typescript
   import { parseCsv } from '@nuell/db';

   interface User {
     Id: number;
     Name: string;
     Balance: number;
     IsActive: boolean;
     CreatedAt: number;
   }

   const res = await fetch('/api/users');
   const users = parseCsv<User>(await res.text());
   ```

---

## 📁 Repository Structure

```text
.
├── .editorconfig          # Shared formatting rules across .NET and JS/TS
├── .gitignore             # Monorepo-wide ignore rules
├── LICENSE                # MIT License
├── README.md              # Repository portal documentation (this file)
│
├── dotnet/                # .NET / NuGet package root
│   ├── Db.sln             # Visual Studio / dotnet solution file
│   ├── Db.slnx            # Modern XML solution file
│   ├── README.md          # Comprehensive .NET documentation
│   ├── src/
│   │   ├── Db.csproj      # Library project (targets net10.0)
│   │   └── *.cs
│   └── tests/
│       ├── Db.Tests.csproj# Unit test suite
│       └── *.cs
│
└── npm/                   # npm package root
    ├── .gitignore
    ├── LICENSE
    ├── README.md          # Comprehensive npm package documentation
    ├── package.json       # @nuell/db definition & scripts
    ├── tsconfig.json      # TypeScript compiler configuration
    ├── tsup.config.ts     # Bundler configuration (ESM + CJS + DTS)
    ├── src/               # TypeScript source code
    └── tests/             # Node test runner suite
```

---

## 🛠️ Development & Building

### .NET Library
```bash
# Build entire solution
dotnet build dotnet/Db.sln

# Run all unit tests
dotnet test dotnet/Db.sln

# Create NuGet package (.nupkg)
dotnet pack dotnet/src/Db.csproj
```

### npm Package
```bash
cd npm

# Install dependencies
npm install

# Run tests
npm test

# Typecheck and build bundle (ESM/CJS/DTS)
npm run typecheck
npm run build
```

---

## 📄 License

This repository is licensed under the [MIT License](LICENSE).
