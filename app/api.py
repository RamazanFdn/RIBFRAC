import os
import sys
import time
import shutil
import tempfile
import base64
import numpy as np
import nibabel as nib
import cv2
from fastapi import FastAPI, UploadFile, File
from fastapi.responses import JSONResponse
from scipy import ndimage

os.environ["nnUNet_raw"] = r"C:\ribfrac\data\raw"
os.environ["nnUNet_preprocessed"] = r"C:\ribfrac\data\preprocessed"
os.environ["nnUNet_results"] = r"C:\ribfrac\models\nnunet"

from nnunetv2.inference.predict_from_raw_data import nnUNetPredictor
import torch

app = FastAPI()
MODEL_VERSION = "nnUNetv2-2d-fold0"

predictor = nnUNetPredictor(
    tile_step_size=0.5,
    use_gaussian=True,
    use_mirroring=False,
    perform_everything_on_device=True,
    device=torch.device("cuda" if torch.cuda.is_available() else "cpu"),
    verbose=False
)
predictor.initialize_from_trained_model_folder(
    r"C:\ribfrac\models\nnunet\Dataset001_RibFrac\nnUNetTrainer__nnUNetPlans__2d",
    use_folds=(0,),
    checkpoint_name="checkpoint_best.pth"
)


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/analyze")
async def analyze(file: UploadFile = File(...)):
    start = time.time()
    tmp_dir = tempfile.mkdtemp()
    input_dir = os.path.join(tmp_dir, "input")
    output_dir = os.path.join(tmp_dir, "output")
    os.makedirs(input_dir)
    os.makedirs(output_dir)

    try:
        input_path = os.path.join(input_dir, "case_0000.nii.gz")
        with open(input_path, "wb") as f:
            f.write(await file.read())

        predictor.predict_from_files(
            [[input_path]],
            [os.path.join(output_dir, "case.nii.gz")],
            save_probabilities=False,
            overwrite=True,
            num_processes_preprocessing=1,
            num_processes_segmentation_export=1
        )

        pred_path = os.path.join(output_dir, "case.nii.gz")
        if not os.path.exists(pred_path):
            files = [f for f in os.listdir(output_dir) if f.endswith(".nii.gz")]
            if files:
                pred_path = os.path.join(output_dir, files[0])
            else:
                return JSONResponse(status_code=500, content={
                    "status": "error",
                    "message": "Tahmin dosyasi bulunamadi.",
                    "findings": [],
                    "overall_confidence": 0.0,
                    "inference_time_ms": 0.0
                })

        pred = nib.load(pred_path).get_fdata()
        ct = nib.load(input_path).get_fdata()
        findings = extract_findings(pred, ct)
        overall = float(np.mean([f["confidence"] for f in findings])) if findings else 0.0
        elapsed = (time.time() - start) * 1000

        return {
            "status": "success",
            "message": f"{len(findings)} kirik tespit edildi." if findings else "Kirik tespit edilmedi.",
            "model_version": MODEL_VERSION,
            "inference_time_ms": round(elapsed, 1),
            "findings": findings,
            "overall_confidence": round(overall, 3)
        }

    finally:
        shutil.rmtree(tmp_dir, ignore_errors=True)


def make_overlay(ct: np.ndarray, pred: np.ndarray, slice_idx: int) -> str:
    ct_slice = ct[:, :, slice_idx].T
    pred_slice = pred[:, :, slice_idx].T

    ct_norm = np.clip(ct_slice, -1000, 3000)
    ct_norm = ((ct_norm - ct_norm.min()) / (ct_norm.max() - ct_norm.min()) * 255).astype(np.uint8)

    ct_bgr = cv2.cvtColor(ct_norm, cv2.COLOR_GRAY2BGR)

    mask = (pred_slice > 0).astype(np.uint8)
    ct_bgr[mask == 1] = [0, 0, 255]

    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(ct_bgr, contours, -1, (0, 0, 255), 2)

    _, buffer = cv2.imencode(".png", ct_bgr)
    return base64.b64encode(buffer).decode("utf-8")


def extract_findings(pred: np.ndarray, ct: np.ndarray) -> list:
    if pred.max() == 0:
        return []
    labeled, n = ndimage.label(pred)
    findings = []
    for i in range(1, n + 1):
        comp = (labeled == i)
        if comp.sum() < 10:
            continue
        coords = np.argwhere(comp)
        mn = coords.min(axis=0)
        mx = coords.max(axis=0)
        conf = min(0.99, 0.5 + comp.sum() / 5000)
        best_slice = int(comp.sum(axis=(0, 1)).argmax())
        z_c = (mn[2] + mx[2]) / 2
        tz = pred.shape[2]
        if z_c < tz * 0.33:
            region = "Ust kaburga"
        elif z_c < tz * 0.66:
            region = "Orta kaburga"
        else:
            region = "Alt kaburga"

        overlay_b64 = make_overlay(ct, pred, best_slice)

        findings.append({
            "type": "fracture",
            "label": f"Kaburga kirigi #{i}",
            "confidence": round(float(conf), 3),
            "region": region,
            "slice_index": best_slice,
            "overlay_image": overlay_b64,
            "bbox": {
                "x": int(mn[0]),
                "y": int(mn[1]),
                "z": int(mn[2]),
                "width": int(mx[0] - mn[0]),
                "height": int(mx[1] - mn[1]),
                "depth": int(mx[2] - mn[2])
            }
        })
    findings.sort(key=lambda f: f["confidence"], reverse=True)
    return findings


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)