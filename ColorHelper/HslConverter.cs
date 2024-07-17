using Vortice.Mathematics;

namespace ColorHelper;

public static class HslConverter
{
    public static (byte H, byte S, byte L) RgbToHsl(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        double s = 0;
        double l = (max + min) / 2;

        if (delta != 0)
        {
            s = l < 0.5 ? delta / (max + min) : delta / (2 - max - min);

            if (max == r)
            {
                h = (g - b) / delta + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
                h = (b - r) / delta + 2;
            }
            else if (max == b)
            {
                h = (r - g) / delta + 4;
            }

            h /= 6;
        }

        return ((byte)(h * 255), (byte)(s * 255), (byte)(l * 255));
    }

    public static Color HslToRgb(byte h, byte s, byte l)
    {
        double h_norm = h / 255.0;
        double s_norm = s / 255.0;
        double l_norm = l / 255.0;

        double r = l_norm, g = l_norm, b = l_norm;

        if (s != 0)
        {
            double q = l_norm < 0.5 ? l_norm * (1 + s_norm) : l_norm + s_norm - l_norm * s_norm;
            double p = 2 * l_norm - q;
            r = HueToRgb(p, q, h_norm + 1.0 / 3);
            g = HueToRgb(p, q, h_norm);
            b = HueToRgb(p, q, h_norm - 1.0 / 3);
        }

        return new Color(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}