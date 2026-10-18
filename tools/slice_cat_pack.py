"""Map PACK 64x64 grids to game frames and slice into project folders."""
from PIL import Image
import numpy as np
import os
import shutil

PACK = r"C:\Users\melisa uyar\Downloads\PACK\PACK"
OUT_ROOT = r"d:\Projects\Mitzi\Assets\_Game\Art\Cats\_Pack"
CELL = 64

# Clean grid files (prefer these over "with text")
SHEETS = {
    "black": None,
    "ginger": None,
    "white": None,
}

for f in os.listdir(PACK):
    if not f.endswith(".png"):
        continue
    low = f.lower()
    path = os.path.join(PACK, f)
    if low.startswith("cat 1"):
        SHEETS["black"] = path
    elif low.startswith("cat 2"):
        SHEETS["ginger"] = path
    elif low.startswith("cat 3"):
        SHEETS["white"] = path

print("Sheets:", {k: os.path.basename(v) if v else None for k, v in SHEETS.items()})

# Row layout: 0 sit, 1 stand, 2-5 walk, 6-7 bridge, 8+ labeled actions (run..hind).

LABELS = [
    "walk_down", "walk_up", "walk_right", "walk_left",
    "run_down", "run_up", "run_right", "run_left",
    "lick_paw_sit_front", "lick_paw_lie_front",
    "meow_sit_front", "meow_lie_front", "meow_stand_front",
    "scratch_sit_left", "scratch_sit_right",
    "tailwag_sit_front", "tailwag_sit_back", "tailwag_sit_left", "tailwag_sit_right",
    "tailwag_stand_front", "tailwag_stand_back", "tailwag_stand_left", "tailwag_stand_right",
    "tailwag_lie_left", "tailwag_lie_right",
    "swipe_r_stand_front", "swipe_l_stand_front", "swipe_stand_back",
    "swipe_l_stand_left", "swipe_r_stand_left", "swipe_l_stand_right", "swipe_r_stand_right",
    "swipe_r_sit_front", "swipe_l_sit_front", "swipe_sit_back",
    "swipe_l_sit_left", "swipe_r_sit_left", "swipe_l_sit_right", "swipe_r_sit_right",
    "yawn_sit_front",
    "sleep1_left_front", "sleep1_right_front", "sleep1_left_back", "sleep1_right_back",
    "sleep2_left_front", "sleep2_right_front",
    "sleep3_left_front", "sleep3_right_front",
    "sleep4_left_front", "sleep4_right_front",
    "sleep5_left_front", "sleep5_right_front",
    "eat_stand_front", "eat_stand_back", "eat_stand_left", "eat_stand_right",
    "hiss_front_left", "hiss_front_right",
    "jump_back", "jump_left", "jump_right",
    "hind_legs",
]

DIRS4 = ["front", "back", "left", "right"]


def occupied_cols(arr, row):
    cols = []
    for c in range(arr.shape[1] // CELL):
        patch = arr[row * CELL : (row + 1) * CELL, c * CELL : (c + 1) * CELL]
        if ((patch[:, :, 0] > 8) | (patch[:, :, 1] > 8) | (patch[:, :, 2] > 8)).sum() > 5:
            cols.append(c)
    return cols


def black_to_alpha(im):
    """Near-black background -> transparent."""
    arr = np.array(im.convert("RGBA"))
    # Treat very dark pixels as transparent (sprite sheets use solid black BG)
    mask = (arr[:, :, 0] < 8) & (arr[:, :, 1] < 8) & (arr[:, :, 2] < 8)
    arr[mask, 3] = 0
    return Image.fromarray(arr)


def save_cell(arr, row, col, path):
    patch = arr[row * CELL : (row + 1) * CELL, col * CELL : (col + 1) * CELL].copy()
    im = black_to_alpha(Image.fromarray(patch))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)


def slice_sheet(color, src_path):
    im = Image.open(src_path).convert("RGBA")
    arr = np.array(im)
    rows = arr.shape[0] // CELL
    dest = os.path.join(OUT_ROOT, color)
    if os.path.isdir(dest):
        shutil.rmtree(dest)
    os.makedirs(dest, exist_ok=True)

    # Idle sit / stand
    for i, d in enumerate(DIRS4):
        save_cell(arr, 0, i, os.path.join(dest, f"sit_{d}.png"))
        save_cell(arr, 1, i, os.path.join(dest, f"stand_{d}.png"))

    # Side run (unlabeled row 6) + bridge row 7
    for r in (6, 7):
        prefix = "run_side" if r == 6 else f"extra_r{r}"
        for i, c in enumerate(occupied_cols(arr, r)):
            save_cell(arr, r, c, os.path.join(dest, f"{prefix}_{i+1:02d}.png"))

    # Labeled rows: label i -> grid row
    # walk 0-3 -> rows 2-5; then skip 6-7; run+ 4.. -> rows 8..
    for li, name in enumerate(LABELS):
        if li <= 3:
            grow = 2 + li
        else:
            grow = 8 + (li - 4)
        if grow >= rows:
            print(f"  WARN {color}: {name} row {grow} out of range")
            continue
        cols = occupied_cols(arr, grow)
        if not cols:
            print(f"  WARN {color}: {name} row {grow} empty")
            continue
        for i, c in enumerate(cols):
            save_cell(arr, grow, c, os.path.join(dest, f"{name}_{i+1:02d}.png"))

    # Game-facing aliases expected by animation builder (side-scroller)
    aliases = []

    def copy_alias(src_name, dst_name):
        src = os.path.join(dest, src_name)
        dst = os.path.join(dest, dst_name)
        if os.path.isfile(src):
            shutil.copy2(src, dst)
            aliases.append(dst_name)
            return True
        return False

    # Walk: prefer walk_right (game flips via scale.x)
    for i in range(1, 7):
        copy_alias(f"walk_right_{i:02d}.png", f"walk_{i:02d}.png")
    # If fewer than 6, pad from run_side
    for i in range(1, 25):
        if not os.path.isfile(os.path.join(dest, f"walk_{i:02d}.png")):
            # loop walk_right
            src_i = ((i - 1) % 6) + 1
            if not copy_alias(f"walk_right_{src_i:02d}.png", f"walk_{i:02d}.png"):
                break

    # Idle
    copy_alias("stand_right.png", "idle_01.png")
    copy_alias("stand_front.png", "idle_02.png")
    if not os.path.isfile(os.path.join(dest, "idle_01.png")):
        copy_alias("sit_right.png", "idle_01.png")

    # Jump: jump_right frames + land from last
    jump_frames = sorted(
        f for f in os.listdir(dest) if f.startswith("jump_right_") and f.endswith(".png")
    )
    for i, f in enumerate(jump_frames[:3], 1):
        copy_alias(f, f"jump_{i:02d}.png")
    if jump_frames:
        copy_alias(jump_frames[-1], "jump_04_land.png")
    # ensure at least something
    for i, name in enumerate(["jump_01.png", "jump_02.png", "jump_03.png", "jump_04_land.png"], 1):
        if not os.path.isfile(os.path.join(dest, name)):
            copy_alias("stand_right.png", name)

    # Crouch / Sit / Lie / Sleep / Eat / Climb
    copy_alias("sit_right.png", "crouch_01.png")
    copy_alias("sit_right.png", "sit_01.png")
    copy_alias("sit_front.png", "sit_02.png")
    copy_alias("lick_paw_lie_front_01.png", "lie_01.png") or copy_alias(
        "sleep1_right_front_01.png", "lie_01.png"
    )
    copy_alias("sleep1_right_front_01.png", "sleep_01.png")
    copy_alias("eat_stand_right_01.png", "eat_01.png")
    for i in range(1, 5):
        src = f"eat_stand_right_{i:02d}.png"
        if not copy_alias(src, f"eatanim_{i:02d}.png"):
            # cycle available eat frames
            eats = sorted(f for f in os.listdir(dest) if f.startswith("eat_stand_right_"))
            if eats:
                copy_alias(eats[(i - 1) % len(eats)], f"eatanim_{i:02d}.png")
    # Climb from hind legs
    hind = sorted(f for f in os.listdir(dest) if f.startswith("hind_legs_") and f.endswith(".png"))
    for i in range(1, 5):
        if hind:
            copy_alias(hind[(i - 1) % len(hind)], f"climb_{i:02d}.png")
        else:
            copy_alias("stand_back.png", f"climb_{i:02d}.png")

    n = len([f for f in os.listdir(dest) if f.endswith(".png")])
    print(f"{color}: {n} frames -> {dest}")
    return dest


def main():
    os.makedirs(OUT_ROOT, exist_ok=True)
    # Also copy original sheets for reference (disabled-use documentation)
    ref = os.path.join(OUT_ROOT, "_sheets")
    os.makedirs(ref, exist_ok=True)
    for color, path in SHEETS.items():
        if path:
            slice_sheet(color, path)
            shutil.copy2(path, os.path.join(ref, f"{color}_64.png"))
    # Copy labeled sheets too
    for f in os.listdir(PACK):
        if "with text" in f.lower() and f.endswith(".png"):
            shutil.copy2(os.path.join(PACK, f), os.path.join(ref, f))
    print("Done.")


if __name__ == "__main__":
    main()
