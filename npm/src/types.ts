/**
 * Supported column type flags encoded in the CSV header by Csv.cs:
 *
 * - `!` : Integer (Byte, Int16, Int32, Int64) -> parsed as number
 * - `%` : Float / Decimal (Single, Double, Decimal) -> parsed as number
 * - `^` : Boolean ('1' = true, '0' = false) -> parsed as boolean
 * - `$` : String / Text (String, Char, Guid, TimeSpan, byte[] Base64) -> parsed as string
 * - `#` : DateTime / DateTimeOffset (ISO 8601 string) -> parsed as Date
 */
export type ColumnTypeFlag = '!' | '%' | '^' | '$' | '#' | string;
