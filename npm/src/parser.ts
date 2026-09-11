import type { ParseCsvOptions } from './types.ts';

function parseCsvRows(
	csv: string | undefined | null,
	options: ParseCsvOptions | undefined,
	onRow: (obj: Record<string, unknown>, rawValues: string[], keys: string[]) => void
): string[] {
	if (!csv) return [];

	const firstPipe = csv.indexOf('|');
	if (firstPipe === -1) return [];

	const headerRow = csv.slice(0, firstPipe);
	const headerParts = headerRow.split('~');
	const headerCount = headerParts.length;

	const keys: string[] = new Array(headerCount);
	const types: string[] = new Array(headerCount);

	for (let i = 0; i < headerCount; i++) {
		const part = headerParts[i];
		types[i] = part.charAt(0);
		keys[i] = part.slice(1);
	}

	const isDateMode = options?.dateMode === 'date';
	let pos = firstPipe + 1;
	const len = csv.length;

	while (pos <= len) {
		const nextPipe = csv.indexOf('|', pos);
		let row: string;

		if (nextPipe === -1) {
			row = csv.slice(pos);
		} else {
			row = csv.slice(pos, nextPipe);
			pos = nextPipe + 1;
		}

		if (row.endsWith('\n') || row.endsWith('\r')) {
			row = row.replace(/[\r\n]+$/, '');
		}

		if (row.length === 0) {
			if (nextPipe === -1) break;
			continue;
		}

		const values = row.split('~');
		const obj: Record<string, unknown> = {};

		for (let h = 0; h < headerCount; h++) {
			const val = values[h];
			const key = keys[h];

			if (val === undefined || val === 'Ø') {
				obj[key] = null;
				continue;
			}

			switch (types[h]) {
				case '$':
					obj[key] = val;
					break;
				case '!': {
					const parsed = parseInt(val, 10);
					obj[key] = isNaN(parsed) ? null : parsed;
					break;
				}
				case '%': {
					const parsed = parseFloat(val);
					obj[key] = isNaN(parsed) ? null : parsed;
					break;
				}
				case '^':
					obj[key] = val === '1';
					break;
				case '#': {
					const sec = parseInt(val, 10);
					if (isNaN(sec)) {
						obj[key] = null;
					} else {
						const ms = sec * 1000;
						obj[key] = isDateMode ? new Date(ms) : ms;
					}
					break;
				}
				default:
					obj[key] = val;
					break;
			}
		}

		onRow(obj, values, keys);

		if (nextPipe === -1) break;
	}

	return keys;
}

export function parseCsv<T = Record<string, unknown>>(
	csv?: string | null,
	options?: ParseCsvOptions
): T[] {
	const output: T[] = [];
	parseCsvRows(csv, options, (obj) => {
		output.push(obj as T);
	});
	return output;
}

export function mapFromCsv<T = Record<string, unknown>, K = number | string>(
	csv?: string | null,
	keyColumn?: string | number,
	options?: ParseCsvOptions
): Map<K, T> {
	const map = new Map<K, T>();
	let resolvedKeyIndex = typeof keyColumn === 'number' ? keyColumn : -1;
	let resolvedKeyName = typeof keyColumn === 'string' ? keyColumn : '';

	parseCsvRows(csv, options, (obj, values, keys) => {
		if (resolvedKeyIndex === -1 && resolvedKeyName === '') {
			resolvedKeyIndex = 0;
		} else if (resolvedKeyIndex === -1 && resolvedKeyName !== '') {
			resolvedKeyIndex = keys.indexOf(resolvedKeyName);
			if (resolvedKeyIndex === -1) {
				resolvedKeyIndex = 0;
			}
		}

		const keyName = keys[resolvedKeyIndex];
		const key = (keyName in obj ? obj[keyName] : values[resolvedKeyIndex]) as K;
		map.set(key, obj as T);
	});

	return map;
}

export function parseMultiCsv<T extends unknown[] = Record<string, unknown>[][]>(
	csv?: string | string[] | null,
	options?: ParseCsvOptions
): T {
	if (!csv) return [] as unknown as T;

	if (Array.isArray(csv)) {
		return csv.map((singleCsv) => parseCsv(singleCsv, options)) as unknown as T;
	}

	if (typeof csv === 'string') {
		const datasets = csv.split(/\r?\n/).filter((s) => s.trim().length > 0);
		return datasets.map((singleCsv) => parseCsv(singleCsv, options)) as unknown as T;
	}

	return [] as unknown as T;
}
