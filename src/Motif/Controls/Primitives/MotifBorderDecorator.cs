namespace Motif.Controls.Primitives
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    // Summary:
    //     Specifics the type of System.Windows.Controls.Border to draw.
    public enum MotifBorderStyle
    {
        // Summary:
        //     No border.
        None = 0,

        //
        // Summary:
        //     Used for System.Windows.Controls.Button elements in their normal state.
        Raised = 1,

        //
        // Summary:
        //     Used for System.Windows.Controls.Button elements in their pressed state.
        RaisedPressed = 2,

        //
        // Summary:
        //     Used for System.Windows.Controls.Button elements that have keyboard focus
        //     or are the default System.Windows.Controls.Button.
        RaisedFocused = 3,

        //
        // Summary:
        //     Used for System.Windows.Controls.ListBox, System.Windows.Controls.TextBox,
        //     and System.Windows.Controls.CheckBox.
        Sunken = 4,

        //
        // Summary:
        //     Used for System.Windows.Controls.GroupBox.
        Etched = 5,

        //
        // Summary:
        //     Used for horizontal System.Windows.Controls.Separator.
        HorizontalLine = 6,

        //
        // Summary:
        //     Used for vertical System.Windows.Controls.Separator.
        VerticalLine = 7,

        //
        // Summary:
        //     Used for System.Windows.Controls.TabItem.
        TabRight = 8,

        //
        // Summary:
        //     Used for System.Windows.Controls.TabItem.
        TabTop = 9,

        //
        // Summary:
        //     Used for System.Windows.Controls.TabItem.
        TabLeft = 10,

        //
        // Summary:
        //     Used for System.Windows.Controls.TabItem.
        TabBottom = 11,

        //
        // Summary:
        //     Used for top level System.Windows.Controls.MenuItem when the mouse or other
        //     input device is hovering over them.
        ThinRaised = 12,

        //
        // Summary:
        //     Used for top level System.Windows.Controls.MenuItem in their pressed state.
        ThinPressed = 13,

        //
        // Summary:
        //     Used for the System.Windows.Controls.Primitives.Thumb on a System.Windows.Controls.Primitives.ScrollBar
        //     in a normal state.
        AltRaised = 14,

        //
        // Summary:
        //     Used for the System.Windows.Controls.Primitives.Thumb on a System.Windows.Controls.Primitives.ScrollBar
        //     in their pressed state.
        AltPressed = 15,

        //
        // Summary:
        //     A System.Windows.Controls.RadioButton border.
        RadioButton = 16,
    }

    public class MotifBorderDecorator : Decorator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct HlsColor
        {
            public readonly byte Alpha;
            public readonly float Hue;
            public readonly float Saturation;
            public float Lightness;

            public HlsColor(byte alpha, float hue, float lightness, float saturation)
            {
                this.Alpha = alpha;
                this.Hue = hue;
                this.Lightness = lightness;
                this.Saturation = saturation;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RgbColor
        {
            public readonly byte Alpha;
            public readonly float Red;
            public readonly float Green;
            public readonly float Blue;

            public RgbColor(byte alpha, float red, float green, float blue)
            {
                this.Alpha = alpha;
                this.Red = red;
                this.Green = green;
                this.Blue = blue;
            }

            public RgbColor(Color color)
                : this(color.A, ((float)color.R) / 255f, ((float)color.G) / 255f, ((float)color.B) / 255f)
            {
            }

            public Color Color
            {
                get
                {
                    return Color.FromArgb(this.Alpha, (byte)(this.Red * 255f), (byte)(this.Green * 255f), (byte)(this.Blue * 255f));
                }
            }
        }

        // Nested Types
        private class BorderGeometryCache
        {
            // Fields
            public Geometry BorderGeometry;

            public Thickness BorderThickness;
            public Rect Bounds;
        }

        private class CustomBrushCache
        {
            // Fields
            public SolidColorBrush DarkBrush;

            public SolidColorBrush DarkDarkBrush;
            public SolidColorBrush LightBrush;
            public SolidColorBrush LightLightBrush;
        }

        private class TabGeometryCache
        {
            // Fields
            public Rect Bounds;

            public Geometry Highlight1;
            public Geometry Highlight2;
            public Geometry Shadow1;
            public Geometry Shadow2;
            public MatrixTransform Transform;
            public double xOffset;
            public double yOffset;
        }

        public static readonly DependencyProperty BackgroundProperty =
            Panel.BackgroundProperty.AddOwner(typeof(MotifBorderDecorator),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.AffectsRender,
                    new PropertyChangedCallback(MotifBorderDecorator.BorderBrushesChanged)));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(MotifBorderDecorator),
                new FrameworkPropertyMetadata(MotifBorderBrush, FrameworkPropertyMetadataOptions.Journal | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BorderStyleProperty =
            DependencyProperty.Register("BorderStyle", typeof(MotifBorderStyle), typeof(MotifBorderDecorator),
                new FrameworkPropertyMetadata(MotifBorderStyle.Raised, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure,
                    new PropertyChangedCallback(MotifBorderDecorator.BorderBrushesChanged)), new ValidateValueCallback(MotifBorderDecorator.IsValidBorderStyle));

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(MotifBorderDecorator),
                new FrameworkPropertyMetadata(new Thickness(0.0), FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure),
                    new ValidateValueCallback(MotifBorderDecorator.IsValidBorderThickness));

        private static readonly object _resourceAccess = new object();
        private static Geometry _bottomRightArcGeometry;
        private static Brush _motifBorderBrush;
        private static Geometry _topLeftArcGeometry;

        private BorderGeometryCache _borderGeometryCache;

        private CustomBrushCache _brushCache;
        private TabGeometryCache _tabCache;

        static MotifBorderDecorator()
        {
            UIElement.SnapsToDevicePixelsProperty.OverrideMetadata(typeof(MotifBorderDecorator), new FrameworkPropertyMetadata(true));
        }

        public static Brush MotifBorderBrush
        {
            get
            {
                if (_motifBorderBrush == null)
                {
                    lock (BrushLockHolder.BrushLock)
                    {
                        if (_motifBorderBrush == null)
                        {
                            SolidColorBrush brush = new SolidColorBrush();
                            brush.Freeze();
                            _motifBorderBrush = brush;
                        }
                    }
                }
                return _motifBorderBrush;
            }
        }

        // Properties
        public Brush Background
        {
            get
            {
                return (Brush)base.GetValue(BackgroundProperty);
            }
            set
            {
                base.SetValue(BackgroundProperty, value);
            }
        }

        public Brush BorderBrush
        {
            get
            {
                return (Brush)base.GetValue(BorderBrushProperty);
            }
            set
            {
                base.SetValue(BorderBrushProperty, value);
            }
        }

        public MotifBorderStyle BorderStyle
        {
            get
            {
                return (MotifBorderStyle)base.GetValue(BorderStyleProperty);
            }
            set
            {
                base.SetValue(BorderStyleProperty, value);
            }
        }

        public Thickness BorderThickness
        {
            get
            {
                return (Thickness)base.GetValue(BorderThicknessProperty);
            }
            set
            {
                base.SetValue(BorderThicknessProperty, value);
            }
        }

        private static Geometry BottomRightArcGeometry
        {
            get
            {
                if (_bottomRightArcGeometry == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_bottomRightArcGeometry == null)
                        {
                            StreamGeometry geometry = new StreamGeometry();
                            StreamGeometryContext context = geometry.Open();
                            context.BeginFigure(new Point(2.0, 10.0), true, false);
                            context.ArcTo(new Point(10.0, 2.0), new Size(4.0, 4.0), 0.0, false, SweepDirection.Counterclockwise, true, false);
                            context.Close();
                            geometry.Freeze();
                            _bottomRightArcGeometry = geometry;
                        }
                    }
                }
                return _bottomRightArcGeometry;
            }
        }

        private static Geometry TopLeftArcGeometry
        {
            get
            {
                if (_topLeftArcGeometry == null)
                {
                    lock (_resourceAccess)
                    {
                        if (_topLeftArcGeometry == null)
                        {
                            StreamGeometry geometry = new StreamGeometry();
                            StreamGeometryContext context = geometry.Open();
                            context.BeginFigure(new Point(2.0, 10.0), true, false);
                            context.ArcTo(new Point(10.0, 2.0), new Size(4.0, 4.0), 0.0, false, SweepDirection.Clockwise, true, false);
                            context.Close();
                            geometry.Freeze();
                            _topLeftArcGeometry = geometry;
                        }
                    }
                }
                return _topLeftArcGeometry;
            }
        }

        private Brush DarkBrush
        {
            get
            {
                if (this._brushCache == null)
                {
                    return MotifColors.ControlDarkBrush;
                }
                return this._brushCache.DarkBrush;
            }
        }

        private Brush DarkDarkBrush
        {
            get
            {
                if (this._brushCache == null)
                {
                    return MotifColors.ControlDarkDarkBrush;
                }
                return this._brushCache.DarkDarkBrush;
            }
        }

        private Brush LightBrush
        {
            get
            {
                if (this._brushCache == null)
                {
                    return MotifColors.ControlLightBrush;
                }
                return this._brushCache.LightBrush;
            }
        }

        private Brush LightLightBrush
        {
            get
            {
                if (this._brushCache == null)
                {
                    return MotifColors.ControlLightLightBrush;
                }
                return this._brushCache.LightLightBrush;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElement child = this.Child;
            if (child != null)
            {
                Thickness borderThickness = this.BorderThickness;
                double num = borderThickness.Left + borderThickness.Right;
                double num2 = borderThickness.Top + borderThickness.Bottom;
                Rect finalRect = new Rect();
                if ((finalSize.Width >= num) && (finalSize.Height >= num2))
                {
                    finalRect.X = borderThickness.Left;
                    finalRect.Y = borderThickness.Top;
                    finalRect.Width = finalSize.Width - num;
                    finalRect.Height = finalSize.Height - num2;
                    switch (this.BorderStyle)
                    {
                        case MotifBorderStyle.RaisedPressed:
                        case MotifBorderStyle.AltPressed:
                            finalRect.X++;
                            finalRect.Y++;
                            break;

                        case MotifBorderStyle.ThinPressed:
                            finalRect.X--;
                            finalRect.Y--;
                            break;
                    }
                }
                child.Arrange(finalRect);
            }
            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = HelperCollapseThickness(this.BorderThickness);
            UIElement child = this.Child;
            if (child != null)
            {
                Size size3 = new Size();
                bool flag = availableSize.Width < size.Width;
                bool flag2 = availableSize.Height < size.Height;
                if (!flag)
                {
                    size3.Width = availableSize.Width - size.Width;
                }
                if (!flag2)
                {
                    size3.Height = availableSize.Height - size.Height;
                }
                child.Measure(size3);
                Size desiredSize = child.DesiredSize;
                if (!flag)
                {
                    desiredSize.Width += size.Width;
                }
                if (!flag2)
                {
                    desiredSize.Height += size.Height;
                }
                return desiredSize;
            }
            return new Size(Math.Min(size.Width, availableSize.Width), Math.Min(size.Height, availableSize.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Brush brush2;
            Brush borderBrush = this.BorderBrush;
            MotifBorderStyle borderStyle = this.BorderStyle;
            Thickness borderThickness = this.BorderThickness;
            double classicBorderThickness = this.GetClassicBorderThickness();
            Thickness thickness2 = ScaleThickness(borderThickness, 1.0 / classicBorderThickness);
            Rect bounds = new Rect(0.0, 0.0, base.ActualWidth, base.ActualHeight);
            switch (borderStyle)
            {
                case MotifBorderStyle.RaisedFocused:
                case MotifBorderStyle.RaisedPressed:
                    this.DrawBorder(MotifColors.WindowFrameBrush, thickness2, drawingContext, ref bounds);
                    break;
            }
            bool flag = this.IsTabStyle(borderStyle);
            if (((borderBrush == MotifBorderBrush) || flag) || (borderStyle == MotifBorderStyle.RadioButton))
            {
                switch (borderStyle)
                {
                    case MotifBorderStyle.Raised:
                    case MotifBorderStyle.RaisedFocused:
                        this.DrawRaisedBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.RaisedPressed:
                        this.DrawRaisedPressedBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.Sunken:
                        this.DrawSunkenBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.Etched:
                        this.DrawEtchedBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.HorizontalLine:
                        this.DrawHorizontalLine(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.VerticalLine:
                        this.DrawVerticalLine(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.TabRight:
                        this.DrawTabRight(drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.TabTop:
                        this.DrawTabTop(drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.TabLeft:
                        this.DrawTabLeft(drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.TabBottom:
                        this.DrawTabBottom(drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.ThinRaised:
                        this.DrawThinRaisedBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.ThinPressed:
                        this.DrawThinPressedBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.AltRaised:
                        this.DrawAltRaisedBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.AltPressed:
                        this.DrawAltPressedBorder(thickness2, drawingContext, ref bounds);
                        goto Label_01B1;

                    case MotifBorderStyle.RadioButton:
                        this.DrawRadioButtonBorder(drawingContext, ref bounds);
                        return;
                }
            }
            else
            {
                this.DrawBorder(borderBrush, borderThickness, drawingContext, ref bounds);
            }
        Label_01B1:
            brush2 = this.Background;
            if (((brush2 != null) && (bounds.Width > 0.0)) && (bounds.Height > 0.0))
            {
                drawingContext.DrawRectangle(brush2, null, bounds);
            }
            if (base.SnapsToDevicePixels)
            {
                if (base.VisualXSnappingGuidelines == null)
                {
                    double width = base.RenderSize.Width;
                    DoubleCollection doubles = this.GetPixelSnappingGuidelines(width, thickness2.Left, thickness2.Right, (int)classicBorderThickness);
                    base.VisualXSnappingGuidelines = doubles;
                }
                if (base.VisualYSnappingGuidelines == null)
                {
                    double height = base.RenderSize.Height;
                    DoubleCollection doubles2 = this.GetPixelSnappingGuidelines(height, thickness2.Top, thickness2.Bottom, (int)classicBorderThickness);
                    base.VisualYSnappingGuidelines = doubles2;
                }
            }
        }

        protected void DrawBorder(Brush borderBrush, Thickness borderThickness, DrawingContext dc, ref Rect bounds)
        {
            Size size = HelperCollapseThickness(borderThickness);
            if ((size.Width > 0.0) || (size.Height > 0.0))
            {
                if ((size.Width > bounds.Width) || (size.Height > bounds.Height))
                {
                    if (((borderBrush != null) && (bounds.Width > 0.0)) && (bounds.Height > 0.0))
                    {
                        dc.DrawRectangle(borderBrush, null, bounds);
                    }
                    bounds = Rect.Empty;
                }
                else
                {
                    if (IsSimpleBorderBrush(borderBrush))
                    {
                        if (borderThickness.Top > 0.0)
                        {
                            dc.DrawRectangle(borderBrush, null, new Rect(bounds.Left, bounds.Top, bounds.Width, borderThickness.Top));
                        }
                        if (borderThickness.Left > 0.0)
                        {
                            dc.DrawRectangle(borderBrush, null, new Rect(bounds.Left, bounds.Top, borderThickness.Left, bounds.Height));
                        }
                        if (borderThickness.Right > 0.0)
                        {
                            dc.DrawRectangle(borderBrush, null, new Rect(bounds.Right - borderThickness.Right, bounds.Top, borderThickness.Right, bounds.Height));
                        }
                        if (borderThickness.Bottom > 0.0)
                        {
                            dc.DrawRectangle(borderBrush, null, new Rect(bounds.Left, bounds.Bottom - borderThickness.Bottom, bounds.Width, borderThickness.Bottom));
                        }
                    }
                    else
                    {
                        dc.DrawGeometry(borderBrush, null, this.GetBorder(bounds, borderThickness));
                    }
                    bounds = HelperDeflateRect(bounds, borderThickness);
                }
            }
        }

        protected void DrawBorderPair(Brush highlight, Brush shadow, Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            this.DrawBorder(shadow, new Thickness(0.0, 0.0, singleThickness.Right, singleThickness.Bottom), dc, ref bounds);
            this.DrawBorder(highlight, new Thickness(singleThickness.Left, singleThickness.Top, 0.0, 0.0), dc, ref bounds);
        }

        private static void BorderBrushesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Color color;
            MotifBorderDecorator decorator = (MotifBorderDecorator)d;
            SolidColorBrush background = decorator.Background as SolidColorBrush;
            decorator._tabCache = null;
            if ((((decorator.BorderStyle != MotifBorderStyle.Sunken) && (decorator.BorderStyle != MotifBorderStyle.RadioButton)) &&
                 ((background != null) && ((color = background.Color) != MotifColors.ControlColor))) &&
                (color.A > 0))
            {
                if ((decorator._brushCache == null) || (color != decorator._brushCache.LightBrush.Color))
                {
                    if (decorator._brushCache == null)
                    {
                        decorator._brushCache = new CustomBrushCache();
                    }
                    decorator._brushCache.LightBrush = background;
                    decorator._brushCache.LightLightBrush = new SolidColorBrush(GetControlLightLightColor(color));
                    Color controlDarkColor = GetControlDarkColor(color);
                    decorator._brushCache.DarkBrush = new SolidColorBrush(controlDarkColor);
                    Color color3 = new Color
                    {
                        R = (byte)((controlDarkColor.R + MotifColors.WindowFrameColor.R) / 2),
                        G = (byte)((controlDarkColor.G + MotifColors.WindowFrameColor.G) / 2),
                        B = (byte)((controlDarkColor.B + MotifColors.WindowFrameColor.B) / 2),
                        A = (byte)((controlDarkColor.A + MotifColors.WindowFrameColor.A) / 2)
                    };
                    decorator._brushCache.DarkDarkBrush = new SolidColorBrush(color3);
                }
            }
            else
            {
                decorator._brushCache = null;
            }
            decorator.VisualXSnappingGuidelines = null;
            decorator.VisualYSnappingGuidelines = null;
        }

        private static Geometry GenerateBorderGeometry(Rect rect, Thickness borderThickness)
        {
            PathGeometry geometry = new PathGeometry
            {
                Figures = {
                    GenerateRectFigure(rect),
                    GenerateRectFigure(HelperDeflateRect(rect, borderThickness))
                }
            };
            geometry.Freeze();
            return geometry;
        }

        private static PathFigure GenerateRectFigure(Rect rect)
        {
            PathFigure figure = new PathFigure
            {
                StartPoint = rect.TopLeft,
                Segments = {
                    new LineSegment(rect.TopRight, true),
                    new LineSegment(rect.BottomRight, true),
                    new LineSegment(rect.BottomLeft, true)
                },
                IsClosed = true
            };
            figure.Freeze();
            return figure;
        }

        private static Color GetControlDarkColor(Color controlColor)
        {
            HlsColor hlsColor = RgbToHls(new RgbColor(controlColor));
            hlsColor.Lightness *= 0.666f;
            return HlsToRgb(hlsColor).Color;
        }

        private static Color GetControlLightLightColor(Color controlColor)
        {
            HlsColor hlsColor = RgbToHls(new RgbColor(controlColor));
            hlsColor.Lightness = (hlsColor.Lightness + 1f) * 0.5f;
            return HlsToRgb(hlsColor).Color;
        }

        private static Size HelperCollapseThickness(Thickness th)
        {
            return new Size(th.Left + th.Right, th.Top + th.Bottom);
        }

        private static Rect HelperDeflateRect(Rect rt, Thickness thick)
        {
            return new Rect(rt.Left + thick.Left, rt.Top + thick.Top, Math.Max(0.0, ((rt.Width - thick.Left) - thick.Right)), Math.Max(0.0, ((rt.Height - thick.Top) - thick.Bottom)));
        }

        private static RgbColor HlsToRgb(HlsColor hlsColor)
        {
            float num;
            float num2;
            float num3;
            if (hlsColor.Saturation == 0.0)
            {
                num = num2 = num3 = hlsColor.Lightness;
            }
            else
            {
                float num5;
                float hue = (float)((hlsColor.Hue - Math.Floor((double)hlsColor.Hue)) * 6.0);
                if (hlsColor.Lightness <= 0.5f)
                {
                    num5 = hlsColor.Lightness * (1f + hlsColor.Saturation);
                }
                else
                {
                    num5 = (hlsColor.Lightness + hlsColor.Saturation) - (hlsColor.Lightness * hlsColor.Saturation);
                }
                float num6 = (2f * hlsColor.Lightness) - num5;
                num = HlsValue(num6, num5, hue + 2f);
                num2 = HlsValue(num6, num5, hue);
                num3 = HlsValue(num6, num5, hue - 2f);
            }
            return new RgbColor(hlsColor.Alpha, num, num2, num3);
        }

        private static float HlsValue(float n1, float n2, float hue)
        {
            if (hue < 0f)
            {
                hue += 6f;
            }
            else if (hue >= 6f)
            {
                hue -= 6f;
            }
            if (hue < 1f)
            {
                return (n1 + ((n2 - n1) * hue));
            }
            if (hue < 3f)
            {
                return n2;
            }
            if (hue < 4f)
            {
                return (n1 + ((n2 - n1) * (4f - hue)));
            }
            return n1;
        }

        private static bool IsSimpleBorderBrush(Brush borderBrush)
        {
            SolidColorBrush brush = borderBrush as SolidColorBrush;
            if (brush == null)
            {
                return false;
            }
            if (brush.Color.A != 0xff)
            {
                return (brush.Color.A == 0);
            }
            return true;
        }

        private static bool IsValidBorderStyle(object o)
        {
            MotifBorderStyle style = (MotifBorderStyle)o;
            if (((((style != MotifBorderStyle.None) && (style != MotifBorderStyle.Raised)) &&
                ((style != MotifBorderStyle.RaisedPressed) && (style != MotifBorderStyle.RaisedFocused))) &&
                (((style != MotifBorderStyle.Sunken) && (style != MotifBorderStyle.Etched)) &&
                ((style != MotifBorderStyle.HorizontalLine) && (style != MotifBorderStyle.VerticalLine)))) &&
                ((((style != MotifBorderStyle.TabRight) && (style != MotifBorderStyle.TabTop)) &&
                ((style != MotifBorderStyle.TabLeft) && (style != MotifBorderStyle.TabBottom))) &&
                (((style != MotifBorderStyle.ThinRaised) && (style != MotifBorderStyle.ThinPressed)) &&
                ((style != MotifBorderStyle.AltRaised) && (style != MotifBorderStyle.AltPressed)))))
            {
                return (style == MotifBorderStyle.RadioButton);
            }
            return true;
        }

        private static bool IsValidBorderThickness(object o)
        {
            Thickness thickness = (Thickness)o;
            return (((((thickness.Left >= 0.0) && !double.IsPositiveInfinity(thickness.Left)) && ((thickness.Right >= 0.0) && !double.IsPositiveInfinity(thickness.Right))) && (((thickness.Top >= 0.0) && !double.IsPositiveInfinity(thickness.Top)) && (thickness.Bottom >= 0.0))) && !double.IsPositiveInfinity(thickness.Bottom));
        }

        private static HlsColor RgbToHls(RgbColor rgbColor)
        {
            bool flag;
            bool flag2;
            return RgbToHls(rgbColor, out flag, out flag2);
        }

        private static HlsColor RgbToHls(RgbColor rgbColor, out bool isHueDefined, out bool isSaturationDefined)
        {
            float num = Math.Min(rgbColor.Red, Math.Min(rgbColor.Green, rgbColor.Blue));
            float num2 = Math.Max(rgbColor.Red, Math.Max(rgbColor.Green, rgbColor.Blue));
            float hue = 0f;
            float lightness = (num + num2) / 2f;
            float num5 = 0f;
            if (num == num2)
            {
                isHueDefined = false;
                isSaturationDefined = (lightness > 0f) && (lightness < 1f);
            }
            else
            {
                isHueDefined = isSaturationDefined = true;
                float num6 = num2 - num;
                num5 = (lightness <= 0.5f) ? (num6 / (num + num2)) : (num6 / ((2f - num) - num2));
                num5 = Math.Max(0f, Math.Min(num5, 1f));
                if (rgbColor.Red == num2)
                {
                    hue = (rgbColor.Green - rgbColor.Blue) / num6;
                }
                else if (rgbColor.Green == num2)
                {
                    hue = 2f + ((rgbColor.Blue - rgbColor.Red) / num6);
                }
                else
                {
                    hue = 4f + ((rgbColor.Red - rgbColor.Green) / num6);
                }
                if (hue < 0f)
                {
                    hue += 6f;
                }
                hue /= 6f;
            }
            return new HlsColor(rgbColor.Alpha, hue, lightness, num5);
        }

        private static Thickness ScaleThickness(Thickness t, double s)
        {
            return new Thickness(t.Left * s, t.Top * s, t.Right * s, t.Bottom * s);
        }

        private void DrawAltPressedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (singleThickness.Left + singleThickness.Right)) && (bounds.Height >= (singleThickness.Top + singleThickness.Bottom)))
            {
                this.DrawBorder(this.DarkBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawAltRaisedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (2.0 * (singleThickness.Left + singleThickness.Right))) && (bounds.Height >= (2.0 * (singleThickness.Top + singleThickness.Bottom))))
            {
                this.DrawBorderPair(this.LightBrush, this.DarkDarkBrush, singleThickness, dc, ref bounds);
                this.DrawBorderPair(this.LightLightBrush, this.DarkBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawEtchedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (2.0 * (singleThickness.Left + singleThickness.Right))) && (bounds.Height >= (2.0 * (singleThickness.Top + singleThickness.Bottom))))
            {
                Brush darkBrush = this.DarkBrush;
                Brush lightLightBrush = this.LightLightBrush;
                this.DrawBorderPair(darkBrush, lightLightBrush, singleThickness, dc, ref bounds);
                this.DrawBorderPair(lightLightBrush, darkBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawHorizontalLine(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if (bounds.Height >= (singleThickness.Top + singleThickness.Bottom))
            {
                dc.DrawRectangle(this.DarkBrush, null, new Rect(bounds.Left, bounds.Top, bounds.Width, singleThickness.Top));
                dc.DrawRectangle(this.LightLightBrush, null, new Rect(bounds.Left, bounds.Bottom - singleThickness.Bottom, bounds.Width, singleThickness.Bottom));
                bounds.Y += singleThickness.Top;
                bounds.Height -= singleThickness.Top + singleThickness.Bottom;
            }
        }

        private void DrawRadioButtonBorder(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 12.0) && (bounds.Height >= 12.0))
            {
                dc.DrawGeometry(this.DarkDarkBrush, new Pen(this.DarkBrush, 1.0), TopLeftArcGeometry);
                dc.DrawGeometry(this.LightBrush, new Pen(this.LightLightBrush, 1.0), BottomRightArcGeometry);
                dc.DrawEllipse(this.Background, null, new Point(6.0, 6.0), 4.0, 4.0);
            }
        }

        private void DrawRaisedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (2.0 * (singleThickness.Left + singleThickness.Right))) && (bounds.Height >= (2.0 * (singleThickness.Top + singleThickness.Bottom))))
            {
                this.DrawBorderPair(this.LightLightBrush, this.DarkDarkBrush, singleThickness, dc, ref bounds);
                this.DrawBorderPair(this.LightBrush, this.DarkBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawRaisedPressedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (singleThickness.Left + singleThickness.Right)) && (bounds.Height >= (singleThickness.Top + singleThickness.Bottom)))
            {
                this.DrawBorder(this.DarkBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawSunkenBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (2.0 * (singleThickness.Left + singleThickness.Right))) && (bounds.Height >= (2.0 * (singleThickness.Top + singleThickness.Bottom))))
            {
                this.DrawBorderPair(this.DarkBrush, this.LightLightBrush, singleThickness, dc, ref bounds);
                this.DrawBorderPair(this.DarkDarkBrush, this.LightBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawTabBottom(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 6.0) && (bounds.Height >= 6.0))
            {
                Rect rect = new Rect(0.0, 0.0, bounds.Width, bounds.Height);
                dc.PushTransform(this.GetTabTransform(MotifBorderStyle.TabBottom, bounds.Right, bounds.Bottom));
                dc.DrawGeometry(this.DarkDarkBrush, null, this.GetHighlight1(rect));
                dc.DrawGeometry(this.LightLightBrush, null, this.GetShadow1(rect));
                dc.DrawGeometry(this.DarkBrush, null, this.GetHighlight2(rect));
                dc.DrawGeometry(this.LightBrush, null, this.GetShadow2(rect));
                dc.Pop();
                bounds = HelperDeflateRect(bounds, new Thickness(2.0, 0.0, 2.0, 2.0));
            }
        }

        private void DrawTabLeft(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 6.0) && (bounds.Height >= 6.0))
            {
                Rect rect = new Rect(0.0, 0.0, bounds.Height, bounds.Width);
                dc.PushTransform(this.GetTabTransform(MotifBorderStyle.TabLeft, bounds.Left, bounds.Top));
                dc.DrawGeometry(this.LightLightBrush, null, this.GetHighlight1(rect));
                dc.DrawGeometry(this.DarkDarkBrush, null, this.GetShadow1(rect));
                dc.DrawGeometry(this.LightBrush, null, this.GetHighlight2(rect));
                dc.DrawGeometry(this.DarkBrush, null, this.GetShadow2(rect));
                dc.Pop();
                bounds = HelperDeflateRect(bounds, new Thickness(2.0, 2.0, 0.0, 2.0));
            }
        }

        private void DrawTabRight(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 6.0) && (bounds.Height >= 6.0))
            {
                Rect rect = new Rect(0.0, 0.0, bounds.Height, bounds.Width);
                dc.PushTransform(this.GetTabTransform(MotifBorderStyle.TabRight, bounds.Right, bounds.Bottom));
                dc.DrawGeometry(this.DarkDarkBrush, null, this.GetHighlight1(rect));
                dc.DrawGeometry(this.LightLightBrush, null, this.GetShadow1(rect));
                dc.DrawGeometry(this.DarkBrush, null, this.GetHighlight2(rect));
                dc.DrawGeometry(this.LightBrush, null, this.GetShadow2(rect));
                dc.Pop();
                bounds = HelperDeflateRect(bounds, new Thickness(0.0, 2.0, 2.0, 2.0));
            }
        }

        private void DrawTabTop(DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= 6.0) && (bounds.Height >= 6.0))
            {
                dc.DrawGeometry(this.LightLightBrush, null, this.GetHighlight1(bounds));
                dc.DrawGeometry(this.DarkDarkBrush, null, this.GetShadow1(bounds));
                dc.DrawGeometry(this.LightBrush, null, this.GetHighlight2(bounds));
                dc.DrawGeometry(this.DarkBrush, null, this.GetShadow2(bounds));
                bounds = HelperDeflateRect(bounds, new Thickness(2.0, 2.0, 2.0, 0.0));
            }
        }

        private void DrawThinPressedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (singleThickness.Left + singleThickness.Right)) && (bounds.Height >= (singleThickness.Top + singleThickness.Bottom)))
            {
                this.DrawBorderPair(this.DarkBrush, this.LightLightBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawThinRaisedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if ((bounds.Width >= (singleThickness.Left + singleThickness.Right)) && (bounds.Height >= (singleThickness.Top + singleThickness.Bottom)))
            {
                this.DrawBorderPair(this.LightLightBrush, this.DarkBrush, singleThickness, dc, ref bounds);
            }
        }

        private void DrawVerticalLine(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
        {
            if (bounds.Width >= (singleThickness.Left + singleThickness.Right))
            {
                dc.DrawRectangle(this.DarkBrush, null, new Rect(bounds.Left, bounds.Top, singleThickness.Left, bounds.Height));
                dc.DrawRectangle(this.LightLightBrush, null, new Rect(bounds.Right - singleThickness.Right, bounds.Top, singleThickness.Right, bounds.Height));
                bounds.X += singleThickness.Left;
                bounds.Width -= singleThickness.Left + singleThickness.Right;
            }
        }

        private Geometry GenerateTabTopHighlightGeometry(Rect bounds, bool outerBorder)
        {
            double width = outerBorder ? 3.0 : 2.0;
            double num2 = width - 1.0;
            Size size = new Size(width, width);
            Size size2 = new Size(num2, num2);
            double left = bounds.Left;
            double right = bounds.Right;
            double top = bounds.Top;
            double y = bounds.Bottom - 1.0;
            PathFigure figure = new PathFigure
            {
                StartPoint = new Point(left, y),
                Segments = {
                    new LineSegment(new Point(left, top + width), true),
                    new ArcSegment(new Point(left + width, top), size, 0.0, false, SweepDirection.Clockwise, true),
                    new LineSegment(new Point(right - width, top), true),
                    new ArcSegment(new Point(right - (width * 0.293), top + (width * 0.293)), size, 0.0, false, SweepDirection.Clockwise, true),
                    new LineSegment(new Point((right - 1.0) - (num2 * 0.293), (top + 1.0) + (num2 * 0.293)), true),
                    new ArcSegment(new Point(right - width, top + 1.0), size2, 0.0, false, SweepDirection.Counterclockwise, true),
                    new LineSegment(new Point(left + width, top + 1.0), true),
                    new ArcSegment(new Point(left + 1.0, top + width), size2, 0.0, false, SweepDirection.Counterclockwise, true),
                    new LineSegment(new Point(left + 1.0, y), true)
                },
                IsClosed = true
            };
            figure.Freeze();
            PathGeometry geometry = new PathGeometry
            {
                Figures = { figure }
            };
            geometry.Freeze();
            return geometry;
        }

        private Geometry GenerateTabTopShadowGeometry(Rect bounds, bool outerBorder)
        {
            double width = outerBorder ? 3.0 : 2.0;
            double num2 = width - 1.0;
            Size size = new Size(width, width);
            Size size2 = new Size(num2, num2);
            double right = bounds.Right;
            double top = bounds.Top;
            double y = bounds.Bottom - 1.0;
            PathFigure figure = new PathFigure
            {
                StartPoint = new Point(right - 1.0, y),
                Segments = {
                    new LineSegment(new Point(right - 1.0, top + width), true),
                    new ArcSegment(new Point((right - 1.0) - (num2 * 0.293), (top + 1.0) + (num2 * 0.293)), size2, 0.0, false, SweepDirection.Counterclockwise, true),
                    new LineSegment(new Point(right - (width * 0.293), top + (width * 0.293)), true),
                    new ArcSegment(new Point(right, top + width), size, 0.0, false, SweepDirection.Clockwise, true),
                    new LineSegment(new Point(right, y), true)
                },
                IsClosed = true
            };
            figure.Freeze();
            PathGeometry geometry = new PathGeometry
            {
                Figures = { figure }
            };
            geometry.Freeze();
            return geometry;
        }

        private Geometry GetBorder(Rect bounds, Thickness borderThickness)
        {
            if (this._borderGeometryCache == null)
            {
                this._borderGeometryCache = new BorderGeometryCache();
            }
            if (((this._borderGeometryCache.Bounds != bounds) || (this._borderGeometryCache.BorderThickness != borderThickness)) || (this._borderGeometryCache.BorderGeometry == null))
            {
                this._borderGeometryCache.BorderGeometry = GenerateBorderGeometry(bounds, borderThickness);
                this._borderGeometryCache.Bounds = bounds;
                this._borderGeometryCache.BorderThickness = borderThickness;
            }
            return this._borderGeometryCache.BorderGeometry;
        }

        private double GetClassicBorderThickness()
        {
            if (this.BorderBrush != MotifBorderBrush)
            {
                return 2.0;
            }
            switch (this.BorderStyle)
            {
                case MotifBorderStyle.None:
                case MotifBorderStyle.ThinRaised:
                case MotifBorderStyle.ThinPressed:
                    return 1.0;

                case MotifBorderStyle.Raised:
                case MotifBorderStyle.RaisedPressed:
                case MotifBorderStyle.RaisedFocused:
                    return 3.0;

                case MotifBorderStyle.Sunken:
                case MotifBorderStyle.Etched:
                    return 2.0;

                case MotifBorderStyle.HorizontalLine:
                case MotifBorderStyle.VerticalLine:
                    return 1.0;

                case MotifBorderStyle.TabRight:
                case MotifBorderStyle.TabTop:
                case MotifBorderStyle.TabLeft:
                case MotifBorderStyle.TabBottom:
                    return 2.0;

                case MotifBorderStyle.AltRaised:
                case MotifBorderStyle.AltPressed:
                    return 2.0;
            }
            return 0.0;
        }

        private Geometry GetHighlight1(Rect bounds)
        {
            if (this._tabCache == null)
            {
                this._tabCache = new TabGeometryCache();
            }
            if ((this._tabCache.Bounds != bounds) || (this._tabCache.Highlight1 == null))
            {
                this._tabCache.Highlight1 = this.GenerateTabTopHighlightGeometry(bounds, true);
                this._tabCache.Bounds = bounds;
                this._tabCache.Shadow1 = null;
                this._tabCache.Highlight2 = null;
                this._tabCache.Shadow2 = null;
            }
            return this._tabCache.Highlight1;
        }

        private Geometry GetHighlight2(Rect bounds)
        {
            if (this._tabCache.Highlight2 == null)
            {
                this._tabCache.Highlight2 = this.GenerateTabTopHighlightGeometry(HelperDeflateRect(bounds, new Thickness(1.0, 1.0, 1.0, 0.0)), false);
            }
            return this._tabCache.Highlight2;
        }

        private DoubleCollection GetPixelSnappingGuidelines(double length, double thickness1, double thickness2, int steps)
        {
            DoubleCollection doubles = new DoubleCollection();
            for (int i = 0; i <= steps; i++)
            {
                doubles.Add(i * thickness1);
                doubles.Add(length - (i * thickness2));
            }
            return doubles;
        }

        private Geometry GetShadow1(Rect bounds)
        {
            if (this._tabCache.Shadow1 == null)
            {
                this._tabCache.Shadow1 = this.GenerateTabTopShadowGeometry(bounds, true);
            }
            return this._tabCache.Shadow1;
        }

        private Geometry GetShadow2(Rect bounds)
        {
            if (this._tabCache.Shadow2 == null)
            {
                this._tabCache.Shadow2 = this.GenerateTabTopShadowGeometry(HelperDeflateRect(bounds, new Thickness(1.0, 1.0, 1.0, 0.0)), false);
            }
            return this._tabCache.Shadow2;
        }

        private MatrixTransform GetTabTransform(MotifBorderStyle style, double xOffset, double yOffset)
        {
            if (this._tabCache == null)
            {
                this._tabCache = new TabGeometryCache();
            }
            if (((this._tabCache.Transform == null) || (xOffset != this._tabCache.xOffset)) || (yOffset != this._tabCache.yOffset))
            {
                switch (style)
                {
                    case MotifBorderStyle.TabRight:
                        this._tabCache.Transform = new MatrixTransform(new Matrix(0.0, -1.0, -1.0, 0.0, xOffset, yOffset));
                        break;

                    case MotifBorderStyle.TabLeft:
                        this._tabCache.Transform = new MatrixTransform(new Matrix(0.0, 1.0, 1.0, 0.0, xOffset, yOffset));
                        break;

                    case MotifBorderStyle.TabBottom:
                        this._tabCache.Transform = new MatrixTransform(new Matrix(-1.0, 0.0, 0.0, -1.0, xOffset, yOffset));
                        break;
                }
                this._tabCache.xOffset = xOffset;
                this._tabCache.yOffset = yOffset;
            }
            return this._tabCache.Transform;
        }

        private bool IsTabStyle(MotifBorderStyle style)
        {
            if (((style != MotifBorderStyle.TabLeft) && (style != MotifBorderStyle.TabTop)) && (style != MotifBorderStyle.TabRight))
            {
                return (style == MotifBorderStyle.TabBottom);
            }
            return true;
        }
    }

    internal static class BrushLockHolder
    {
        internal static readonly object BrushLock = new object();
    }
}