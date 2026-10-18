"""Copy validated walking import scales and crouch tutorial references only."""
from pathlib import Path
import re
import shutil
live=Path('D:/Projects/Mitzi')
test=Path('D:/Projects/Mitzi-AnimalValidation-20261018')
rel=Path('Assets/_Game/Art/AnimalsRealistic/Cats')
for color in ['black','silver','calico','greywhite']:
    sources=list((test/rel/color).glob('videowalk_*.png.meta'))
    assert len(sources)==12
    for source in sources:
        dest=live/rel/color/source.name
        assert re.search(r'guid: (.+)',dest.read_text())[1]==re.search(r'guid: (.+)',source.read_text())[1]
        shutil.copy2(source,dest)
catalog=Path('Assets/_Game/Content/PawPathCatalog.asset')
split=r'(?m)^--- !u!'
pattern=r'(?m)^  crouchWalkFrames:\n(?:  -[^\n]*\n)+'
refs={}
for doc in re.split(split,(test/catalog).read_text(encoding='utf-8')):
    cat=re.search(r'(?m)^  id: (mitzi|tarcin|pamuk|ada|komur|moka)$',doc)
    frames=re.search(pattern,doc)
    if cat and frames: refs[cat[1]]=frames[0]
assert len(refs)==6
docs=re.split(split,(live/catalog).read_text(encoding='utf-8'))
for i,doc in enumerate(docs):
    cat=re.search(r'(?m)^  id: (mitzi|tarcin|pamuk|ada|komur|moka)$',doc)
    if cat:
        if '  crouchWalkFrames:' in doc:
            doc=re.sub(r'(?m)^  crouchWalkFrames:(?: \[\])?\n(?:  -[^\n]*\n)*',lambda _:refs[cat[1]],doc)
        else:
            doc=doc.replace('  idleFrames:',refs[cat[1]]+'  idleFrames:',1)
    docs[i]=doc
(live/catalog).write_text('--- !u!'.join(docs),encoding='utf-8')
for name in ['tutorial-draw','tutorial-tools','tutorial-views','tutorial-move','tutorial-jump','tutorial-crouch','tutorial-climb','how-to-draw','how-to-controls','idle-walk-size-comparison']:
    shutil.copy2(test/'AnimalArtSources-20261018'/(name+'.png'),live/'AnimalArtSources-20261018'/(name+'.png'))
print('Synced 48 walking import scales and six crouch references; preserved GUIDs and other catalog fields.')
