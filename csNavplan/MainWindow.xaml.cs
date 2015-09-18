using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace csNavplan
{
    public partial class MainWindow : Window
    {
        public double AlignUtmDistance
        {
            get { return (double)GetValue(AlignUtmDistanceProperty); }
            set { SetValue(AlignUtmDistanceProperty, value); }
        }
        public static readonly DependencyProperty AlignUtmDistanceProperty =
            DependencyProperty.Register("AlignUtmDistance", typeof(double), typeof(MainWindow));

        public string Filename
        {
            get { return (string)GetValue(FilenameProperty); }
            set { SetValue(FilenameProperty, value); }
        }
        public static readonly DependencyProperty FilenameProperty =
            DependencyProperty.Register("Filename", typeof(string), typeof(MainWindow));

        public Plan Plan
        {
            get { return (Plan)GetValue(PlanProperty); }
            set { value?.OnAlignmentPointChanged(); SetValue(PlanProperty, value); }
        }
        public static readonly DependencyProperty PlanProperty =
            DependencyProperty.Register("Plan", typeof(Plan), typeof(MainWindow));

        public Point MouseGps
        {
            get { return (Point)GetValue(MouseGpsProperty); }
            set { SetValue(MouseGpsProperty, value); }
        }
        public static readonly DependencyProperty MouseGpsProperty =
            DependencyProperty.Register("MouseGps", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(0,0)));

        public Point MouseUtm
        {
            get { return (Point)GetValue(MouseUtmProperty); }
            set { SetValue(MouseUtmProperty, value); }
        }
        public static readonly DependencyProperty MouseUtmProperty =
            DependencyProperty.Register("MouseUtm", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(0,0)));

        public Point MouseLocal
        {
            get { return (Point)GetValue(MouseLocalProperty); }
            set { SetValue(MouseLocalProperty, value); }
        }
        public static readonly DependencyProperty MouseLocalProperty =
            DependencyProperty.Register("MouseLocal", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(0,0)));

        public Point MouseXY
        {
            get { return (Point)GetValue(MouseXYProperty); }
            set { SetValue(MouseXYProperty, value); }
        }
        public static readonly DependencyProperty MouseXYProperty =
            DependencyProperty.Register("MouseXY", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(0,0)));

        public Point MousePct
        {
            get { return (Point)GetValue(MousePctProperty); }
            set { SetValue(MousePctProperty, value); }
        }
        public static readonly DependencyProperty MousePctProperty =
            DependencyProperty.Register("MousePct", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(0,0)));

        public Brush GridImageBrush
        {
            get { return (Brush)GetValue(GridImageBrushProperty); }
            set { SetValue(GridImageBrushProperty, value); }
        }
        public static readonly DependencyProperty GridImageBrushProperty =
            DependencyProperty.Register("GridImageBrush", typeof(Brush), typeof(MainWindow));

        public Object PropertyGridObject
        {
            get { return (Object)GetValue(PropertyGridObjectProperty); }
            set { SetValue(PropertyGridObjectProperty, value); }
        }
        public static readonly DependencyProperty PropertyGridObjectProperty =
            DependencyProperty.Register("PropertyGridObject", typeof(Object), typeof(MainWindow));

        static TextBlock _statusBarTextBlock;
        public static string Status {  set { _statusBarTextBlock.Text = value; } }

        double lastMouseRightX, lastMouseRightY;

        public MainWindow()
        {
            InitializeComponent();
            _statusBarTextBlock = StatusBarTextBlock;
        }

        public Point ScreenPoint2Pct(Point a)
        {
            return new Point(a.X / grid1.ActualWidth, a.Y / grid1.ActualHeight);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Plan.IsDirty)
            {
                var rc = MessageBox.Show("Plan has changed, save?", "Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (rc == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (rc == MessageBoxResult.Yes)
                    Plan.Save();
            }

            if (Plan.PlanFilename?.Length > 0)
                Settings1.Default.lastPlan = Plan.PlanFilename;
            else
                Settings1.Default.lastPlan = null;
            Settings1.Default.Width = (float)Width;
            Settings1.Default.Height = (float)Height;
            Settings1.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Width = Settings1.Default.Width;
            Height = Settings1.Default.Height;
            if (Width < 20) Width = 640;
            if (Height < 20) Height = 480;

            if (Settings1.Default.lastPlan?.Length > 0)
            {
                Plan = Plan.Load(Settings1.Default.lastPlan);
                Plan.AlignmentChanged += Plan_AlignmentChanged;
                Plan.OnAlignmentPointChanged();
            }
            else
                New_Click(this, null);
        }

        private void Waypoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            grid1.InvalidateVisual();
        }

        private void Plan_AlignmentChanged(object sender, EventArgs e)
        {
            var a = Plan.Align1.UtmCoord.X - Plan.Align2.UtmCoord.X;
            var b = Plan.Align1.UtmCoord.Y - Plan.Align2.UtmCoord.Y;
            AlignUtmDistance = Math.Sqrt((a * a) + (b * b));
            grid1.InvalidateVisual();
        }

        private void Align1_Click(object sender, RoutedEventArgs e)
        {
            Plan.Align1.AB = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.OnAlignmentPointChanged();
            grid1.InvalidateVisual();
        }

        private void Align2_Click(object sender, RoutedEventArgs e)
        {
            Plan.Align2.AB = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.OnAlignmentPointChanged();
            grid1.InvalidateVisual();
        }

        private void Origin_Click(object sender, RoutedEventArgs e)
        {
            Plan.Origin.XY = new Point(0, 0);
            Plan.Origin.AB = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.Origin.UtmCoord = Plan.Pct2Utm(Plan.Origin.AB);
            Plan.Origin.GpsCoord = Utm.ToLonLat(Plan.Origin.UtmCoord.X, Plan.Origin.UtmCoord.Y, "10");
            Plan.OnAlignmentPointChanged();
            grid1.InvalidateVisual();
        }

        private void grid1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(grid1);
            lastMouseRightX = p.X;
            lastMouseRightY = p.Y;
        }

        private void grid1_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(grid1);
            MousePct = new Point(p.X / grid1.ActualWidth, p.Y / grid1.ActualHeight);
            MouseXY = p;            
            MouseUtm = Plan.Pct2Utm(MousePct);
            MouseLocal = Plan.Utm2Local(MouseUtm);
            MouseGps = Utm.ToLonLat(MouseUtm.X, MouseUtm.Y, "10");    // hack hardcoded zone!!!
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            Plan.ImageFileName = "";
            Plan.ImageData.GpsCoord = new Point(0, 0);
            Plan.BackgroundBrush = null;
            grid1.InvalidateVisual();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Plan = new Plan();
            Plan.AlignmentChanged += Plan_AlignmentChanged;

            // todo stub
#if true
            Random r = new Random();
            for (var i = 0; i < 32; i++)
            {
                var w = new Waypoint();
                w.XY = new Point(r.Next(100)/100.0, r.Next(100)/100.0);
                w.isAction = r.Next(50) < 9;
                Plan.Waypoints.Add(w);
            }
            Plan.Waypoints.Sort(c => c.Sequence);
#endif

            Plan.ResetSequenceNumbers();
            Plan.AlignmentChanged += Plan_AlignmentChanged;
            Plan.Waypoints.CollectionChanged += Waypoints_CollectionChanged;

            Plan.OnAlignmentPointChanged(); // cause recalc
            grid1.InvalidateVisual();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "XML Plan Files|*.xml|All Files|*.*" };
            if (d.ShowDialog() ?? false)
                Plan = Plan.Load(d.FileName);
            grid1.InvalidateVisual();
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            Plan.Save();
        }

        void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            Plan.SaveAs();
        }

        private void Google_Click(object sender, RoutedEventArgs e)
        {
            Plan.BackgroundBrush = null;
            Plan.ImageFileName = "";
            grid1.InvalidateVisual();
        }

        private void ImportImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif|All Files|*.*" };
            if (d.ShowDialog() ?? false)
                Plan.ImageFileName = d.FileName;                
            grid1.InvalidateVisual();
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog { Filter = "JPG Files|*.jpg", DefaultExt = "jpg" };
            if (d.ShowDialog() ?? false)
                Plan.SaveImage(d.FileName);
        }

        private void Waypoint_Click(object sender, RoutedEventArgs e)
        {
            Waypoint wp = new Waypoint { isAction = false };
            wp.XY = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.Waypoints.Add(wp);
        }

        private void ActionWaypoint_Click(object sender, RoutedEventArgs e)
        {
            Waypoint wp = new Waypoint { isAction = true };
            wp.XY = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.Waypoints.Add(wp);
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debugger.Break();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            new ColorsDlg().Show();
        }
    }
}