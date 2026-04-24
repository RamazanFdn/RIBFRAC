import nibabel as nib
import numpy as np
import matplotlib.pyplot as plt

# Dosyaları yükle
ct = nib.load(r"C:\ribfrac\data\test_input\RibFrac1_0000.nii.gz")
pred = nib.load(r"C:\ribfrac\data\test_output\RibFrac1.nii.gz")

ct_data = ct.get_fdata()
pred_data = pred.get_fdata()

# Kırık olan sliceları bul
kirık_sliceler = []
for i in range(pred_data.shape[2]):
    if pred_data[:, :, i].sum() > 0:
        kirık_sliceler.append(i)

print(f"Kırık tespit edilen slice sayısı: {len(kirık_sliceler)}")
print(f"Kırık slice indeksleri: {kirık_sliceler[:10]}")

# İlk kırık slice'ı göster
if kirık_sliceler:
    idx = kirık_sliceler[len(kirık_sliceler)//2]
    
    ct_slice = ct_data[:, :, idx]
    pred_slice = pred_data[:, :, idx]
    
    # Normalize
    ct_slice = np.clip(ct_slice, -1000, 3000)
    ct_slice = (ct_slice - ct_slice.min()) / (ct_slice.max() - ct_slice.min())
    
    fig, axes = plt.subplots(1, 3, figsize=(15, 5))
    
    # CT görüntüsü
    axes[0].imshow(ct_slice.T, cmap='gray', origin='lower')
    axes[0].set_title('CT Görüntüsü')
    axes[0].axis('off')
    
    # Tahmin maskesi
    axes[1].imshow(pred_slice.T, cmap='hot', origin='lower')
    axes[1].set_title('Kırık Tespiti')
    axes[1].axis('off')
    
    # Üst üste bindirilmiş
    axes[2].imshow(ct_slice.T, cmap='gray', origin='lower')
    axes[2].imshow(pred_slice.T, cmap='Reds', alpha=0.5, origin='lower')
    axes[2].set_title('CT + Kırık Overlay')
    axes[2].axis('off')
    
    plt.suptitle(f'RibFrac1 - Slice {idx}', fontsize=14)
    plt.tight_layout()
    plt.savefig(r"C:\ribfrac\data\test_output\sonuc.png", dpi=150)
    plt.show()
    print("Görüntü kaydedildi!")
else:
    print("Bu CT'de kırık tespit edilmedi.")