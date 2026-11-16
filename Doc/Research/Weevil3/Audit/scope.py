"""Map the source files changed since Weevil 2.x to the 3.0 epics (#986, #988, #990).

Each commit since the merge base is assigned to the epics whose issues its message
cites (#N). A file's churn (lines added + deleted) is then split by those epics.
Commits citing no epic issue count as "other" (out of scope).

The issue lists come from the epics' sub-issue trees on GitHub, read on 2026-11-16.

Usage, from the repository root (needs full history and Releases/2.x):
    git fetch --unshallow origin
    git fetch origin "Releases/2.x:refs/remotes/origin/Releases/2.x"
    python3 Doc/Research/Weevil3/Audit/scope.py
"""
import collections
import re
import subprocess

EPICS = {
    '986': [986, 421, 1157, 1177, 1024, 1025, 1026, 1027, 1028, 1162, 1164, 1166, 1168, 1170]
           + list(range(1209, 1221)),
    '988': [988, 977, 978, 958, 961, 1008, 1019, 1020, 1021, 1022, 1023, 1201, 1131, 1133,
            1135, 1136, 1137, 1138, 1139, 1140, 1141, 1150],
    '990': [990, 994, 996, 1004, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1030, 1066,
            1091, 1107, 1055, 1057, 1059, 1062, 1064, 1075, 1077, 1080, 1082, 1095, 1097, 1100,
            1108, 1109],
}
MERGE_BASE = 'baa70a9d'  # git merge-base origin/Releases/2.x main (2.12 release, 2026-06-27)
MIN_EPIC_CHURN = 30

issue_to_epic = {n: e for e, ns in EPICS.items() for n in ns}
log = subprocess.run(
    ['git', 'log', '--no-merges', '--format=@@%H%x00%s%x00%b', '--numstat', f'{MERGE_BASE}..HEAD'],
    capture_output=True, text=True, check=True).stdout

churn = collections.defaultdict(collections.Counter)
commits = collections.Counter()
for block in log.split('@@')[1:]:
    head, _, rest = block.partition('\n')
    lines = rest.split('\n')
    text = head + ' ' + ' '.join(l for l in lines if '\t' not in l)
    epics = {issue_to_epic[int(n)] for n in re.findall(r'#(\d+)', text) if int(n) in issue_to_epic}
    tag = '+'.join(sorted(epics)) or 'other'
    commits[tag] += 1
    for line in lines:
        parts = line.split('\t')
        if len(parts) == 3:
            added, deleted, path = parts
            churn[path][tag] += (int(added) if added.isdigit() else 0) + (int(deleted) if deleted.isdigit() else 0)

tracked = set(subprocess.run(['git', 'ls-files'], capture_output=True, text=True).stdout.split('\n'))
print('Commits by epic:', dict(commits.most_common()))
print(f'{"File":75} {"Lines":>6} {"Epic churn":>10}  Epics')
for path in sorted(churn):
    counts = churn[path]
    epic_churn = sum(v for k, v in counts.items() if k != 'other')
    if not path.startswith('Src/') or path not in tracked or epic_churn < MIN_EPIC_CHURN:
        continue
    with open(path, errors='ignore') as f:
        size = sum(1 for _ in f)
    epics = '/'.join(k for k, _ in counts.most_common() if k != 'other')
    print(f'{path[4:]:75} {size:6} {epic_churn:10}  {epics}')
