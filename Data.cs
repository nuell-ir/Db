using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace nuell
{
    public static partial class Data
    {
        public static string ConnectionString { get; set; }

        public static SqlParameter NS(string name, string value)
            => new SqlParameter(name, string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim());
        internal static readonly SqlParameter[] NoParams = new SqlParameter[] { };

        internal static SqlParameter[] SqlParams((string name, object value)[] parameters)
            => parameters.Select(p => new SqlParameter(p.name, p.value)).ToArray();

    }
}

