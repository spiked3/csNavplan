using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace csNavplan
{
    public class PlanCanvas : Canvas
    {
        static double markerRadius = 5.0;

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }
        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(Brush), typeof(PlanCanvas), new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public Brush AlignPointBrush
        {
            get { return (Brush)GetValue(AlignPointBrushProperty); }
            set { SetValue(AlignPointBrushProperty, value); }
        }
        public static readonly DependencyProperty AlignPointBrushProperty =
            DependencyProperty.Register("AlignPointBrush", typeof(Brush), typeof(PlanCanvas), new PropertyMetadata(new SolidColorBrush(Colors.Yellow)));

        public Typeface Typeface
        {
            get { return (Typeface)GetValue(TypefaceProperty); }
            set { SetValue(TypefaceProperty, value); }
        }
        public static readonly DependencyProperty TypefaceProperty =
            DependencyProperty.Register("Typeface", typeof(Typeface), typeof(PlanCanvas), new PropertyMetadata(new Typeface("Arial")));

        static Brush originBrush = Brushes.White;
        static Brush alignBrush = Brushes.Cyan;

        double NpEmSize = 10.0;
        Brush NpBrush = new SolidColorBrush(Colors.Magenta);

        public Point Pct2CanvasPoint(Point a)
        {
            return new Point(a.X * ActualWidth, a.Y * ActualHeight);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            base.OnRender(dc);

            Plan plan = (DataContext as MainWindow).Plan;
            if (plan == null) return;

            plan.RenderBackground(dc, this);

            DrawPoint(dc, plan.Origin, AlignPointBrush, Foreground);
            DrawPoint(dc, plan.Align1, AlignPointBrush, Foreground);
            DrawPoint(dc, plan.Align2, AlignPointBrush, Foreground);
        }

        void DrawPoint(DrawingContext dc, PlanPoint pp, Brush elipseBrush, Brush labelBrush)
        {
            // AB is the pctg point, XY is the local Coordinates
            FormattedText ft;
            Point tp, p = Pct2CanvasPoint(pp.AB);    

            dc.DrawEllipse(elipseBrush, null, p, markerRadius, markerRadius);

            ft = new FormattedText($"{pp.PointName}", Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, Typeface, NpEmSize, labelBrush);
            tp = new Point(p.X - (ft.Width / 2), p.Y + (markerRadius / 2) + 1);
            dc.DrawText(ft, tp);

            ft = new FormattedText($"({pp.XY.X:F2}, {pp.XY.Y:F2})", Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, Typeface, NpEmSize, labelBrush);
            tp = new Point(p.X - (ft.Width / 2), ft.Height + p.Y + (markerRadius / 2) + 1);
            dc.DrawText(ft, tp);
        }
    }
}
