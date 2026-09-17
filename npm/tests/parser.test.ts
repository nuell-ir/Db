import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { parseCsv, mapFromCsv } from '../src/index.ts';

describe('parseCsv', () => {
	it('should parse ISO date fields with explicit offsets and local wall-clock values', () => {
		const csv = '#Local~#Offset~#Missing|2026-09-17T12:30:00.1234567~2026-09-17T12:30:00.1234567-04:00~Ø';
		const expectedLocal = new Date(2026, 8, 17, 12, 30, 0, 123).getTime();
		const expectedOffset = Date.UTC(2026, 8, 17, 16, 30, 0, 123);
		assert.deepEqual(parseCsv(csv), [{ Local: new Date(expectedLocal), Offset: new Date(expectedOffset), Missing: null }]);
		const dates = parseCsv<{ Local: Date; Offset: Date; Missing: null }>(csv);
		assert.equal(dates[0].Local.getTime(), expectedLocal);
		assert.equal(dates[0].Offset.getTime(), expectedOffset);
		assert.equal(dates[0].Missing, null);
	});

	it('should distinguish missing, empty, and null fields and ignore extra fields', () => {
		assert.deepEqual(parseCsv('$A~$B~$C|x|x~|x~~|~Ø~z~ignored||\r\n'), [
			{ A: 'x', B: null, C: null },
			{ A: 'x', B: '', C: null },
			{ A: 'x', B: '', C: '' },
			{ A: '', B: null, C: 'z' }
		]);
	});

	it('should preserve numeric prefix parsing and invalid-number handling', () => {
		assert.deepEqual(parseCsv('!Id~%Rate~#Created|12x~1.5x~123x|bad~~Ø'), [
			{ Id: 12, Rate: 1.5, Created: null },
			{ Id: null, Rate: null, Created: null }
		]);
	});

	it('should strip row-ending newlines while preserving internal newlines', () => {
		assert.deepEqual(parseCsv('$A~$B|a\nb~\r\n|c~d\r\n'), [
			{ A: 'a\nb', B: '' }, { A: 'c', B: 'd' }
		]);
	});

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
			'|1~Widget~19.99~1~2025-09-09T12:00:00.0000000+00:00~Ø';

		interface Item {
			Id: number;
			Name: string;
			Rate: number;
			IsActive: boolean;
			Created: Date;
			NullCol: unknown;
		}

		const result = parseCsv<Item>(csv);
		assert.equal(result.length, 1);
		assert.deepEqual(result[0], {
			Id: 1,
			Name: 'Widget',
			Rate: 19.99,
			IsActive: true,
			Created: new Date('2025-09-09T12:00:00Z'),
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
		const isoDate = "2025-09-09T12:00:00.0000000+00:00";

		const csv = '!Id~$Name~$UniqueId~#Timestamp~$Duration~$Data~%Rate~^IsActive~#Created~!NullableInt' +
			`|1~Item 1~${guid}~${isoDate}~${timespan}~${base64}~3.14~1~${isoDate}~Ø` +
			`|2~Ø~00000000-0000-0000-0000-000000000000~${isoDate}~00:00:00~Ø~0.5~0~${isoDate}~42`;

		interface AllTypes {
			Id: number;
			Name: string | null;
			UniqueId: string;
			Timestamp: Date;
			Duration: string;
			Data: string | null;
			Rate: number;
			IsActive: boolean;
			Created: Date;
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

	it('should return Date objects for date fields', () => {
		const isoDate = "2025-09-09T12:00:00.0000000+00:00"; // 2025-09-09T12:00:00Z
		const csv = '!Id~#Created|1~' + isoDate;
		const result = parseCsv<{ Id: number; Created: Date }>(csv);
		assert.equal(result.length, 1);
		assert.ok(result[0].Created instanceof Date);
		assert.equal(result[0].Created.getTime(), Date.parse(isoDate));
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
	it('should fall back to the first column for unknown names and index -1', () => {
		const csv = '!Id~$Name|1~Alpha|2~Beta';
		for (const key of ['Missing', '', -1]) {
			assert.deepEqual(mapFromCsv(csv, key), mapFromCsv(csv));
		}
	});

	it('should preserve raw extra-field keys and missing keys', () => {
		const csv = '!Id|1~extra|2~|3~Ø|4';
		assert.deepEqual([...mapFromCsv(csv, 1)], [
			['extra', { Id: 1 }], ['', { Id: 2 }], ['Ø', { Id: 3 }], [undefined, { Id: 4 }]
		]);
		for (const key of [-2, 0.5, NaN, Infinity, 10]) {
			assert.deepEqual([...mapFromCsv(csv, key)], [[undefined, { Id: 4 }]]);
		}
	});

	it('should use the final parsed value for duplicate header names', () => {
		assert.deepEqual([...mapFromCsv('!Id~!Id|1~2|3~2', 0)], [[2, { Id: 2 }]]);
	});

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
