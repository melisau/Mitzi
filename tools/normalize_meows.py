"""Measure/level the three shipped meows; requires FFmpeg (or imageio-ffmpeg).

Default: validate only. Use --apply for a deliberate source-asset update.
All clips become 48 kHz stereo PCM16 before measuring, so mono/stereo playback
layout cannot bias the comparison. Only a constant gain is applied: no limiter,
compressor, trimming, EQ, or pitch changes.
"""
import argparse
import array
import json
import math
from pathlib import Path
import shutil
import subprocess
import tempfile
import wave

ROOT = Path(__file__).resolve().parents[1]
NAMES = ("cat_meow2.wav", "cat_meow3.wav", "cat_meow4.wav")
TARGET_LUFS = -22.0
MAX_TRUE_PEAK = -6.0


def run(ffmpeg, *args):
    result = subprocess.run([ffmpeg, "-hide_banner", "-nostats", *map(str, args)],
                            capture_output=True, text=True)
    if result.returncode:
        raise RuntimeError(result.stderr)
    return result.stderr


def measure(ffmpeg, path):
    output = run(ffmpeg, "-i", path, "-af",
                 "loudnorm=I=-22:TP=-6:LRA=11:print_format=json", "-f", "null", "-")
    stats = json.loads(output[output.rfind("{"):output.rfind("}") + 1])
    with wave.open(str(path), "rb") as audio:
        return {"lufs": float(stats["input_i"]), "true_peak_dbtp": float(stats["input_tp"]),
                "rate": audio.getframerate(), "channels": audio.getnchannels(),
                "seconds": audio.getnframes() / audio.getframerate()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--ffmpeg")
    parser.add_argument("--report", type=Path)
    parser.add_argument("--preview", type=Path)
    parser.add_argument("--backup-dir", type=Path)
    args = parser.parse_args()
    ffmpeg = args.ffmpeg or shutil.which("ffmpeg")
    if not ffmpeg:
        import imageio_ffmpeg
        ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    records = []
    # Stage every candidate and validate the complete set before replacing any asset.
    with tempfile.TemporaryDirectory(prefix="mitzi-meows-") as temp:
        staged = []
        for name in NAMES:
            path = ROOT / "Assets" / name
            before = measure(ffmpeg, path)
            if args.apply:
                converted = Path(temp) / ("stereo-" + name)
                # PCM24 retains precision across resampling and the gain measurement.
                run(ffmpeg, "-y", "-i", path, "-ac", 2, "-ar", 48000,
                    "-c:a", "pcm_s24le", converted)
                stereo = measure(ffmpeg, converted)
                gain = TARGET_LUFS - stereo["lufs"]
                if not math.isfinite(gain) or stereo["true_peak_dbtp"] + gain > MAX_TRUE_PEAK:
                    raise ValueError(f"{name}: target cannot be reached safely with linear gain")
                candidate = Path(temp) / name
                run(ffmpeg, "-y", "-i", converted, "-af", f"volume={gain:.6f}dB",
                    "-c:a", "pcm_s16le", candidate)
                after = measure(ffmpeg, candidate)
            else:
                candidate, after, gain = path, before, 0.0
            if (abs(after["lufs"] - TARGET_LUFS) > .15 or after["true_peak_dbtp"] > MAX_TRUE_PEAK
                    or after["channels"] != 2 or after["rate"] != 48000):
                raise ValueError(f"{name}: invalid normalized result: {after}")
            if abs(after["seconds"] - before["seconds"]) > 1 / 48000 + 1e-6:
                raise ValueError(f"{name}: duration changed")
            records.append({"file": name, "before": before, "gain_db": gain, "after": after})
            staged.append((path, candidate))
        spread = max(r["after"]["lufs"] for r in records) - min(r["after"]["lufs"] for r in records)
        if spread > .2:
            raise ValueError(f"Meow loudness spread too large: {spread}")
        if args.apply:
            for path, candidate in staged:
                if args.backup_dir:
                    args.backup_dir.mkdir(parents=True, exist_ok=True)
                    backup = args.backup_dir / path.name
                    if not backup.exists():
                        shutil.copy2(path, backup)
                shutil.copyfile(candidate, path)
    report = {"target_lufs": TARGET_LUFS, "maximum_true_peak_dbtp": MAX_TRUE_PEAK,
              "loudness_spread_lu": spread, "clips": records}
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    if args.preview:
        args.preview.parent.mkdir(parents=True, exist_ok=True)
        with wave.open(str(args.preview), "wb") as preview:
            preview.setparams((2, 2, 48000, 0, "NONE", "not compressed"))
            preview.writeframes(bytes(48000 * 4 // 2))
            for _ in range(2):
                for name in NAMES:
                    with wave.open(str(ROOT / "Assets" / name), "rb") as clip:
                        samples = array.array("h", clip.readframes(clip.getnframes()))
                    # Match the game's shared 0.8 effects/meow gain without a player
                    # applying separate loudness normalization to individual clips.
                    preview.writeframes(array.array("h", (round(value * .8) for value in samples)).tobytes())
                    preview.writeframes(bytes(round(48000 * .6) * 4))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
