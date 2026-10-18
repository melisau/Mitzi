"""Sync validated climbing/audio references without changing other catalog data."""
from pathlib import Path
import re
import shutil
live=Path('D:/Projects/Mitzi')
test=Path('D:/Projects/Mitzi-AnimalValidation-20261018')
rel=Path('Assets/_Game/Art/AnimalsRealistic/Cats')
for color in ['black','silver','calico','greywhite']:
    for source in (test/rel/color).iterdir():
        if source.name.startswith('climb_') or source.name in [color+'_climb.anim',color+'_climb.anim.meta']:
            shutil.copy2(source,live/rel/color/source.name)
catalog=Path('Assets/_Game/Content/PawPathCatalog.asset')
split=r'(?m)^--- !u!'
validated=re.split(split,(test/catalog).read_text(encoding='utf-8'))
refs={}; common=None; purr=None
for doc in validated:
    cat=re.search(r'(?m)^  id: (mitzi|tarcin|pamuk|ada|komur|moka)$',doc)
    climb=re.search(r'(?m)^  climbFrames:\n(?:  -[^\n]*\n)+',doc)
    if cat and climb: refs[cat[1]]=climb[0]
    if '  climbingCatFrames:' in doc:
        common=re.search(r'(?m)^  climbingCatFrames:\n(?:  -[^\n]*\n)+',doc)[0]
        purr=re.search(r'(?m)^  catPurrSfx:[^\n]*\n',doc)[0]
assert len(refs)==6 and common and purr
docs=re.split(split,(live/catalog).read_text(encoding='utf-8'))
for i,doc in enumerate(docs):
    cat=re.search(r'(?m)^  id: (mitzi|tarcin|pamuk|ada|komur|moka)$',doc)
    if cat:
        doc,n=re.subn(r'(?m)^  climbFrames:\n(?:  -[^\n]*\n)+',lambda _:refs[cat[1]],doc)
        assert n==1
    if '  climbingCatFrames:' in doc:
        doc=re.sub(r'(?m)^  climbingCatFrames:\n(?:  -[^\n]*\n)+',lambda _:common,doc)
        if '  catPurrSfx:' in doc:
            doc=re.sub(r'(?m)^  catPurrSfx:[^\n]*\n',lambda _:purr,doc)
        else:
            doc=re.sub(r'(?m)^(  catStepSfx:[^\n]*\n)',lambda m:m[0]+purr,doc)
    docs[i]=doc
(live/catalog).write_text('--- !u!'.join(docs),encoding='utf-8')
print('Synced six climbing sequences, fallback and purr; other catalog data preserved.')
