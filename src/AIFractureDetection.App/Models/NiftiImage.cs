namespace AIFractureDetection.App.Models;

/// <summary>
/// NIfTI görüntü verisi (yalnızca 2D önizleme için gerekli minimum bilgi).
/// </summary>
public class NiftiImage
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Depth { get; init; }
    public float VoxelSizeX { get; init; } = 1f;
    public float VoxelSizeY { get; init; } = 1f;
    public float VoxelSizeZ { get; init; } = 1f;

    /// <summary>
    /// 3D voksel verisi — düzlemleştirilmiş (z, y, x) sırasına göre.
    /// Uzunluk = Width * Height * Depth
    /// </summary>
    public float[] Voxels { get; init; } = Array.Empty<float>();

    public float MinValue { get; init; }
    public float MaxValue { get; init; }

    /// <summary>
    /// Bir eksene göre kesit çıkarır (axial / coronal / sagittal).
    /// Dönüş değeri 0-255 aralığında uint8 intensiteler.
    /// </summary>
    public byte[] GetSlice(SliceOrientation orientation, int sliceIndex, out int sliceWidth, out int sliceHeight)
    {
        float range = Math.Max(MaxValue - MinValue, 1e-5f);

        switch (orientation)
        {
            case SliceOrientation.Axial: // z sabit
                sliceWidth = Width;
                sliceHeight = Height;
                return ExtractAxial(sliceIndex, range);
            case SliceOrientation.Coronal: // y sabit
                sliceWidth = Width;
                sliceHeight = Depth;
                return ExtractCoronal(sliceIndex, range);
            case SliceOrientation.Sagittal: // x sabit
                sliceWidth = Height;
                sliceHeight = Depth;
                return ExtractSagittal(sliceIndex, range);
            default:
                sliceWidth = 0; sliceHeight = 0;
                return Array.Empty<byte>();
        }
    }

    private byte[] ExtractAxial(int z, float range)
    {
        z = Math.Clamp(z, 0, Depth - 1);
        var buf = new byte[Width * Height];
        int sliceStart = z * Width * Height;
        for (int i = 0; i < buf.Length; i++)
        {
            float v = (Voxels[sliceStart + i] - MinValue) / range;
            buf[i] = (byte)Math.Clamp(v * 255f, 0, 255);
        }
        return buf;
    }

    private byte[] ExtractCoronal(int y, float range)
    {
        y = Math.Clamp(y, 0, Height - 1);
        var buf = new byte[Width * Depth];
        for (int z = 0; z < Depth; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                int src = z * Width * Height + y * Width + x;
                float v = (Voxels[src] - MinValue) / range;
                buf[z * Width + x] = (byte)Math.Clamp(v * 255f, 0, 255);
            }
        }
        return buf;
    }

    private byte[] ExtractSagittal(int x, float range)
    {
        x = Math.Clamp(x, 0, Width - 1);
        var buf = new byte[Height * Depth];
        for (int z = 0; z < Depth; z++)
        {
            for (int y = 0; y < Height; y++)
            {
                int src = z * Width * Height + y * Width + x;
                float v = (Voxels[src] - MinValue) / range;
                buf[z * Height + y] = (byte)Math.Clamp(v * 255f, 0, 255);
            }
        }
        return buf;
    }
}

public enum SliceOrientation
{
    Axial,
    Coronal,
    Sagittal
}
