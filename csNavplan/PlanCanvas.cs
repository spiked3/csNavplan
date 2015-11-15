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

            if (plan.Origin != null) DrawPlanPoint(dc, plan.Origin, plan);
            if (plan.Align1 != null) DrawPlanPoint(dc, plan.Align1, plan);
            if (plan.Align2 != null) DrawPlanPoint(dc, plan.Align2, plan);

            foreach (var w in plan.Waypoints)
                DrawWaypoint(dc, w, plan);

            if (RulerStart != null && RulerEnd != null)
                dc.DrawLine(RulerPen, RulerStart.Value, RulerEnd.Value);
        }

        private void DrawWaypoint(DrawingContext dc, BasePoint p, Plan plan)
        {
            // todo DrawWaypoint
            //FormattedText ft;

            //Point drawPoint = new Point(plan.PlanImage.pctPoint.X * ActualWidth, plan.PlanImage.pctPoint.X * ActualHeight);

            //dc.DrawEllipse(p.isAction ? ActionWpBrush : WpBrush,
            //    null, drawPoint, markerRadius, markerRadius);

            //ft = new FormattedText($"{p.Idx}", Thread.CurrentThread.CurrentUICulture,
            //    FlowDirection.LeftToRight, Typeface, NpEmSize, Brushes.Black);      // todo harcoded brush
            //Point textPoint = new Point(drawPoint.X - (ft.Width / 2), drawPoint.Y - markerRadius - 1);
            //dc.DrawText(ft, textPoint);

            //Point local = p.GetLocalXY(plan.Origin);
            //ft = new FormattedText($"({local.X:F2}, {local.Y:F2})", Thread.CurrentThread.CurrentUICulture,
            //    FlowDirection.LeftToRight, Typeface, NpEmSize, Foreground);
            //textPoint = new Point(drawPoint.X - (ft.Width / 2), drawPoint.Y + 4);
            //dc.DrawText(ft, textPoint);
        }

        void DrawPlanPoint(DrawingContext dc, BasePoint p, Plan plan)
        {
            // todo DrawPlanPoint
            // AB is the pctg point, XY is the local Coordinates
            //FormattedText ft;
            //Point drawPoint = Pct2CanvasPoint(p.GetLocalXY(Plan.Origin);

            //dc.DrawEllipse(AlignPointBrush, null, drawPoint, markerRadius, markerRadius);

            //ft = new FormattedText($"{p.Name}", Thread.CurrentThread.CurrentUICulture,
            //    FlowDirection.LeftToRight, Typeface, NpEmSize, Foreground);
            //Point tp = new Point(drawPoint.X - (ft.Width / 2), drawPoint.Y + (markerRadius / 2) + 1);
            //dc.DrawText(ft, tp);
        }
    }
}
