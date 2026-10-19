"""Measure motion between registered Ghost poses; write vector data, never edit sprite pixels."""
from pathlib import Path
import hashlib, json, re, struct, sys
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'AnimalArtSources-20261018/motion-deps'))
import cv2

source=root/'Assets/_Game/Art/AnimalsRealistic/Cats/ghost/walk.png'
meta=source.with_suffix('.png.meta').read_text()
ppu=float(re.search(r'spritePixelsToUnits: ([\d.]+)',meta).group(1))
pattern=r'name: (walk_\d+).*?rect:\s+serializedVersion: \d+\s+x: ([\d.]+)\s+y: ([\d.]+)\s+width: ([\d.]+)\s+height: ([\d.]+).*?pivot: \{x: ([-\d.eE]+), y: ([-\d.eE]+)\}'
frames=[]
for name,x,y,w,h,px,py in re.findall(pattern,meta,re.S):
    x,y,w,h,px,py=map(float,(x,y,w,h,px,py))
    frames.append(dict(name=name,rect=(x,y,w,h),bounds=(-px*w/ppu,-py*h/ppu,w/ppu,h/ppu)))
assert len(frames)==8
left=min(f['bounds'][0] for f in frames);bottom=min(f['bounds'][1] for f in frames)
right=max(f['bounds'][0]+f['bounds'][2] for f in frames);top=max(f['bounds'][1]+f['bounds'][3] for f in frames)
width,height=192,128
xs=left+(np.arange(width,dtype=np.float32)+.5)*(right-left)/width
ys=bottom+(np.arange(height,dtype=np.float32)+.5)*(top-bottom)/height
xx,yy=np.meshgrid(xs,ys)
pixels=np.asarray(Image.open(source).convert('RGBA'))
gray=[]
for frame in frames:
    x,y,w,h=frame['rect'];bx,by,bw,bh=frame['bounds']
    sx=(x+(xx-bx)*ppu-.5).astype(np.float32)
    sy=(pixels.shape[0]-y-(yy-by)*ppu-.5).astype(np.float32)
    sample=cv2.remap(pixels,sx,sy,cv2.INTER_LINEAR,borderMode=cv2.BORDER_CONSTANT)
    inside=(xx>=bx)&(xx<=bx+bw)&(yy>=by)&(yy<=by+bh)
    alpha=sample[:,:,3]/255.0*inside
    gray.append((cv2.cvtColor(sample[:,:,:3],cv2.COLOR_RGB2GRAY)*alpha).astype(np.uint8))
fade=np.clip(((top-bottom)*.5-(yy-bottom))/((top-bottom)*.16),0,1)
vectors=[]
estimator=cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
estimator.setUseSpatialPropagation(True)
for i in range(8):
    forward=estimator.calc(gray[i],gray[(i+1)%8],None)
    backward=estimator.calc(gray[(i+1)%8],gray[i],None)
    for flow,pose in ((forward,gray[i]),(backward,gray[(i+1)%8])):
        # Inverse sampling visits the empty space a paw is moving into. Extend
        # the nearest visible surface's motion there instead of leaving it still.
        foreground=pose>32
        _,labels=cv2.distanceTransformWithLabels((~foreground).astype(np.uint8),cv2.DIST_L2,5,labelType=cv2.DIST_LABEL_PIXEL)
        nearest=np.zeros((int(labels.max())+1,2),np.float32)
        nearest[labels[foreground]]=flow[foreground]
        flow[~foreground]=nearest[labels[~foreground]]
        flow[:,:,0]*=(right-left)/width;flow[:,:,1]*=(top-bottom)/height
        flow[:]*=fade[:,:,None]
    vectors.append(np.concatenate((forward,backward),axis=2).astype('<f4'))
output=root/'Assets/_Game/Resources/Motion/GhostWalk.bytes';output.parent.mkdir(parents=True,exist_ok=True)
with output.open('wb') as stream:
    stream.write(b'GWF1');stream.write(struct.pack('<iii4f',width,height,8,left,bottom,right-left,top-bottom))
    for frame in frames:stream.write(struct.pack('<4f',*frame['bounds']))
    for flow in vectors:stream.write(flow.tobytes())
report={'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'ppu':ppu,'grid':[width,height],'frames':frames,'data_bytes':output.stat().st_size,'max_motion':max(float(np.abs(v).max()) for v in vectors)}
(root/'AnimalArtSources-20261018/ghost-motion-vectors.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'data':str(output),'bytes':output.stat().st_size,'max_motion':report['max_motion']}))
