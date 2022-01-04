using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA
{
    public static class Gdi
    {
        /// <summary> 
        /// Converts RGB to HSL 
        /// </summary> 
        /// <remarks>Takes advantage of whats already built in to .NET by using the Color.GetHue, Color.GetSaturation and Color.GetBrightness methods</remarks> 
        /// <param name="color">A Color to convert</param> 
        /// <returns>An HSL tuple</returns> 
        public static (float H, float S, float L) GetHsl(this Color color)
        {
            var H = color.GetHue() / 360f; 
            var L = color.GetBrightness();
            var S = color.GetSaturation();

            return (H, S, L);
        }
        /// <summary>
        /// Converts a color from HSL to RGB
        /// </summary>
        /// <remarks>Adapted from the algorithm in Foley and Van-Dam</remarks>
        /// <param name="hsl">The HSL tuple</param>
        /// <returns>A Color structure containing the equivalent RGB values</returns>
        public static Color GetColor(this (float H, float S, float L) hsl, float alpha = 1f)
        {
            float r = 0, g = 0, b = 0;
            float temp1, temp2;
            // Clamp HSL between 0..1
            hsl = (
                Math.Max(0, Math.Min(1f, hsl.H)), 
                Math.Max(0, Math.Min(1f, hsl.S)), 
                Math.Max(0, Math.Min(1f, hsl.L)));
            if (hsl.L == 0) r = g = b = 0;
            else
            {
                if (hsl.S == 0) r = g = b = hsl.L;
                else
                {
                    temp2 = ((hsl.L <= 0.5f) ? hsl.L * (1f + hsl.S) : hsl.L + hsl.S - (hsl.L * hsl.S));
                    temp1 = 2f * hsl.L - temp2;

                    var t3 = new[] { hsl.H + 1 / 3f, hsl.H, hsl.H - 1 / 3f };
                    var clr = new float[] { 0, 0, 0 };
                    for (int i = 0; i < 3; i++)
                    {
                        if (t3[i] < 0) t3[i] += 1f;
                        if (t3[i] > 1) t3[i] -= 1f;

                        if (6.0 * t3[i] < 1.0) clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6f;
                        else if (2.0 * t3[i] < 1.0) clr[i] = temp2;
                        else if (3.0 * t3[i] < 2.0) clr[i] = (temp1 + (temp2 - temp1) * ((2 / 3f) - t3[i]) * 6);
                        else clr[i] = temp1;
                    }
                    r = clr[0];
                    g = clr[1];
                    b = clr[2];
                }
            }

            return Color.FromArgb((int)(255*alpha), (int)(255 * r), (int)(255 * g), (int)(255 * b));
        }
    }
}
