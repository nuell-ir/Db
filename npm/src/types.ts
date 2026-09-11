/**
 * Configuration options for parsing CSV output from nuel.Db C# library.
 */
export interface ParseCsvOptions {
	/**
	 * How date fields ('#') should be parsed:
	 * - 'timestamp': returns epoch milliseconds as number (default, e.g. 1757419200000)
	 * - 'date': returns JavaScript Date object (new Date(ms))
	 *
	 * @default 'timestamp'
	 */
	dateMode?: 'timestamp' | 'date';
}

/**
 * Supported column type flags encoded in the CSV header by Csv.cs:
 *
 * - `!` : Integer (Byte, Int16, Int32, Int64) -> parsed as number
 * - `%` : Float / Decimal (Single, Double, Decimal) -> parsed as number
 * - `^` : Boolean ('1' = true, '0' = false) -> parsed as boolean
 * - `$` : String / Text (String, Char, Guid, TimeSpan, byte[] Base64) -> parsed as string
 * - `#` : DateTime / DateTimeOffset (Unix epoch seconds) -> parsed as timestamp ms or Date
 */
export type ColumnTypeFlag = '!' | '%' | '^' | '$' | '#' | string;
