"""Extract video reference contact sheets without modifying the video."""
from pathlib import Path
import sys
import imageio_ffmpeg
from PIL import Image, ImageDraw

source, output = sys.argv[1:3]
reader=imageio_ffmpeg.read_frames(source, pix_fmt='rgb24')
meta=next(reader)
print(meta)
fps=meta['fps']; duration=meta['duration']; w,h=meta['size']
indices={round(i*fps*duration/24):i for i in range(24)}
sheet=Image.new('RGB',(4*384,6*238),'#ddd')
draw=ImageDraw.Draw(sheet)
for n,raw in enumerate(reader):
    if n not in indices: continue
    i=indices[n]
    im=Image.frombytes('RGB',(w,h),raw); im.thumbnail((384,216))
    sheet.paste(im,((i%4)*384,(i//4)*238))
    draw.text(((i%4)*384+5,(i//4)*238+218),f'{n/fps:.3f}s',fill='black')
sheet.save(output)
