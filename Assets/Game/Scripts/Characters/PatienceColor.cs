using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>The patience bar's colour: its hue slides from calm (green) through yellow to red as patience runs out.</summary>
    public static class PatienceColor
    {
        /// <param name="patience">Patience used up, from 0 to 1.</param>
        public static Color At(float patience, PatienceLook look) =>
            FromHsl(look.CalmHue * (1f - Mathf.Clamp01(patience)), look.Saturation, look.Lightness);

        /// <summary>HSL to RGB, every value from 0 to 1.</summary>
        public static Color FromHsl(float hue, float saturation, float lightness)
        {
            var chroma = (1f - Mathf.Abs(2f * lightness - 1f)) * saturation;
            var sector = Mathf.Repeat(hue, 1f) * 6f;
            var x = chroma * (1f - Mathf.Abs(Mathf.Repeat(sector, 2f) - 1f));
            var m = lightness - chroma * 0.5f;

            Vector3 rgb;
            if (sector < 1f)
                rgb = new Vector3(chroma, x, 0f);
            else if (sector < 2f)
                rgb = new Vector3(x, chroma, 0f);
            else if (sector < 3f)
                rgb = new Vector3(0f, chroma, x);
            else if (sector < 4f)
                rgb = new Vector3(0f, x, chroma);
            else if (sector < 5f)
                rgb = new Vector3(x, 0f, chroma);
            else
                rgb = new Vector3(chroma, 0f, x);
            return new Color(rgb.x + m, rgb.y + m, rgb.z + m, 1f);
        }
    }
}
