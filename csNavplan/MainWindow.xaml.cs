using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using uPLibrary.Networking.M2Mqtt;

namespace csNavplan
{
    public partial class MainWindow : RibbonWindow
    {
        public float  RulerLength
        {
            get { return (float )GetValue(RulerLengthProperty); }
            set { SetValue(RulerLengthProperty, value); }
        }
        public static readonly DependencyProperty RulerLengthProperty =
            DependencyProperty.Register("RulerLength", typeof(float ), typeof(MainWindow), new PropertyMetadata(0F));

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
            set { value?.RecalcViewSize(); SetValue(PlanProperty, value); }
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


        double lastMouseRightX, lastMouseRightY;

        #region StatusMessage

        static TextBlock _StatusTextBlock;

        public static void Message(string t)
        {
            if (_StatusTextBlock == null)
                _StatusTextBlock = FindChild<TextBlock>(Application.Current.MainWindow, "StatusTextBlock");
            _StatusTextBlock.Text = t;
        }

        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);

                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
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
            Settings1.Default.gridSpacing = grid1.GridSpacing;
            Settings1.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Width = Settings1.Default.Width;
            Height = Settings1.Default.Height;
            if (Width < 20) Width = 640;
            if (Height < 20) Height = 480;

            spacingCombobox1.SelectedValue = Settings1.Default.gridSpacing.ToString();

            if (Settings1.Default.lastPlan?.Length > 0)
                Plan = Plan.Load(Settings1.Default.lastPlan);
            if (Plan == null)
                New_Click(this, null);

            Plan.Waypoints.CollectionChanged += Waypoints_CollectionChanged;
            WindowTitle = $"Navigation Planner, {Plan.PlanFilename}{(Plan.IsDirty ? '*' : ' ')}";
        }

        private void Waypoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            grid1.InvalidateVisual();
        }

        private void Align1_Click(object sender, RoutedEventArgs e)
        {
            var pctPoint = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            var d = new NavPointEditDlg(pctPoint, null, Plan.CoordinateType);
            d.Owner = this;
            d.Left = lastMouseRightX;
            d.Top = lastMouseRightY;
            d.ShowDialog();
            if (d.DialogResult ?? false)
            {
                Plan.Align1 = d.Final;
                grid1.InvalidateVisual();
                Plan.OnPropertyChanged(null);  
                Plan.RecalcViewSize();
            }
        }

        private void Align2_Click(object sender, RoutedEventArgs e)
        {
            var pctPoint = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            var d = new NavPointEditDlg(pctPoint, null, Plan.CoordinateType);
            d.Owner = this;
            d.Left = lastMouseRightX;
            d.Top = lastMouseRightY;
            d.ShowDialog();
            if (d.DialogResult ?? false)
            {
                Plan.Align2 = d.Final;
                grid1.InvalidateVisual();
                Plan.OnPropertyChanged(null);
                Plan.RecalcViewSize();
            }
        }

        private void Origin_Click(object sender, RoutedEventArgs e)
        {
            if (!Plan.isAligned)
            {
                MessageBox.Show("Set align points first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Point pctPoint = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));

            //---R
            //+ I am here
            
            // todo calculate XY in align point terms
            // NavPointEditDlg to allow tweaking
            // recalc actual pctPoint based on final

            Plan.Origin = new LocalPoint(pctPoint, new Point(0, 0));

            grid1.InvalidateVisual();
            Plan.OnPropertyChanged(null);
            Plan.RecalcViewSize();
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

            // todo some visual indicator as you move a mouse
            //MouseUtm = Plan.Pct2Utm(MousePct);
            //MouseLocal = Plan.Utm2Local(MouseUtm);
            //MouseGps = MouseUtm.AsWgs84();

            if (mouseDragStarted)
            {
                var h = Math.Atan2(grid1.RulerEnd.Value.Y - grid1.RulerStart.Value.Y, grid1.RulerEnd.Value.X - grid1.RulerStart.Value.X);

                RulerHeading = (float)(h * 180 / Math.PI) + 90;

                while (RulerHeading > 180 || RulerHeading < -180)
                    RulerHeading += (RulerHeading > 180) ? -360 : 360;  // normalize

                grid1.RulerEnd = e.GetPosition(grid1);

                // todo some visual indicator as you move a ruler
                //var _endPct = ScreenPoint2Pct(new Point(grid1.RulerEnd.Value.X, grid1.RulerEnd.Value.Y));
                //var _strtPct = ScreenPoint2Pct(new Point(grid1.RulerStart.Value.X, grid1.RulerStart.Value.Y));
                //var _len = Plan.Pct2Local(_endPct) - Plan.Pct2Local(_strtPct);
                //RulerLength = (float)_len.Length;

                grid1.InvalidateVisual();
            }
        }

        private void grid1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                zoom1.Value -= 0.1;
            else
                zoom1.Value += 0.1;

            // hack too zoom at mouse focus, kinda kludgy but works
            grid1.RenderTransformOrigin = MousePct;

            e.Handled = true;
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            Plan.PlanImage = null;
            grid1.InvalidateVisual();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var d = MessageBox.Show(this, "Use World Coordinates?", "Coordinates type", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (d == MessageBoxResult.Cancel)
                return;
            Plan = new Plan();

            Settings1.Default.lastPlan = null;
            Settings1.Default.Save();

            if (d == MessageBoxResult.Yes)
                Plan.CoordinateType = CoordinateType.World;
            else
                Plan.CoordinateType = CoordinateType.Local;

            WindowTitle = $"Navigation Planner, New Plan, {(Plan.CoordinateType == CoordinateType.World ? "World" : "Local")} coordinates";

#if false
            Random r = new Random();
            for (var i = 0; i < 5; i++)
            {
                var w = new NavPoint(new Point(r.Next(100) / 100.0, r.Next(100) / 100.0), 
                    new Utm();
                //w.XY = new Point(r.Next(100)/100.0, r.Next(100)/100.0);
                w.isAction = r.Next(50) < 9;
                Plan.Waypoints.Add(w);
            }
            Plan.Waypoints.Sort(c => c.Sequence);
#endif
            //Plan.Waypoints.CollectionChanged += Waypoints_CollectionChanged;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "JSON Plan Files|*.json|All Files|*.*" };
            if (d.ShowDialog() ?? false)
                Plan = Plan.Load(d.FileName);
            Plan.BackgroundBrush = null;
            grid1.InvalidateVisual();
        }

        void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            Plan.SaveAs(this);
            WindowTitle = $"Navigation Planner, {Plan.PlanFilename}{(Plan.IsDirty ? '*' : ' ')}";
        }

        private void Google_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // todo google - need to extract the image portion
                // or use the API which seems to be an older image

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(new Uri(Clipboard.GetText())));

                SaveFileDialog s = new SaveFileDialog { };
                if (!s.ShowDialog(this) ?? false)
                    return;

                Plan.PlanImage = new PlanImage();
                Plan.PlanImage.FileName = s.FileName;
                using (var filestream = new FileStream(Plan.PlanImage.FileName, FileMode.OpenOrCreate))
                    encoder.Save(filestream);
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        private void ImportImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif|All Files|*.*" };
            if (d.ShowDialog() ?? false)
            {
                Plan.BackgroundBrush = null;
                Plan.PlanImage = new PlanImage { FileName = d.FileName };
                grid1.InvalidateVisual();
            }
        }

        //private void SaveImage_Click(object sender, RoutedEventArgs e)
        //{
        //    SaveFileDialog d = new SaveFileDialog { Filter = "JPG Files|*.jpg", DefaultExt = "jpg" };
        //    if (d.ShowDialog() ?? false)
        //    {
        //        if (Plan.SaveImage(d.FileName))
        //            MainWindow.Message("Image saved");
        //        else
        //            MainWindow.Message("Image save failed!");
        //    }
        //}

        private void AddWaypoint(bool action)
        {
            //var pctPoint = new Point(lastMouseRightX / grid1.ActualWidth, lastMouseRightY / grid1.ActualHeight);
            //var u = Plan.Pct2Utm(pctPoint);
            //NavPoint wp = new NavPoint(pctPoint, u, action);
            //Plan.Waypoints.Add(wp);
            //grid1.InvalidateVisual();
        }

        private void Waypoint_Click(object sender, RoutedEventArgs e)
        {
            AddWaypoint(false);
            MainWindow.Message("Waypoint added");
        }

        private void ActionWaypoint_Click(object sender, RoutedEventArgs e)
        {
            AddWaypoint(true);
            MainWindow.Message("Action waypoint added");
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Message("Test_Click");

            var pctPoint = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));

            Point targetLoc = new Point(lastMouseRightX, lastMouseRightY);
            var w = new Wgs84 { Latitude = -122.3510883, Longitude = 47.6204584 };
            var u = Utm.FromWgs84(w);
            var temp = new WorldPoint(pctPoint, u);
            //var temp = new LocalPoint(pctPoint, new Point(0,0));

            var d = new NavPointEditDlg(pctPoint, temp);

            d.Owner = this;                        
            d.Left = targetLoc.X;
            d.Top = targetLoc.Y;            

            d.ShowDialog();

            if (d.DialogResult ?? false)
            {
                string t = d.Final is WorldPoint ? "WorldPoint" : "LocalPoint";
                MainWindow.Message($"Dialog returned {t} ");
            }

            //System.Diagnostics.Debugger.Break();
        }

        private void PlanToClipboard_Click(object sender, RoutedEventArgs e)
        {
            //Clipboard.SetText(Plan.GetNavCode(RulerHeading));
            Clipboard.SetText(Plan.WayPointsAsJson(RulerHeading));
            MainWindow.Message("Code pushed onto clipboard");
        }

        private void CommandBinding_Save(object sender, ExecutedRoutedEventArgs e)
        {
            Plan.Save(this);
            MainWindow.Message("Saved");
        }

        private void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void OriginRC_Click(object sender, RoutedEventArgs e)
        {
            //Plan.RecalcOrigin();
        }

        private void UtmRectRCRibbonButton_Click(object sender, RoutedEventArgs e)
        {
            Plan.RecalcViewSize();
        }

        private void RibbonGallery_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var t = ((sender as RibbonGallery).SelectedItem as RibbonGalleryItem).Content.ToString();
            double g = double.Parse(t);
            grid1.GridSpacing = g;
            grid1.InvalidateVisual();
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            MqttClient Mq;
            //string broker = "192.168.42.1";
            string broker = "127.0.0.1";
            Mq = new MqttClient(broker);
            Mq.Connect("pNavPlan");

            System.Diagnostics.Trace.WriteLine($"Connected to MQTT @ {broker}", "1");

            Mq.Publish("Navplan/WayPoints", Encoding.ASCII.GetBytes(Plan.WayPointsAsJson(RulerHeading)));
            System.Threading.Thread.Sleep(200);

            Mq.Disconnect();
        }

        private void InvalidateVisual_Click(object sender, RoutedEventArgs e)
        {
            grid1.InvalidateVisual();
        }

        private void ForeGroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (grid1 != null)
                grid1.InvalidateVisual();
        }

 
        private void Color_Click(object sender, RoutedEventArgs e)
        {
            new ColorsDlg().Show();
        }
    }
}