import sys, re, collections
def load(p):
    cases = collections.OrderedDict(); cur=None
    with open(p, encoding='utf-8') as f:
        for line in f:
            line=line.rstrip('\n')
            if line.startswith('### '):
                cur=line[4:]; cases[cur]=[]
            else:
                line = re.sub(r'CurrentStreamPosition=\d+', 'CurrentStreamPosition=N', line)
                cases[cur].append(line)
    return cases
o=load(sys.argv[1]); n=load(sys.argv[2])
assert list(o.keys())==list(n.keys())
bykind=collections.Counter(); examples={}
for k in o:
    if o[k]!=n[k]:
        m = re.match(r'(\S+) parser=(\S+) start=(\d+) range=(\S+) limit=(\d+) search=(\d+)', k)
        log, parser = m.group(1), m.group(2)
        key=(parser,)
        bykind[key]+=1
        examples.setdefault(key, []).append(k)
for key,c in bykind.most_common():
    print(key, c, examples[key][:3])
