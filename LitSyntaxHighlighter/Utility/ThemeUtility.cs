using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System.Windows.Media;

namespace LitSyntaxHighlighter.Utility
{
    internal static class ThemeUtility
    {
        internal enum ThemeColor
        {
            Light,
            Dark
        }

        public static bool IsLightTheme => CurrentColorTheme == ThemeColor.Light;
        public static Color TextColor
        {
            get
            {
                System.Drawing.Color textColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
                return Color.FromArgb(textColor.A, textColor.R, textColor.G, textColor.B);
            }
        }
        internal static ThemeColor CurrentColorTheme
        {
            get
            {
                System.Drawing.Color bgColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                var luminance = (bgColor.R * 0.2126) + (bgColor.G * 0.7152) + (bgColor.B * 0.0722);
                if(luminance > (255 / 2))
                {
                    return ThemeColor.Light;
                }
                return ThemeColor.Dark;
            }
        }

        public static Color ToColor(ThemeResourceKey key)
        {
            var color = VSColorTheme.GetThemedColor(key);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
