function* parseCsvRows(
	csv: string | undefined | null
): Generator<{ obj: Record<string, unknown>; row: string; keys: string[] }> {
	if (!csv) return;

	const firstPipe = csv.indexOf('|');
	if (firstPipe === -1) return;

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

		let fieldPos = 0;
		const obj: Record<string, unknown> = {};

		for (let h = 0; h < headerCount; h++) {
			let val: string | undefined;
			if (fieldPos <= row.length) {
				const delimiter = row.indexOf('~', fieldPos);
				const end = delimiter === -1 ? row.length : delimiter;
				val = row.slice(fieldPos, end);
				// Keep a trailing empty field distinct from a missing field.
				fieldPos = delimiter === -1 ? row.length + 1 : delimiter + 1;
			}
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
					const ms = Date.parse(val);
					if (isNaN(ms)) {
						obj[key] = null;
					} else {
						obj[key] = new Date(ms);
					}
					break;
				}
				default:
					obj[key] = val;
					break;
			}
		}

		yield { obj, row, keys };

		if (nextPipe === -1) break;
	}
}

export function parseCsv<T = Record<string, unknown>>(
	csv?: string | null
): T[] {
	const output: T[] = [];
	for (const { obj } of parseCsvRows(csv)) {
		output.push(obj as T);
	}
	return output;
}

export function mapFromCsv<T = Record<string, unknown>, K = number | string>(
	csv?: string | null,
	keyColumn?: string | number
): Map<K, T> {
	const map = new Map<K, T>();
	let resolvedKeyIndex = typeof keyColumn === 'number' ? keyColumn : -1;
	const resolvedKeyName = typeof keyColumn === 'string' ? keyColumn : '';
	let keyName = '';
	let keyResolved = false;

	for (const { obj, row, keys } of parseCsvRows(csv)) {
		if (!keyResolved) {
			if (resolvedKeyIndex === -1) {
				resolvedKeyIndex = resolvedKeyName === '' ? 0 : keys.indexOf(resolvedKeyName);
				if (resolvedKeyIndex === -1) resolvedKeyIndex = 0;
			}
			keyName = keys[resolvedKeyIndex];
			keyResolved = true;
		}

		// Preserve raw-field fallback for indices outside the header without
		// allocating a field array for the normal, named-column path.
		const key = (keyName in obj ? obj[keyName] : row.split('~')[resolvedKeyIndex]) as K;
		map.set(key, obj as T);
	}

	return map;
}
