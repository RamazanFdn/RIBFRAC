import os
import nibabel as nib
import numpy as np

LABEL_DIR = r"C:\ribfrac\data\raw\Dataset001_RibFrac\labelsTr"

files = [f for f in os.listdir(LABEL_DIR) if f.endswith(".nii.gz")]
print(f"Toplam {len(files)} label düzeltilecek...")

for i, fname in enumerate(files):
    path = os.path.join(LABEL_DIR, fname)
    img = nib.load(path)
    data = img.get_fdata()
    
    # 1'den büyük tüm değerleri 1'e dönüştür (hepsi kırık)
    data[data > 1] = 1
    
    new_img = nib.Nifti1Image(data.astype(np.uint8), img.affine, img.header)
    nib.save(new_img, path)
    print(f"[{i+1}/{len(files)}] {fname} düzeltildi")

print("Tamamlandı!")