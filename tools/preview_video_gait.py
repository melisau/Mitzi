"""Compare the former atlas with the pose-preserving video gait."""
from pathlib import Path
import json
from PIL import Image, ImageDraw
import numpy as np

root=Path('D:/Projects/Mitzi')
art=root/'Assets/_Game/Art/AnimalsRealistic/Cats'
out=root/'AnimalArtSources-20261018'
report={}
for kind in ['black','silver','calico','greywhite']:
    series={}
    for prefix in ['walk_','videowalk_']:
        frames=[Image.open(p).convert('RGBA') for p in sorted((art/kind).glob(prefix+'*.png'))]
        # Fur color does not affect the silhouette measurement. Track the forward
        # paw at contact height; a complete stride retracts under the shoulder.
        xs=[int(np.where(np.array(im)[:,:,3][280:,:]>96)[1].max()) for im in frames]
        series[prefix]={'front_contact_x':xs,'excursion_px':max(xs)-min(xs)}
    assert series['videowalk_']['excursion_px']>=35,(kind,series)
    report[kind]=series
(out/'video-gait-analysis.json').write_text(json.dumps(report,indent=2))

frames=[]
for n in range(72):
    canvas=Image.new('RGB',(640,350),'#aab7bd')
    draw=ImageDraw.Draw(canvas)
    for col,prefix in enumerate(['walk_','videowalk_']):
        paths=sorted((art/'black').glob(prefix+'*.png'))
        rate=len(paths)/0.75 if col==0 else 24
        index=int(n/24*rate)%len(paths)
        sprite=Image.open(paths[index]).convert('RGBA')
        canvas.paste(sprite,(col*320,25),sprite)
        draw.text((col*320+15,8),'Eski' if col==0 else 'Yeni',fill='#25343a')
        draw.line((col*320,319,col*320+319,319),fill='#5a666b',width=1)
    frames.append(canvas)
times=[10*(round((i+1)*300/72)-round(i*300/72)) for i in range(72)]
frames[0].save(out/'walk-before-after.gif',save_all=True,append_images=frames[1:],duration=times,loop=0)
print(json.dumps({k:{p:v['excursion_px'] for p,v in s.items()} for k,s in report.items()}))
