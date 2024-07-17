namespace ColorHelper;

using Vortice.Mathematics;

public class ColorAnalyzer
{
    private const int BinSize = 4; // Adjust bin size as needed, smaller bin size means more accurate but slower
    private const int NumBins = 256 / BinSize;
    private readonly int[] colorBins = new int[NumBins * NumBins * NumBins];

    public unsafe Color GetDominantColor(byte[] boxBytes, IntPtr boxBytesPtr)
    {
        // Reset bins to zero
        Array.Clear(colorBins, 0, colorBins.Length);

        var bpp = 4;
        var numPixels = boxBytes.Length / bpp;

        var data = (byte*)boxBytesPtr.ToPointer();
        for (var i = 0; i < numPixels; i++)
        {
            var r = data[i * 4 + 2] / BinSize;
            var g = data[i * 4 + 1] / BinSize;
            var b = data[i * 4] / BinSize;

            // Calculate the index for the flat array
            var index = (r * NumBins * NumBins) + (g * NumBins) + b;
            colorBins[index]++;
        }

        var maxCount = 0;
        var dominantIndex = 0;

        for (var i = 0; i < colorBins.Length; i++)
        {
            if (colorBins[i] > maxCount)
            {
                maxCount = colorBins[i];
                dominantIndex = i;
            }
        }

        // Convert flat array index back to bin indices
        var rMax = dominantIndex / (NumBins * NumBins);
        var gMax = (dominantIndex / NumBins) % NumBins;
        var bMax = dominantIndex % NumBins;

        // Convert bin back to color value
        var dominantColor = new Color(rMax * BinSize, gMax * BinSize, bMax * BinSize);
        return dominantColor;
    }
}
