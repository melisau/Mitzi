"""Slice generated transparent animation sheets without replacing source references.

Only atlas extraction, uniform resampling and registration are performed here.
Background extraction and pose reconstruction are supplied by imagegen alpha.
"""
from pathlib import Path
import json
import shutil
from PIL import Image, ImageDraw
import numpy as np
from scipy import ndimage

PROJECT = Path(__file__).resolve().parents[1]
ROOT = PROJECT / 'Assets/_Game/Art/AnimalsRealistic'
SOURCES = PROJECT / 'AnimalArtSources-20261018'
SIZE = 320

def cells(path, columns, rows):
    im = Image.open(path).convert('RGBA')
    pixels = np.array(im)
    labels, _ = ndimage.label(pixels[:,:,3] > 96)
    components = {}
    candidates = []
    for label, bounds in enumerate(ndimage.find_objects(labels),1):
        if bounds is None:
            continue
        ys,xs = bounds
        if (xs.stop-xs.start)*(ys.stop-ys.start) < 4000:
            continue
        candidates.append((label,bounds))
    candidates.sort(key=lambda item:(item[1][0].start+item[1][0].stop)*.5)
    assert len(candidates)==columns*rows, (path,len(candidates))
    for row in range(rows):
        group=sorted(candidates[row*columns:(row+1)*columns],key=lambda item:item[1][1].start)
        for col,item in enumerate(group): components[row,col]=item
    assert len(components)==columns*rows, (path,len(components))
    result = []
    for row in range(rows):
        for col in range(columns):
            label,(ys,xs) = components[row,col]
            assert xs.start>1 and ys.start>1 and xs.stop<im.width-1 and ys.stop<im.height-1, ('Source clipped',path,row,col)
            x0,y0,x1,y1=max(0,xs.start-4),max(0,ys.start-4),min(im.width,xs.stop+4),min(im.height,ys.stop+4)
            # Extract the entire sprite, even when it crosses a nominal grid line.
            # Keep its alpha fringe, excluding parts belonging to adjacent sprites.
            mask=ndimage.binary_dilation(labels[y0:y1,x0:x1]==label,iterations=4)
            tile=pixels[y0:y1,x0:x1].copy()
            tile[:,:,3]=np.where(mask,tile[:,:,3],0)
            result.append(Image.fromarray(tile))
    return result

def register(images, ground=True, preferred_scale=None):
    # One scale for the complete sequence prevents body-size pulsing.
    scale = min((SIZE-32)/max(im.width for im in images),
                (SIZE-48)/max(im.height for im in images))
    if preferred_scale is not None:
        scale = min(scale, preferred_scale)
    result = []
    for im in images:
        frame = Image.new('RGBA', (SIZE,SIZE))
        resized = im.resize((round(im.width*scale),round(im.height*scale)), Image.Resampling.LANCZOS)
        y = round(SIZE*.92)-resized.height if ground else (SIZE-resized.height)//2
        frame.alpha_composite(resized, ((SIZE-resized.width)//2,y))
        result.append(frame)
    return result

def save_frames(folder, names, frames):
    folder.mkdir(parents=True, exist_ok=True)
    for name, im in zip(names, frames):
        im.save(folder / f'{name}.png')

def cat(kind, source):
    raw = cells(source,6,4)
    frames = register(raw)
    names = [f'walk_{i:02}' for i in range(1,13)] + [
        'crouch_01','jump_02','jump_03','jump_04_land','idle_01','sit_01',
        'lie_01','sleep_01','eat_01','climb_01','climb_02','climb_03']
    folder = ROOT/'Cats'/kind
    save_frames(folder,names,frames)
    # Compatibility aliases for existing home-animation consumers.
    aliases = {'jump_01':'crouch_01','idle_02':'idle_01','sit_02':'sit_01',
               'climb_04':'climb_02', **{f'eatanim_{i:02}':'eat_01' for i in range(1,5)}}
    for target, original in aliases.items():
        shutil.copy2(folder/f'{original}.png',folder/f'{target}.png')
    preview(kind,frames[:12],63)

def preview(name,frames,duration):
    SOURCES.mkdir(exist_ok=True)
    bgframes=[]
    for im in frames:
        bg=Image.new('RGBA',im.size,'#aab7bd')
        bg.alpha_composite(im)
        bgframes.append(bg.convert('RGB'))
    bgframes[0].save(SOURCES/f'{name}.gif',save_all=True,append_images=bgframes[1:],
                     duration=duration,loop=0)

def animals(source):
    raw=cells(source,4,8)
    for row,kind in enumerate(['Dog','RobotDog','Bird','RobotBird']):
        frames=register(raw[row*8:(row+1)*8],ground=row<2)
        prefix='dog_run' if row<2 else 'bird_fly'
        save_frames(ROOT/kind,[f'{prefix}_{i:02}' for i in range(1,9)],frames)
        preview(kind,frames,63 if row<2 else 42)

def actions(kind, source):
    raw = cells(source,4,6)
    folder = ROOT/'Cats'/kind
    standing = Image.open(folder/'idle_01.png').getchannel('A').point(lambda a:255 if a>96 else 0).getbbox()
    preferred = (standing[3]-standing[1])/np.median([im.height for im in raw[12:16]])
    frames = register(raw, preferred_scale=preferred)
    for row, prefix in enumerate(['crouchwalk','eatanim','drink','idle','sleep','climb']):
        sequence=frames[row*4:row*4+4]
        save_frames(folder,[f'{prefix}_{i:02}' for i in range(1,5)],sequence)
        preview(kind+'-'+prefix,sequence,125 if row<3 or row==5 else 500)
    shutil.copy2(folder/'crouchwalk_01.png',folder/'crouch_01.png')
    shutil.copy2(folder/'eatanim_01.png',folder/'eat_01.png')

def walk_cycle(kind, source, columns=4, rows=6, prefix='walk'):
    raw=cells(source,columns,rows)
    folder=ROOT/'Cats'/kind
    standing=Image.open(folder/'idle_01.png').getchannel('A').point(lambda a:255 if a>96 else 0).getbbox()
    preferred=(standing[3]-standing[1])/np.median([im.height for im in raw])
    frames=register(raw,preferred_scale=preferred)
    save_frames(folder,[f'{prefix}_{i:02}' for i in range(1,len(frames)+1)],frames)
    # GIF stores centiseconds; alternate 10/20ms to preserve the 750ms cycle.
    durations=[10*(round((i+1)*75/len(frames))-round(i*75/len(frames))) for i in range(len(frames))]
    preview(kind+'-walk',frames,durations)

def validate():
    files=list(ROOT.rglob('*.png'))
    for path in files:
        im=Image.open(path)
        assert im.mode=='RGBA' and im.size==(SIZE,SIZE),path
        a=im.getchannel('A')
        assert a.getextrema()[0]==0 and a.getextrema()[1]>200,path
        # Silhouettes must have margin on every side.
        b=a.point(lambda x: 255 if x>24 else 0).getbbox()
        assert b and min(b[0],b[1],SIZE-b[2],SIZE-b[3])>=8,(path,b)
    (SOURCES/'asset-validation.json').write_text(json.dumps({'png_count':len(files),
        'canvas':[SIZE,SIZE],'transparent':True,'minimum_margin_px':8},indent=2))
    print(f'Validated {len(files)} transparent registered sprites')

def upright_climb(kind, source):
    frames=register(cells(source,3,4))
    save_frames(ROOT/'Cats'/kind,[f'climb_{i:02}' for i in range(1,13)],frames)
    preview(kind+'-upright-climb',frames,[40,40,50]*4)

if __name__=='__main__':
    import argparse
    p=argparse.ArgumentParser()
    p.add_argument('--cat',nargs=2,action='append',default=[])
    p.add_argument('--animals')
    p.add_argument('--actions',nargs=2,action='append',default=[])
    p.add_argument('--walk',nargs=2,action='append',default=[])
    p.add_argument('--walk-grid',nargs=2,type=int,default=[4,6])
    p.add_argument('--walk-prefix',default='walk')
    p.add_argument('--dog-idle')
    p.add_argument('--upright-climb',nargs=2,action='append',default=[])
    args=p.parse_args()
    for kind,path in args.cat: cat(kind,path)
    if args.animals: animals(args.animals)
    for kind,path in args.actions: actions(kind,path)
    for kind,path in args.walk: walk_cycle(kind,path,*args.walk_grid,args.walk_prefix)
    for kind,path in args.upright_climb: upright_climb(kind,path)
    if args.dog_idle:
        for kind,pose in zip(['Dog','RobotDog'],cells(args.dog_idle,2,1)):
            save_frames(ROOT/kind,['dog_idle'],register([pose]))
    validate()
