using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Media;

namespace csNavplan
{
    public partial class MainWindow : RibbonWindow
    {
        public float RulerHeading
        {
            get { return (float)GetValue(RulerHeadingProperty); }
            set { SetValue(RulerHeadingProperty, value); }
        }
        public static readonly DependencyProperty RulerHeadingProperty =
            DependencyProperty.Register("RulerHeading", typeof(float), typeof(MainWindow));

        public string WindowTitle
        {
            get { return (string)GetValue(WindowTitleProperty); }
            set { SetValue(WindowTitleProperty, value); }
        }
        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register("WindowTitle", typeof(string), typeof(MainWindow), new PropertyMetadata("csNavPlan - spiked3.com"));

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
            set { value?.RecalcUtmRect(); SetValue(PlanProperty, value); }
        }
        public static readonly DependencyProperty PlanProperty =
            DependencyProperty.Register("Plan", typeof(Plan), typeof(MainWindow));

        public Wgs84 MouseGps
        {
            get { return (Wgs84)GetValue(MouseGpsProperty); }
            set { SetValue(MouseGpsProperty, value); }
        }
        public static readonly DependencyProperty MouseGpsProperty =
            DependencyProperty.Register("MouseGps", typeof(Wgs84), typeof(MainWindow));

        public Utm MouseUtm
        {
            get { return (Utm)GetValue(MouseUtmProperty); }
            set { SetValue(MouseUtmProperty, value); }
        }
        public static readonly DependencyProperty MouseUtmProperty =
            DependencyProperty.Register("MouseUtm", typeof(Utm), typeof(MainWindow), new PropertyMetadata(new Utm()));

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
                    Plan.Save(this);
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
                Plan.Waypoints.CollectionChanged += Waypoints_CollectionChanged;
            }
            else
                New_Click(this, null);

            PlanChanged += MainWindow_PlanChanged;
            WindowTitle = $"Navigation Planner, {Plan.PlanFilename}{(Plan.IsDirty ? '*' : ' ')}";

        }

        private void Plan_AlignmentChanged(object sender, EventArgs e)
        {
            MainWindow_PlanChanged(sender, null);
        }

        private void Waypoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MainWindow_PlanChanged(sender, null);
        }

        private void MainWindow_PlanChanged(object sender, RoutedEventArgs e)
        {
            var a = Plan.Align1.Utm.Easting - Plan.Align2.Utm.Easting;
            var b = Plan.Align1.Utm.Northing - Plan.Align2.Utm.Northing;
            AlignUtmDistance = Math.Sqrt((a * a) + (b * b));

            // uncomment this to see it in Wgs84 - seems to be a bit more accurate, but Utm is within 1/3 meter
            //AlignUtmDistance = Wgs84.gps2m(Plan.Align1.Wgs84.Latitude, Plan.Align1.Wgs84.Longitude, Plan.Align2.Wgs84.Latitude, Plan.Align2.Wgs84.Longitude);
            grid1.InvalidateVisual();
        }

        public event RoutedEventHandler PlanChanged
        {
            add { AddHandler(PlanChangedEvent, value); }
            remove { RemoveHandler(PlanChangedEvent, value); }
        }

        public static readonly RoutedEvent PlanChangedEvent =
            EventManager.RegisterRoutedEvent("PlanChanged", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MainWindow));

        private void Align1_Click(object sender, RoutedEventArgs e)
        {
            Plan.Align1.Pct = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.RecalcUtmRect();
            Plan.RecalcOrigin();
            grid1.InvalidateVisual();
        }

        private void Align2_Click(object sender, RoutedEventArgs e)
        {
            Plan.Align2.Pct = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.RecalcUtmRect();
            Plan.RecalcOrigin();
            grid1.InvalidateVisual();
        }

        private void Origin_Click(object sender, RoutedEventArgs e)
        {
            Plan.Origin.Local = new Point(0, 0);
            Plan.Origin.Pct = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.RecalcOrigin();
            grid1.InvalidateVisual();
        }

        bool mouseDragStarted = false;

        private void grid1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseDragStarted = false;
            grid1.RulerStart = grid1.RulerEnd = null;
        }

        private void grid1_MouseLeave(object sender, MouseEventArgs e)
        {
            grid1_MouseLeftButtonUp(sender, null);
        }

        private void grid1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDragStarted = true;
            grid1.RulerEnd = grid1.RulerStart = e.GetPosition(grid1);
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
            MouseGps = MouseUtm.AsWgs84();

            if (mouseDragStarted)
            {
                // todo needs normalizing -179/180
                var h = Math.Atan2(grid1.RulerEnd.Value.Y - grid1.RulerStart.Value.Y, grid1.RulerEnd.Value.X - grid1.RulerStart.Value.X);
                RulerHeading = (float)(h * 180 / Math.PI) + 90;
                grid1.RulerEnd = e.GetPosition(grid1);
                grid1.InvalidateVisual();
            }
        }

        private void grid1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                zoom1.Value -= 0.1;
            else
                zoom1.Value += 0.1;

            // hack kinda kludgy but works kinda
            grid1.RenderTransformOrigin = MousePct;

            e.Handled = true;
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            Plan.ImageFileName = "";
            Plan.ImageData.Wgs84 = new Wgs84(); 
            Plan.BackgroundBrush = null;
            grid1.InvalidateVisual();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Plan = new Plan();

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

            Plan.Waypoints.CollectionChanged += Waypoints_CollectionChanged;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "XML Plan Files|*.xml|All Files|*.*" };
            if (d.ShowDialog() ?? false)
                Plan = Plan.Load(d.FileName);
            grid1.InvalidateVisual();
        }

        void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            Plan.SaveAs(this);
            WindowTitle = $"Navigation Planner, {Plan.PlanFilename}{(Plan.IsDirty ? '*' : ' ')}";
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
            {
                if (Plan.SaveImage(d.FileName))
                    Status = "Image saved";
                else
                    Status = "Image save failed!";
            }
        }

        private void Waypoint_Click(object sender, RoutedEventArgs e)
        {
            Waypoint wp = new Waypoint { isAction = false };
            wp.XY = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.Waypoints.Add(wp);
            Status = "Waypoint added";
        }

        private void ActionWaypoint_Click(object sender, RoutedEventArgs e)
        {
            Waypoint wp = new Waypoint { isAction = true };
            wp.XY = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            Plan.Waypoints.Add(wp);
            Status = "Action Waypoint added";
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            Status = "Test_Click";
            var w = new Wgs84 { Latitude = -122.3510883, Longitude = 47.6204584 };
            var u = Utm.FromWgs84(w);

            System.Diagnostics.Debugger.Break();
        }

        private void PlanToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Plan.GetNavCode(RulerHeading));
            Status = "Code pushed onto clipboard";
        }

        private void CommandBinding_Save(object sender, ExecutedRoutedEventArgs e)
        {
            Plan.Save(this);
            Status = "Saved";
        }

        private void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            new ColorsDlg().Show();
        }
    }
}