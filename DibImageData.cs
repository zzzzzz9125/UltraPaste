using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.IO;

public class DibImageData
{
    public static Bitmap ConvertToBitmap(byte[] dibData)
    {
        BITMAPINFOHEADER infoHeader = GetInfoHeader(dibData);
        int colorTableSize = CalculateColorTableSize(infoHeader);
        byte[] fileHeader = CreateBitmapFileHeader(infoHeader, colorTableSize, dibData.Length);
        MemoryStream mergedStream = new MemoryStream();
        mergedStream.Write(fileHeader, 0, fileHeader.Length);
        mergedStream.Write(dibData, 0, dibData.Length);
        mergedStream.Position = 0;
        return new Bitmap(mergedStream);
    }
    private static BITMAPINFOHEADER GetInfoHeader(byte[] dibData)
    {
        GCHandle handle = GCHandle.Alloc(dibData, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            return (BITMAPINFOHEADER)Marshal.PtrToStructure(ptr, typeof(BITMAPINFOHEADER));
        }
        finally
        {
            handle.Free();
        }
    }

    private static int CalculateColorTableSize(BITMAPINFOHEADER infoHeader)
    {
        if (infoHeader.biClrUsed != 0)
            return (int)infoHeader.biClrUsed * 4;

        switch (infoHeader.biBitCount)
        {
            case 1: return 2 * 4;
            case 4: return 16 * 4;
            case 8: return 256 * 4;
            default: return 0;
        }
    }

    private static byte[] CreateBitmapFileHeader(BITMAPINFOHEADER infoHeader, int colorTableSize, int dibDataLength)
    {
        byte[] header = new byte[14];
        header[0] = (byte)'B';
        header[1] = (byte)'M';

        int fileSize = 14 + dibDataLength;
        Buffer.BlockCopy(BitConverter.GetBytes(fileSize), 0, header, 2, 4);

        int dataOffset = 14 + (int)infoHeader.biSize + colorTableSize;
        Buffer.BlockCopy(BitConverter.GetBytes(dataOffset), 0, header, 10, 4);

        return header;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }
}