import json, os

output_dir = r"C:\ribfrac\data\raw\Dataset001_RibFrac"

dataset = {
    "channel_names": {"0": "CT"},
    "labels": {"background": 0, "fracture": 1},
    "numTraining": 300,
    "file_ending": ".nii.gz"
}

with open(os.path.join(output_dir, "dataset.json"), "w") as f:
    json.dump(dataset, f, indent=4)

print("dataset.json güncellendi!")