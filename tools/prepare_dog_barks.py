"""Prepare the supplied natural/robot dog recordings, preserving the originals."""
from pathlib import Path
import argparse
import array
import json
import subprocess
import wave
import imageio_ffmpeg

parser = argparse.ArgumentParser()
parser.add_argument("natural", type=Path)
parser.add_argument("robot", type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
destination = root / "Assets/_Game/Resources/Audio"
destination.mkdir(parents=True, exist_ok=True)
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
report = []
for source, name, duration in [(args.natural, "DogBark", 1.032), (args.robot, "RobotDogBark", 2.5)]:
    output = destination / (name + ".wav")
    filters = f"volume=-6dB,afade=t=in:st=0:d=0.02,afade=t=out:st={duration-.12}:d=0.12"
    subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-i", str(source),
                    "-t", str(duration), "-af", filters, "-ac", "1", "-ar", "48000", "-c:a", "pcm_s16le", str(output)], check=True)
    with wave.open(str(output), "rb") as audio:
        samples = array.array("h", audio.readframes(audio.getnframes()))
        peak = max(abs(value) for value in samples) / 32768
        assert peak < .52, "Peak headroom lost"
        report.append({"file": str(output.relative_to(root)), "seconds": len(samples)/48000,
                       "peak": peak, "clipped_samples": sum(abs(value) >= 32767 for value in samples), "source": str(source)})
print(json.dumps(report, indent=2))
