using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Data.SqlClient;

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

        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = false,
        };

        internal static readonly JsonWriterOptions JsonWriterOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}

