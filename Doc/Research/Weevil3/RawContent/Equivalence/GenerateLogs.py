import os, random, sys
out = sys.argv[1]
os.makedirs(out, exist_ok=True)
random.seed(1133)
def tsv(i, sev='Information', tid=2, text=None):
    s = i % 60
    return f"2026-10-16 10:{(i//60)%60:02d}:{s:02d}.{i%100000:05d}\t123\t{tid}\t{sev}\t{text if text is not None else 'Record ' + str(i)}"
sevs = ['Trace','Debug','Information','Warning','Error','Critical']
uni = ['é', 'Ünïcödé', '日本語', 'emoji 😀', 'tab\there', 'nul\x00char', 'nel\u0085x', 'ls x', 'ff\x0cx', '   ']
def lines_tsv(n, cont_prob=0.3, blank_prob=0.05):
    L = []
    for i in range(n):
        L.append(tsv(i, sevs[i % 6], 1 if i % 7 == 0 else 2, f"Record {i} {random.choice(uni)}"))
        while random.random() < cont_prob:
            r = random.random()
            if r < blank_prob * 4: L.append('')
            elif r < 0.3: L.append('   ')
            else: L.append(f"   at Weevil.Method{i}() {random.choice(uni)}")
    return L
def join(L, breaks, trailing):
    s = ''
    for k, line in enumerate(L):
        s += line
        if k < len(L) - 1 or trailing:
            s += random.choice(breaks)
    return s
def write(name, text, enc, bom=True):
    data = text.encode(enc, errors='surrogatepass')
    boms = {'utf-8': b'\xef\xbb\xbf', 'utf-16-le': b'\xff\xfe', 'utf-16-be': b'\xfe\xff', 'utf-32-le': b'\xff\xfe\x00\x00', 'utf-32-be': b'\x00\x00\xfe\xff'}
    with open(os.path.join(out, name), 'wb') as f:
        f.write((boms[enc] if bom else b'') + data)
L = lines_tsv(3000)
for brk, tag in [(['\r\n'], 'crlf'), (['\n'], 'lf'), (['\r'], 'cr'), (['\r\n', '\n', '\r'], 'mixed')]:
    for trailing in (True, False):
        t = join(L, brk, trailing)
        write(f"tsv_{tag}_{'t' if trailing else 'nt'}_utf8.log", t, 'utf-8', bom=False)
        write(f"tsv_{tag}_{'t' if trailing else 'nt'}_utf8bom.log", t, 'utf-8')
        for enc in ['utf-16-le', 'utf-16-be', 'utf-32-le', 'utf-32-be']:
            write(f"tsv_{tag}_{'t' if trailing else 'nt'}_{enc}.log", t, enc)
# preamble before the first record
write("tsv_preamble.log", join(['garbage line'] * 40 + L[:200], ['\r\n'], True), 'utf-8', bom=False)
# plain text (default plugin): every line a record
P = [random.choice(uni) + f" line {i}" if i % 5 else '' for i in range(2000)]
for brk, tag in [(['\r\n'], 'crlf'), (['\n'], 'lf'), (['\r'], 'cr'), (['\r\n', '\n', '\r', '\r\r'], 'mixed')]:
    t = join(P, brk, True)
    write(f"plain_{tag}_utf8.log", t, 'utf-8', bom=False)
    write(f"plain_{tag}_utf16le.log", t, 'utf-16-le')
    write(f"plain_{tag}_utf16be.log", t, 'utf-16-be')
# invalid UTF-8 bytes
raw = b''
for i in range(500):
    line = tsv(i).encode()
    k = i % 6
    if k == 0: line += b'\xc3'                 # truncated 2-byte sequence at end of line
    elif k == 1: line += b' \xff\xfe bad'      # invalid bytes
    elif k == 2: line += b'\xf0\x9f\x98'       # truncated 4-byte sequence
    elif k == 3: line += b'\xe2\x82'           # truncated 3-byte sequence
    elif k == 4: line += b' \xed\xa0\x80'      # encoded surrogate
    raw += line + (b'\r\n' if i % 3 else b'\n')
    if i % 4 == 0: raw += b'   at continuation \xc3\r'
open(os.path.join(out, 'tsv_invalid_utf8.log'), 'wb').write(raw)
# UTF-16 with lone surrogates and an odd trailing byte
t = join(L[:300], ['\r\n', '\n'], True)
data = b'\xff\xfe' + t.encode('utf-16-le')
data = data.replace('Record 5 '.encode('utf-16-le'), 'Record 5 '.encode('utf-16-le') + b'\x00\xd8')  # lone high surrogate
open(os.path.join(out, 'tsv_utf16le_odd.log'), 'wb').write(data + b'\x41')
# edge files
open(os.path.join(out, 'empty.log'), 'wb').write(b'')
open(os.path.join(out, 'bom_only.log'), 'wb').write(b'\xef\xbb\xbf')
open(os.path.join(out, 'one_break.log'), 'wb').write(b'\n')
open(os.path.join(out, 'lone_cr_end.log'), 'wb').write(tsv(1).encode() + b'\r')
# larger than one chunk, so chunks are sealed: about 40 MB UTF-8 and 36 MB UTF-16
B = lines_tsv(600000, cont_prob=0.35)
write('big_mixed_utf8.log', join(B, ['\r\n', '\n', '\r'], True), 'utf-8', bom=False)
write('big_crlf_utf16le.log', join(B[:450000], ['\r\n'], True), 'utf-16-le')
print(len(os.listdir(out)))
