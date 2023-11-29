namespace Motif.Controls
{
    using System.Windows.Media;

    public static class MotifColors
    {
        static MotifColors()
        {
            WindowFrameColor = Color.FromRgb(0xB2, 0x4D, 0x7A);
            WindowFrameBrush = new SolidColorBrush(WindowFrameColor);

            ControlColor = Color.FromRgb(0xAE, 0xB2, 0xC3);
            ControlBrush = new SolidColorBrush(ControlColor);
            ControlLightColor = Color.FromRgb(0xDC, 0xDE, 0xE5);
            ControlLightBrush = new SolidColorBrush(ControlLightColor);
            ControlLightLightColor = ControlLightColor;
            ControlLightLightBrush = new SolidColorBrush(ControlLightLightColor);
            ControlDarkColor = Color.FromRgb(0x5D, 0x60, 0x69);
            ControlDarkBrush = new SolidColorBrush(ControlDarkColor);
            ControlDarkDarkColor = ControlDarkColor;
            ControlDarkDarkBrush = new SolidColorBrush(ControlDarkDarkColor);
        }

        public static Color WindowFrameColor { get; }
        public static SolidColorBrush WindowFrameBrush { get; }
        public static Color ControlColor { get; }
        public static SolidColorBrush ControlBrush { get; }
        public static Color ControlLightColor { get; }
        public static SolidColorBrush ControlLightBrush { get; }
        public static Color ControlLightLightColor { get; }
        public static SolidColorBrush ControlLightLightBrush { get; }
        public static Color ControlDarkColor { get; }
        public static SolidColorBrush ControlDarkBrush { get; }
        public static Color ControlDarkDarkColor { get; }
        public static SolidColorBrush ControlDarkDarkBrush { get; }
    }
}