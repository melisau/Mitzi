"""Contact sheet of consecutive source-video poses for matte extraction."""
from PIL import Image
import imageio_ffmpeg
from pathlib import Path
root=Path('D:/Projects/Mitzi/AnimalArtSources-20261018')
source=Path('C:/Users/melisa uyar/Desktop/animasyon paketleri/PixVerse_V6_Image_Text_540P_bu_kedinin_aynı_be.mp4')
r=imageio_ffmpeg.read_frames(str(source)); m=next(r)
sheet=Image.new('RGB',(2048,1200),'white')
for i,raw in enumerate(r):
    if i<12:
        frame=Image.frombytes('RGB',m['size'],raw).crop((0,160,1024,920))
        frame.thumbnail((480,360))
        sheet.paste(frame,((i%4)*512+(512-frame.width)//2,(i//4)*400+(400-frame.height)//2))
sheet.save(root/'black-source-walk-poses.png')
