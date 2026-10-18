"""Render diagnostic GIFs from the registered walking sprites."""
from PIL import Image
import prepare_realistic_animals as pack

for kind in ['silver','calico','greywhite','black']:
    paths=sorted((pack.ROOT/'Cats'/kind).glob('walk_*.png'))
    frames=[Image.open(path).convert('RGBA') for path in paths]
    times=[10*(round((i+1)*75/len(frames))-round(i*75/len(frames))) for i in range(len(frames))]
    pack.preview(kind+'-walk',frames,times)
    print(kind,len(frames),sum(times))
