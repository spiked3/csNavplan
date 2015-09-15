using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Windows.Controls.Primitives;

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
            Settings1.Default.Width = (float)Width;
            Settings1.Default.Height = (float)Height;
            Settings1.Default.Plan = JsonConvert.SerializeObject(Plan);
            Settings1.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Width = Settings1.Default.Width;
            Height = Settings1.Default.Height;
            if (Width < 20) Width = 640;
            if (Height < 20) Height = 480;

            Plan = JsonConvert.DeserializeObject<Plan>(Settings1.Default.Plan);

            if (Plan == null)
                Plan = new Plan();

            Plan.AlignmentChanged += Plan_AlignmentChanged;
            Plan.OnAlignmentPointChanged(); // cause recalc
            grid1.InvalidateVisual();
        }

        private void Plan_AlignmentChanged(object sender, EventArgs e)
        {
            var a = Plan.Align1.UtmCoord.X - Plan.Align2.UtmCoord.X;
            var b = Plan.Align1.UtmCoord.Y - Plan.Align2.UtmCoord.Y;
            AlignUtmDistance = Math.Sqrt((a * a) + (b * b));
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

        double lastMouseRightX, lastMouseRightY;

        private void grid1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(grid1);
            lastMouseRightX = p.X;
            lastMouseRightY = p.Y;
        }

        private void grid1_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(grid1);
            var MousePct = new Point(p.X / grid1.ActualWidth, p.Y / grid1.ActualHeight);
            MouseUtm = Plan.Pct2Utm(MousePct);
            MouseLocal = Plan.Utm2Local(MouseUtm);
            MouseGps = Utm.ToLonLat(MouseUtm.X, MouseUtm.Y, "10");    // +++hardcoded zone!!!
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Plan.ImageFileName = "";
            Plan.ImageData.GpsCoord = new Point(0, 0);
            Plan.BackgroundBrush = null;
            grid1.InvalidateVisual();
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Filename))
                SaveAs_Click(sender, e);
            else
                /* NavPlan.Save() */ ;
        }

        void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
            SaveFileDialog d = new SaveFileDialog { Filter = "XML Files|*.xml|All Files|*.*", DefaultExt = "xml" };
            if (d.ShowDialog() ?? false)
            {
                //NavPlan.Filename = d.FileName;
                //NavPlan.Save();
            }
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

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            new ColorsDlg().Show();
        }
    }
}