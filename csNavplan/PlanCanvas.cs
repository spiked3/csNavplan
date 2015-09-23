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

        public double GridSpacing
        {
            get { return (double)GetValue(GridSpacingProperty); }
            set { SetValue(GridSpacingProperty, value); }
        }
        public static readonly DependencyProperty GridSpacingProperty =
            DependencyProperty.Register("GridSpacing", typeof(double), typeof(PlanCanvas), new PropertyMetadata(10.0));


        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }
        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(Brush), typeof(PlanCanvas), new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public Brush ActionWpBrush
        {
            get { return (Brush)GetValue(ActionWpBrushProperty); }
            set { SetValue(ActionWpBrushProperty, value); }
        }
        public static readonly DependencyProperty ActionWpBrushProperty =
            DependencyProperty.Register("ActionWpBrush", typeof(Brush), typeof(PlanCanvas), new PropertyMetadata(new SolidColorBrush(Colors.Red)));

        public Brush WpBrush
        {
            get { return (Brush)GetValue(WpBrushProperty); }
            set { SetValue(WpBrushProperty, value); }
        }
        public static readonly DependencyProperty WpBrushProperty =
            DependencyProperty.Register("WpBrush", typeof(Brush), typeof(PlanCanvas), new PropertyMetadata(new SolidColorBrush(Colors.Orange)));

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

        public Point? RulerStart { get; set; }
        public Point? RulerEnd { get; set; }

        static Brush originBrush = Brushes.White;
        static Brush alignBrush = Brushes.Cyan;
        static Pen RulerPen = new Pen(Brushes.White, 4.0);

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

            plan.RenderBackground(dc, this, GridSpacing);

            DrawPlanPoint(dc, plan.Origin);
            DrawPlanPoint(dc, plan.Align1);
            DrawPlanPoint(dc, plan.Align2);

            foreach (var w in plan.Waypoints)
                DrawWaypoint(dc, w);

            if (RulerStart != null && RulerEnd != null)
                dc.DrawLine(RulerPen, RulerStart.Value, RulerEnd.Value);
        }

        private void DrawWaypoint(DrawingContext dc, Waypoint wp)
        {
            FormattedText ft;
            Point tp, p = new Point(wp.XY.X * ActualWidth, wp.XY.Y * ActualHeight);
            Plan plan = (DataContext as MainWindow).Plan;

            if (plan == null) return;

            dc.DrawEllipse(wp.isAction ? ActionWpBrush : WpBrush,
                null, p, markerRadius, markerRadius);

            ft = new FormattedText($"{wp.Sequence}", Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, Typeface, NpEmSize, Brushes.Black);      // todo harcoded brush
            tp = new Point(p.X - (ft.Width / 2), p.Y - markerRadius - 1);
            dc.DrawText(ft, tp);

            var localXY = plan.Pct2Local(wp.XY);

            ft = new FormattedText($"({localXY.X:F2}, {localXY.Y:F2})", Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, Typeface, NpEmSize, Foreground);
            tp = new Point(p.X - (ft.Width / 2), p.Y + 4);
            dc.DrawText(ft, tp);
        }

        void DrawPlanPoint(DrawingContext dc, PlanPoint pp)
        {
            // AB is the pctg point, XY is the local Coordinates
            FormattedText ft;
            Point tp, p = Pct2CanvasPoint(pp.Pct);

            dc.DrawEllipse(AlignPointBrush, null, p, markerRadius, markerRadius);

            ft = new FormattedText($"{pp.PointName}", Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, Typeface, NpEmSize, Foreground);
            tp = new Point(p.X - (ft.Width / 2), p.Y + (markerRadius / 2) + 1);
            dc.DrawText(ft, tp);
        }
    }
}
