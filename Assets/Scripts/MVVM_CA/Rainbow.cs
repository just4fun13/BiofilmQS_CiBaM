using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.MVVM_CA
{
    public static class Rainbow
    {
        public static Color GetRainbowColor(double value)
        {
            if (value <= 3.33)
                return Color.Lerp(Color.blue, Color.green, (float)(value / 3.33));
            if (value <= 6.66)
                return Color.Lerp(Color.green, Color.yellow, (float)((value - 3.33) / 3.33));
            return Color.Lerp(Color.yellow, Color.red, (float)((value - 6.66) / 3.33));

            if (value < 0 || value > 1)
                throw new ArgumentException("Value must be between 0 and 1.");

            double h = value * 6; // Scale value to range [0, 6] for HSV hue
            int i = (int)Math.Floor(h); // Integer part of h
            double f = h - i; // Fractional part of h

            byte r = 0, g = 0, b = 0;
            switch (i)
            {
                case 0:
                    r = 255;
                    g = (byte)(255 * f);
                    b = 0;
                    break;
                case 1:
                    r = (byte)(255 * (1 - f));
                    g = 255;
                    b = 0;
                    break;
                case 2:
                    r = 0;
                    g = 255;
                    b = (byte)(255 * f);
                    break;
                case 3:
                    r = 0;
                    g = (byte)(255 * (1 - f));
                    b = 255;
                    break;
                case 4:
                    r = (byte)(255 * f);
                    g = 0;
                    b = 255;
                    break;
                case 5:
                    r = 255;
                    g = 0;
                    b = (byte)(255 * (1 - f));
                    break;
                default:
                    r = 255;
                    g = 0;
                    b = 0;
                    break;
            }

            return Color.HSVToRGB(r, g, b);
        }
    }
}
