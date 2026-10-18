"""Sync only validated walk references, preserving other live catalog fields."""
from pathlib import Path
import re
import shutil
live=Path('D:/Projects/Mitzi')
test=Path('D:/Projects/Mitzi-AnimalValidation-20261018')
rel=Path('Assets/_Game/Art/AnimalsRealistic/Cats')
for color in ['black','silver','calico','greywhite']:
    for source in (test/rel/color).iterdir():
        if source.name.startswith('videowalk_') or source.name in [color+'_walk.anim',color+'_walk.anim.meta']:
            shutil.copy2(source,live/rel/color/source.name)
catalog=Path('Assets/_Game/Content/PawPathCatalog.asset')
pattern=r'(?m)^--- !u!'
validated=re.split(pattern,(test/catalog).read_text(encoding='utf-8'))
refs={}
for doc in validated:
    cat=re.search(r'(?m)^  id: (mitzi|tarcin|pamuk|ada|komur|moka)$',doc)
    walk=re.search(r'(?m)^  walkFrames:\n(?:  -[^\n]*\n)+',doc)
    if cat and walk: refs[cat[1]]=walk[0]
assert len(refs)==6,refs.keys()
docs=re.split(pattern,(live/catalog).read_text(encoding='utf-8'))
for i,doc in enumerate(docs):
    cat=re.search(r'(?m)^  id: (mitzi|tarcin|pamuk|ada|komur|moka)$',doc)
    if cat:
        docs[i],n=re.subn(r'(?m)^  walkFrames:\n(?:  -[^\n]*\n)+',lambda _:refs[cat[1]],doc)
        assert n==1,cat[1]
(live/catalog).write_text('--- !u!'.join(docs),encoding='utf-8')
print('Synced six verified walking sequences; other catalog fields preserved.')
