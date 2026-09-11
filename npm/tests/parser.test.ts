import test, { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { parseCsv, mapFromCsv, parseMultiCsv } from '../src/index.ts';

describe('parseCsv', () => {
	it('should return empty array for null, undefined, or empty string', () => {
		assert.deepEqual(parseCsv(), []);
		assert.deepEqual(parseCsv(null), []);
		assert.deepEqual(parseCsv(''), []);
	});

	it('should return empty array when header is present but no rows exist', () => {
		assert.deepEqual(parseCsv('!Id~$Name~%Price'), []);
	});

	it('should correctly parse all supported data types', () => {
		const csv = '!Id~$Name~%Rate~^IsActive~#Created~ØNullCol' +
			'|1~Widget~19.99~1~1757419200~Ø';

		interface Item {
			Id: number;
			Name: string;
			Rate: number;
			IsActive: boolean;
			Created: number;
			NullCol: unknown;
		}

		const result = parseCsv<Item>(csv);
		assert.equal(result.length, 1);
		assert.deepEqual(result[0], {
			Id: 1,
			Name: 'Widget',
			Rate: 19.99,
			IsActive: true,
			Created: 1757419200000,
			NullCol: null
		});
	});

	it('should parse boolean values correctly (1 = true, 0 = false)', () => {
		const csv = '^FlagA~^FlagB|1~0|0~1';
		const result = parseCsv<{ FlagA: boolean; FlagB: boolean }>(csv);
		assert.equal(result.length, 2);
		assert.deepEqual(result[0], { FlagA: true, FlagB: false });
		assert.deepEqual(result[1], { FlagA: false, FlagB: true });
	});

	it('should parse multiple rows accurately', () => {
		const csv = '!Id~$Name|1~Alice|2~Bob|3~Charlie';
		const result = parseCsv<{ Id: number; Name: string }>(csv);
		assert.equal(result.length, 3);
		assert.deepEqual(result[0], { Id: 1, Name: 'Alice' });
		assert.deepEqual(result[1], { Id: 2, Name: 'Bob' });
		assert.deepEqual(result[2], { Id: 3, Name: 'Charlie' });
	});

	it('should correctly match C# CsvTests parity with GUID, TimeSpan, Base64, and nulls', () => {
		const guid = '11111111-2222-3333-4444-555555555555';
		const timespan = '01:30:00';
		const base64 = 'AQIDBA==';
		const unixSec = 1757419200;

		const csv = '!Id~$Name~$UniqueId~#Timestamp~$Duration~$Data~%Rate~^IsActive~#Created~!NullableInt' +
			`|1~Item 1~${guid}~${unixSec}~${timespan}~${base64}~3.14~1~${unixSec}~Ø` +
			`|2~Ø~00000000-0000-0000-0000-000000000000~${unixSec}~00:00:00~Ø~0.5~0~${unixSec}~42`;

		interface AllTypes {
			Id: number;
			Name: string | null;
			UniqueId: string;
			Timestamp: number;
			Duration: string;
			Data: string | null;
			Rate: number;
			IsActive: boolean;
			Created: number;
			NullableInt: number | null;
		}

		const result = parseCsv<AllTypes>(csv);
		assert.equal(result.length, 2);

		// Row 1
		assert.equal(result[0].Id, 1);
		assert.equal(result[0].Name, 'Item 1');
		assert.equal(result[0].UniqueId, guid);
		assert.equal(result[0].Duration, timespan);
		assert.equal(result[0].Data, base64);
		assert.equal(result[0].Rate, 3.14);
		assert.equal(result[0].IsActive, true);
		assert.equal(result[0].NullableInt, null);

		// Row 2 (nulls represented by Ø)
		assert.equal(result[1].Id, 2);
		assert.equal(result[1].Name, null);
		assert.equal(result[1].Data, null);
		assert.equal(result[1].Rate, 0.5);
		assert.equal(result[1].IsActive, false);
		assert.equal(result[1].NullableInt, 42);
	});

	it('should support dateMode: "date" to return Date objects', () => {
		const unixSec = 1757419200; // 2025-09-09T12:00:00Z
		const csv = '!Id~#Created|1~' + unixSec;
		const result = parseCsv<{ Id: number; Created: Date }>(csv, { dateMode: 'date' });
		assert.equal(result.length, 1);
		assert.ok(result[0].Created instanceof Date);
		assert.equal(result[0].Created.getTime(), unixSec * 1000);
	});

	it('should handle trailing pipe and trailing newlines gracefully', () => {
		const csv = '!Id~$Name|1~Alice|2~Bob|\r\n';
		const result = parseCsv<{ Id: number; Name: string }>(csv);
		assert.equal(result.length, 2);
		assert.deepEqual(result[0], { Id: 1, Name: 'Alice' });
		assert.deepEqual(result[1], { Id: 2, Name: 'Bob' });
	});

	it('should preserve empty string when value is empty vs null (Ø)', () => {
		const csv = '!Id~$EmptyStr~$NullStr|1~~Ø';
		const result = parseCsv<{ Id: number; EmptyStr: string; NullStr: string | null }>(csv);
		assert.equal(result.length, 1);
		assert.equal(result[0].EmptyStr, '');
		assert.equal(result[0].NullStr, null);
	});
});

describe('mapFromCsv', () => {
	it('should return empty Map for empty input', () => {
		const map = mapFromCsv();
		assert.equal(map.size, 0);
	});

	it('should default to first column as key', () => {
		const csv = '!Id~$Name|101~Alpha|102~Beta';
		const map = mapFromCsv<{ Id: number; Name: string }>(csv);
		assert.equal(map.size, 2);
		assert.deepEqual(map.get(101), { Id: 101, Name: 'Alpha' });
		assert.deepEqual(map.get(102), { Id: 102, Name: 'Beta' });
	});

	it('should allow specifying key column by name', () => {
		const csv = '!Id~$Code~$Name|1~A1~Alpha|2~B2~Beta';
		const map = mapFromCsv<{ Id: number; Code: string; Name: string }, string>(csv, 'Code');
		assert.equal(map.size, 2);
		assert.equal(map.get('A1')?.Name, 'Alpha');
		assert.equal(map.get('B2')?.Name, 'Beta');
	});

	it('should allow specifying key column by index', () => {
		const csv = '!Id~$Code~$Name|1~A1~Alpha|2~B2~Beta';
		const map = mapFromCsv<{ Id: number; Code: string; Name: string }, string>(csv, 1);
		assert.equal(map.size, 2);
		assert.equal(map.get('A1')?.Id, 1);
		assert.equal(map.get('B2')?.Id, 2);
	});
});

describe('parseMultiCsv', () => {
	it('should handle array of CSV strings', () => {
		const ds1 = '!Id~$Name|1~Alice';
		const ds2 = '!OrderId~%Total|10~99.50';
		const result = parseMultiCsv([ds1, ds2]);
		assert.equal(result.length, 2);
		assert.deepEqual(result[0], [{ Id: 1, Name: 'Alice' }]);
		assert.deepEqual(result[1], [{ OrderId: 10, Total: 99.50 }]);
	});

	it('should handle newline-separated stream string from Db.MultiCsv', () => {
		const multiCsvStream = '!Id~$Name|1~Alice\n!OrderId~%Total|10~99.50\r\n';
		const result = parseMultiCsv(multiCsvStream);
		assert.equal(result.length, 2);
		assert.deepEqual(result[0], [{ Id: 1, Name: 'Alice' }]);
		assert.deepEqual(result[1], [{ OrderId: 10, Total: 99.50 }]);
	});

	it('should return empty array for null or empty input', () => {
		assert.deepEqual(parseMultiCsv(), []);
		assert.deepEqual(parseMultiCsv(null), []);
		assert.deepEqual(parseMultiCsv(''), []);
	});
});
