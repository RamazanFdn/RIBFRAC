using System.IO;
using System.IO.Compression;
using AIFractureDetection.App.Models;

namespace AIFractureDetection.App.Services;

/// <summary>
/// Minimal NIfTI-1 okuyucu. Önizleme amaçlıdır; tam medikal analiz için
/// gerçek AI modeline gönderilen ham dosya kullanılır.
/// </summary>
public class NiftiReader : INiftiReader
{
    private const int HeaderSize = 348;

    // NIfTI datatype kodları (yalnız sık kullanılanlar)
    private const short DT_UINT8 = 2;
    private const short DT_INT16 = 4;
    private const short DT_INT32 = 8;
    private const short DT_FLOAT32 = 16;
    private const short DT_FLOAT64 = 64;
    private const short DT_INT8 = 256;
    private const short DT_UINT16 = 512;
    private const short DT_UINT32 = 768;

    public async Task<NiftiImage> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");

        byte[] raw;
        if (filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            await using var fs = File.OpenRead(filePath);
            await using var gz = new GZipStream(fs, CompressionMode.Decompress);
            await using var ms = new MemoryStream();
            await gz.CopyToAsync(ms, cancellationToken);
            raw = ms.ToArray();
        }
        else
        {
            raw = await File.ReadAllBytesAsync(filePath, cancellationToken);
        }

        if (raw.Length < HeaderSize)
            throw new InvalidDataException("Geçersiz NIfTI dosyası (başlık eksik).");

        // Byte order kontrolü: sizeof_hdr ilk 4 bayt, 348 olmalı
        int sizeOfHdr = BitConverter.ToInt32(raw, 0);
        bool swap = sizeOfHdr != HeaderSize;
        if (swap)
        {
            sizeOfHdr = BitConverter.ToInt32(SwapBytes(raw, 0, 4), 0);
            if (sizeOfHdr != HeaderSize)
                throw new InvalidDataException("NIfTI başlığı geçersiz.");
        }

        // dim: short[8] offset 40
        short dim0 = ReadInt16(raw, 40, swap);
        short dimX = ReadInt16(raw, 42, swap);
        short dimY = ReadInt16(raw, 44, swap);
        short dimZ = ReadInt16(raw, 46, swap);

        if (dim0 < 3 || dimX <= 0 || dimY <= 0 || dimZ <= 0)
            throw new InvalidDataException("NIfTI boyut bilgisi geçersiz.");

        // datatype offset 70, bitpix offset 72
        short datatype = ReadInt16(raw, 70, swap);

        // pixdim offset 76, float[8]
        float pixX = ReadFloat(raw, 80, swap);
        float pixY = ReadFloat(raw, 84, swap);
        float pixZ = ReadFloat(raw, 88, swap);

        // vox_offset offset 108 (float)
        float voxOffset = ReadFloat(raw, 108, swap);
        int dataStart = (int)Math.Max(voxOffset, HeaderSize);

        if (dataStart >= raw.Length)
            throw new InvalidDataException("Voxel verisi bulunamadı.");

        int voxelCount = dimX * dimY * dimZ;
        var voxels = new float[voxelCount];

        ReadVoxels(raw, dataStart, datatype, swap, voxels);

        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < voxels.Length; i++)
        {
            if (voxels[i] < min) min = voxels[i];
            if (voxels[i] > max) max = voxels[i];
        }

        return new NiftiImage
        {
            Width = dimX,
            Height = dimY,
            Depth = dimZ,
            VoxelSizeX = pixX == 0 ? 1f : pixX,
            VoxelSizeY = pixY == 0 ? 1f : pixY,
            VoxelSizeZ = pixZ == 0 ? 1f : pixZ,
            Voxels = voxels,
            MinValue = min,
            MaxValue = max
        };
    }

    private static void ReadVoxels(byte[] raw, int start, short datatype, bool swap, float[] target)
    {
        switch (datatype)
        {
            case DT_UINT8:
                for (int i = 0; i < target.Length; i++)
                    target[i] = raw[start + i];
                break;
            case DT_INT8:
                for (int i = 0; i < target.Length; i++)
                    target[i] = (sbyte)raw[start + i];
                break;
            case DT_INT16:
                for (int i = 0; i < target.Length; i++)
                    target[i] = ReadInt16(raw, start + i * 2, swap);
                break;
            case DT_UINT16:
                for (int i = 0; i < target.Length; i++)
                    target[i] = (ushort)ReadInt16(raw, start + i * 2, swap);
                break;
            case DT_INT32:
                for (int i = 0; i < target.Length; i++)
                    target[i] = ReadInt32(raw, start + i * 4, swap);
                break;
            case DT_UINT32:
                for (int i = 0; i < target.Length; i++)
                    target[i] = (uint)ReadInt32(raw, start + i * 4, swap);
                break;
            case DT_FLOAT32:
                for (int i = 0; i < target.Length; i++)
                    target[i] = ReadFloat(raw, start + i * 4, swap);
                break;
            case DT_FLOAT64:
                for (int i = 0; i < target.Length; i++)
                    target[i] = (float)ReadDouble(raw, start + i * 8, swap);
                break;
            default:
                throw new NotSupportedException($"Desteklenmeyen NIfTI datatype: {datatype}");
        }
    }

    private static short ReadInt16(byte[] buf, int offset, bool swap)
        => swap ? BitConverter.ToInt16(SwapBytes(buf, offset, 2), 0) : BitConverter.ToInt16(buf, offset);

    private static int ReadInt32(byte[] buf, int offset, bool swap)
        => swap ? BitConverter.ToInt32(SwapBytes(buf, offset, 4), 0) : BitConverter.ToInt32(buf, offset);

    private static float ReadFloat(byte[] buf, int offset, bool swap)
        => swap ? BitConverter.ToSingle(SwapBytes(buf, offset, 4), 0) : BitConverter.ToSingle(buf, offset);

    private static double ReadDouble(byte[] buf, int offset, bool swap)
        => swap ? BitConverter.ToDouble(SwapBytes(buf, offset, 8), 0) : BitConverter.ToDouble(buf, offset);

    private static byte[] SwapBytes(byte[] buf, int offset, int count)
    {
        var result = new byte[count];
        for (int i = 0; i < count; i++)
            result[i] = buf[offset + count - 1 - i];
        return result;
    }
}
