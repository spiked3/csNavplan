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

        public int GridDivisions
        {
            get { return (int)GetValue(GridDivisionsProperty); }
            set { SetValue(GridDivisionsProperty, value); }
        }
        public static readonly DependencyProperty GridDivisionsProperty =
            DependencyProperty.Register("GridDivisions", typeof(int), typeof(PlanCanvas), new PropertyMetadata(10));

        static Pen gridPen = new Pen(Brushes.Gray, 1.0);
        static Brush originBrush = Brushes.White;
        static Brush alignBrush = Brushes.Cyan;

        Typeface NpTypeface = new Typeface("Arial");
        double NpEmSize = 10.0;
        Brush NpBrush = new SolidColorBrush(Colors.Magenta);

        public Point Pct2CanvasPoint(Point a)
        {
            return new Point(a.X * ActualWidth, a.Y * ActualHeight);
        }

        protected override void OnRender(DrawingContext dc)
        {
            FormattedText ft;
            Point tp;

            base.OnRender(dc);

            if (DesignerProperties.GetIsInDesignMode(this)) return;

            Plan plan = (DataContext as MainWindow).Plan;
            if (plan == null) return;

            for (double x = 0, y = 0; x < ActualWidth; x += ActualWidth / GridDivisions, y += ActualHeight / GridDivisions)
            {
                dc.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
                dc.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
            }

            Point o = Pct2CanvasPoint(plan.Origin.AB);
            dc.DrawEllipse(alignBrush, null, (Point)o, markerRadius, markerRadius);
            ft = new FormattedText($"({plan.Origin.XY.X:F2}, {plan.Origin.XY.Y:F2})", Thread.CurrentThread.CurrentUICulture, 
                FlowDirection.LeftToRight, NpTypeface, NpEmSize, NpBrush);
            tp = new Point(o.X - (ft.Width/2) , o.Y + (markerRadius/2) + 1);
            dc.DrawText(ft, tp);

            Point a1 = Pct2CanvasPoint(plan.Align1.AB);
            dc.DrawEllipse(alignBrush, null, (Point)a1, markerRadius, markerRadius);
            ft = new FormattedText($"({plan.Align1.XY.X:F2}, {plan.Align1.XY.Y:F2})", Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, NpTypeface, NpEmSize, NpBrush);
            tp = new Point(a1.X - (ft.Width / 2), a1.Y + (markerRadius / 2) + 1);
            dc.DrawText(ft, tp);

            Point a2 = Pct2CanvasPoint(plan.Align2.AB);
            dc.DrawEllipse(alignBrush, null, (Point)a2, markerRadius, markerRadius);
            ft = new FormattedText($"({plan.Align2.XY.X:F2}, {plan.Align2.XY.Y:F2})", Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, NpTypeface, NpEmSize, NpBrush);
            tp = new Point(a2.X - (ft.Width / 2), a2.Y + (markerRadius / 2) + 1);
            dc.DrawText(ft, tp);
        }
    }
}
