using System.Drawing;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace Grabber;

public static class Utilities
{
    public static  Bitmap Texture2DToBitmap(ID3D11Device1 device, ID3D11Texture2D texture)
    {
        // Map the texture
        var dataBox = device.ImmediateContext.Map(texture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        var width = texture.Description.Width;
        var height = texture.Description.Height;
        var stride = dataBox.RowPitch; // Correct stride from the data box

        // Create a bitmap to hold the texture data
        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);

        // Copy the data from the texture to the bitmap
        for (var y = 0; y < height; y++)
        {
            unsafe
            {
                var srcPtr = IntPtr.Add(dataBox.DataPointer, y * dataBox.RowPitch);
                var destPtr = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                MemCopy(srcPtr.ToPointer(), destPtr.ToPointer(), width * 4);
            }
        }

        bitmap.UnlockBits(bitmapData);

        // Unmap the texture
        device.ImmediateContext.Unmap(texture, 0);

        return bitmap;
    }
    
    public static unsafe void MemCopy(void* source, void* destination, int bytes)
    {
        Unsafe.CopyBlock(destination, source, (uint)bytes);
    }
}