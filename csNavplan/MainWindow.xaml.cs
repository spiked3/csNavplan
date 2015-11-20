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
    // todo add an exit button on ribbon bar
    // todo When editting local points, the numbers shown should be Origin relative, not view relative
    // this might be harder than you think, but would help a few places
    // every basePoint, must have a reference to plan, but then has access to origin, instead of requiring a parameter.
    // I think we do not create BasePoints now unless we are aligned+origin, so should be ok.
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
            set { value?.RecalcView(); SetValue(PlanProperty, value); }
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
            DependencyProperty.Register("MouseUtm", typeof(Utm), typeof(MainWindow));

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

        public Point MousePctA
        {
            get { return (Point)GetValue(MousePctProperty); }
            set { SetValue(MousePctProperty, value); }
        }
        public static readonly DependencyProperty MousePctProperty =
            DependencyProperty.Register("MousePctA", typeof(Point), typeof(MainWindow), new PropertyMetadata(new Point(0,0)));

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
            {
                Plan = Plan.Load(Settings1.Default.lastPlan);
                //?ForegroundColorPicker.SelectedColorText = Plan.ForegroundColor;
            }
            if (Plan == null)
                New_Click(this, null);

            Plan.WayPoints.CollectionChanged += Waypoints_CollectionChanged;
            grid1.InvalidateVisual();
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
                Plan.RecalcView();
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
                Plan.RecalcView();
            }
        }

        private void Origin_Click(object sender, RoutedEventArgs e)
        {
            if (!Plan.isAligned)
            {
                MessageBox.Show("Set align points first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Point pp = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));

            BasePoint estimate = Plan.NavPointAtPctPoint(pp);

            var d = new NavPointEditDlg(pp, estimate, Plan.CoordinateType);
            d.Owner = this;
            d.Left = lastMouseRightX;
            d.Top = lastMouseRightY;
            d.ShowDialog();
            if (d.DialogResult ?? false)
            {
                //+ I am here
                d.Final.PctPoint = Plan.PctPointAtNavPoint(d.Final);
                Plan.Origin = d.Final;

                grid1.InvalidateVisual();
                Plan.OnPropertyChanged(null);
                Plan.RecalcView();
            }
        }

        bool mouseDragStarted = false;

        private void grid1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseDragStarted = false;
        }

        private void grid1_MouseLeave(object sender, MouseEventArgs e)
        {
            grid1_MouseLeftButtonUp(sender, null);
        }

        private void grid1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDragStarted = true;
            if (Plan.isOriginValid)
                Plan.RulerEnd = Plan.RulerStart = Plan.NavPointAtPctPoint(ScreenPoint2Pct(e.GetPosition(grid1)));
        }

        private void grid1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(grid1);
            lastMouseRightX = p.X;
            lastMouseRightY = p.Y;
        }

        private void grid1_MouseMove(object sender, MouseEventArgs e)
        {
            if (Plan == null || !Plan.isAligned)
                return;

            // todo some visual indicators as mouse moves

            MouseXY = e.GetPosition(grid1);
            MousePctA = new Point(MouseXY.X / grid1.ActualWidth, MouseXY.Y / grid1.ActualHeight);

            if (Plan.Origin == null)
                return;

            MouseLocal = Plan.NavPointAtPctPoint(MousePctA).GetLocalXY(Plan.Origin);

            if (mouseDragStarted)
            {
                //var h = Math.Atan2(Plan.RulerEnd.Value.Y - Plan.RulerStart.Value.Y, Plan.RulerEnd.Value.X - Plan.RulerStart.Value.X);

                //RulerHeading = (float)(h * 180 / Math.PI) + 90;

                //while (RulerHeading > 180 || RulerHeading < -180)
                //    RulerHeading += (RulerHeading > 180) ? -360 : 360;  // normalize

                //Plan.RulerEnd = e.GetPosition(grid1);

                //// todo some visual indicator as you move a ruler
                //var xlen = (Plan.RulerEnd?.X ?? 0 * grid1.ActualWidth - Plan.RulerStart?.X ?? 0 * grid1.ActualWidth);
                //var ylen = (Plan.RulerEnd?.Y ?? 0 * grid1.ActualHeight - Plan.RulerStart?.Y ?? 0 * grid1.ActualHeight);
                //var l = (float) (xlen * xlen + ylen * ylen);
                //RulerLength = l;

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
            grid1.RenderTransformOrigin = MousePctA;

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

        // todo why arent we a command binding?
        void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            //Plan.ForegroundColor = ForegroundColorPicker.SelectedColor.ToString();
            Plan.SaveAs(this);
            WindowTitle = $"Navigation Planner, {Plan.PlanFilename}{(Plan.IsDirty ? '*' : ' ')}";
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

        private void Google_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // todo google - need to extract the image portion
                // or use the API which seems to be an older image

                //else if (PlanImage.originalWgs84 != null)
                //{
                //    PlanImage.FileName = ""; // indicates we did not load an image from disk

                //    //GoogleUri = "https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&" +
                //    //    $"center={PlanImage.originalWgs84.Latitude},{PlanImage.originalWgs84.Longitude}&" +
                //    //    $"size={(int)pc.ActualWidth}x{(int)pc.ActualHeight}&" +
                //    //    $"zoom={zoom}&" +
                //    //    $"key={API_KEY}";

                //    BackgroundBrush = (ImageBrush)PlanImage;
                //    PlanImage.Width = pc.ActualWidth;
                //    PlanImage.Height = pc.ActualHeight;
                //    MainWindow.Message("Fetched GoogleMap for background");
                //}


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

        private void AddWaypoint(bool action)
        {
            if (!Plan.isAligned)
            {
                MessageBox.Show("Set align points first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Point pp = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));

            BasePoint estimate = Plan.NavPointAtPctPoint(pp);

            var d = new NavPointEditDlg(pp, estimate, Plan.CoordinateType);
            d.Owner = this;
            d.Left = lastMouseRightX;
            d.Top = lastMouseRightY;
            d.ShowDialog();
            if (d.DialogResult ?? false)
            {
                d.Final.PctPoint = Plan.PctPointAtNavPoint(d.Final);
                Plan.WayPoints.Add(d.Final);
                grid1.InvalidateVisual();
            }
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

            var pp = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));

            Point targetLoc = new Point(lastMouseRightX, lastMouseRightY);
            var w = new Wgs84(-122.198157,47.498459);
            var u = Utm.FromWgs84(w);

            //var temp = new LocalPoint(pctPoint, new Point(0,0));
            var temp = new WorldPoint(pp, u);
            //var temp = NavPointAtPctPoint(pp);

            var d = new NavPointEditDlg(pp, temp);

            d.Owner = this;                        
            d.Left = targetLoc.X;
            d.Top = targetLoc.Y;            

            d.ShowDialog();

            if (d.DialogResult ?? false)
            {
                string t = d.Final is WorldPoint ? "WorldPoint" : "LocalPoint";
                MainWindow.Message($"Dialog returned {t} ");
            }
        }

        private void PlanToClipboard_Click(object sender, RoutedEventArgs e)
        {
            //Clipboard.SetText(Plan.GetNavCode(RulerHeading));
            Clipboard.SetText(Plan.WayPointsAsJson(RulerHeading));
            MainWindow.Message("Code pushed onto clipboard");
        }

        private void CommandBinding_Save(object sender, ExecutedRoutedEventArgs e)
        {
            //Plan.ForegroundColor = ForegroundColorPicker.SelectedColor.ToString();
            Plan.Save(this);
            MainWindow.Message("Saved");
        }

        private void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void RecalcView_Click(object sender, RoutedEventArgs e)
        {
            Plan.RecalcView();
        }

        private void GridSize_Changed(object sender, RoutedPropertyChangedEventArgs<object> e)
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

            var j = Plan.WayPointsAsJson(RulerHeading);
            Mq.Publish("Navplan/WayPoints", Encoding.ASCII.GetBytes(j));
            Clipboard.SetText(j);
            System.Threading.Thread.Sleep(200);

            Mq.Disconnect();
        }

        private void InvalidateVisual_Click(object sender, RoutedEventArgs e)
        {
            grid1.InvalidateVisual();
        }

        private void ForegroundColorPicker_Changed(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (grid1 != null)
            {
                grid1.Foreground = new SolidColorBrush(((Xceed.Wpf.Toolkit.ColorPicker)sender).SelectedColor ?? Colors.Black);
                grid1.InvalidateVisual();
            }
        }

        private void GridColorPicker_Changed(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (Plan != null)
                Plan.GridPen = new Pen(new SolidColorBrush(((Xceed.Wpf.Toolkit.ColorPicker)sender).SelectedColor ?? Colors.Gray), .5);
            if (grid1 != null)
                grid1.InvalidateVisual();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            new ColorsDlg().Show();
        }
    }
}