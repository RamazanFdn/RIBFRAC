import os
import shutil
import pandas as pd

RAW_DIR = r"C:\ribfrac\data\raw\Part1"
OUTPUT_DIR = r"C:\ribfrac\data\nnunet\Dataset001_RibFrac"

IMAGE_TR = os.path.join(OUTPUT_DIR, "imagesTr")
LABEL_TR = os.path.join(OUTPUT_DIR, "labelsTr")

os.makedirs(IMAGE_TR, exist_ok=True)
os.makedirs(LABEL_TR, exist_ok=True)

print("Dosyalar taşınıyor...")
count = 0
for fname in os.listdir(RAW_DIR):
    src = os.path.join(RAW_DIR, fname)
    if fname.endswith("-image.nii.gz"):
        case_id = fname.replace("-image.nii.gz", "")
        dst = os.path.join(IMAGE_TR, f"{case_id}_0000.nii.gz")
        shutil.copy2(src, dst)
        count += 1
        print(f"  Görüntü: {fname} → {os.path.basename(dst)}")
    elif fname.endswith("-label.nii.gz"):
        case_id = fname.replace("-label.nii.gz", "")
        dst = os.path.join(LABEL_TR, f"{case_id}.nii.gz")
        shutil.copy2(src, dst)
        print(f"  Maske:   {fname} → {os.path.basename(dst)}")

print(f"\nTamamlandı! {count} görüntü taşındı.")