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

// google static maps API key AIzaSyAik-F33VKp4evJfuwDD_YTmh38bBZRcOw
namespace csNavplan
{
    public partial class MainWindow : Window
    {
        static readonly string API_KEY = "AIzaSyAik-F33VKp4evJfuwDD_YTmh38bBZRcOw";

        public double AlignUtmDistance
        {
            get { return (double)GetValue(AlignUtmDistanceProperty); }
            set { SetValue(AlignUtmDistanceProperty, value); }
        }
        public static readonly DependencyProperty AlignUtmDistanceProperty =
            DependencyProperty.Register("AlignUtmDistance", typeof(double), typeof(MainWindow));

        public BitmapImage GridImage
        {
            get { return (BitmapImage)GetValue(GridImageProperty); }
            set { SetValue(GridImageProperty, value); }
        }
        public static readonly DependencyProperty GridImageProperty =
            DependencyProperty.Register("GridImage", typeof(BitmapImage), typeof(MainWindow));

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
            set { SetValue(PlanProperty, value); Refresh(); }
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

        public int GridDivisions
        {
            get { return (int)GetValue(GridDivisionsProperty); }
            set { SetValue(GridDivisionsProperty, value); }
        }
        public static readonly DependencyProperty GridDivisionsProperty =
            DependencyProperty.Register("GridDivisions", typeof(int), typeof(MainWindow), new PropertyMetadata(10));

        public MainWindow()
        {
            InitializeComponent();
        }

        public Point ScreenPoint2Pct(Point a)
        {
            return new Point(a.X / grid1.ActualWidth, a.Y / grid1.ActualHeight);
        }

        private void ImportImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif|All Files|*.*" };
            if (d.ShowDialog() ?? false)
            {
                GridImageBrush = new ImageBrush { ImageSource = new BitmapImage(new Uri(d.FileName)) };
                Plan.ImageFileName = d.FileName;
            }
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
            if (Plan.ImageFileName?.Length > 0)
                GridImageBrush = new ImageBrush { ImageSource = new BitmapImage(new Uri(Plan.ImageFileName)) };
            else
                GridImageBrush = new SolidColorBrush(Colors.Beige);
            Plan.AlignmentPointChanged += Plan_AlignmentPointChanged;
        }

        private void Plan_AlignmentPointChanged(object sender, EventArgs e)
        {
            
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Align1_Click(object sender, RoutedEventArgs e)
        {
            Plan.Align1.AB = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            grid1.InvalidateVisual();
        }

        private void Align2_Click(object sender, RoutedEventArgs e)
        {
            Plan.Align2.AB = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            grid1.InvalidateVisual();
        }

        private void Origin_Click(object sender, RoutedEventArgs e)
        {
            Plan.Origin.XY = new Point(0, 0);
            Plan.Origin.AB = ScreenPoint2Pct(new Point(lastMouseRightX, lastMouseRightY));
            grid1.InvalidateVisual();
        }

        double lastMouseRightX, lastMouseRightY;

        private void grid1_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(grid1);
            var MousePct = new Point(p.X / grid1.ActualWidth, p.Y / grid1.ActualHeight);
            MouseUtm = Plan.Pct2Utm(MousePct);
            MouseGps = Utm.ToLonLat(MouseUtm.X, MouseUtm.Y, "10");    // +++hardcoded zone!!!

            MouseLocal = Plan.Screen2Local(p);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            GridImageBrush = new SolidColorBrush(Colors.Beige);
            Plan.ImageFileName = "";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        void Refresh()
        {
            grid1.InvalidateVisual();
            var a = Plan.Align1.UtmCoord.X - Plan.Align2.UtmCoord.X;
            var b = Plan.Align1.UtmCoord.Y - Plan.Align2.UtmCoord.Y;
            AlignUtmDistance = Math.Sqrt((a * a) + (b * b));
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
            SaveFileDialog d = new SaveFileDialog { Filter = "XML Files|*.xml|All Files|*.*", DefaultExt = "xml" };
            if (d.ShowDialog() ?? false)
            {
                //NavPlan.Filename = d.FileName;
                //NavPlan.Save();
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            new ColorsDlg().Show();
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog { Filter = "JPG Files|*.jpg", DefaultExt = "jpg" };
            if (d.ShowDialog() ?? false)
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(GridImage));
                using (var filestream = new FileStream(d.FileName, FileMode.OpenOrCreate))
                    encoder.Save(filestream);
                Plan.ImageFileName = d.FileName;    // so it will load next time
            }
        }

        async private void Google_Click(object sender, RoutedEventArgs e)
        {           
            string u = "https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&" +
                $"center={Plan.ImageData.GpsCoord.X},{Plan.ImageData.GpsCoord.Y}&" +
                $"size={(int)grid1.ActualWidth}x{(int)grid1.ActualHeight}&" +
                $"zoom={(int)Plan.ImageData.XY.X}&" +
                $"key={API_KEY}";

            using (var client = new HttpClient())
            {
                var response = await client.GetStreamAsync(u);
                GridImage = new BitmapImage();
                GridImage.BeginInit();
                GridImage.CacheOption = BitmapCacheOption.OnLoad;
                GridImage.StreamSource = response;
                grid1.Background = new ImageBrush { ImageSource = GridImage };
                GridImage.EndInit();
            }

            // +++ what aligns do I know at this point?
            // I know center AB(50,50) is center_GPS (x/y above) but that is about it
            // I think heading 0 is y axis
            // I think zoom and target W/H will give me a clue as to scale?

            // https://groups.google.com/forum/#!topic/google-maps-js-api-v3/hDRO4oHVSeM
            //double MetersPerPixel = 156543.03392 * Math.Cos(latLng.lat() * Math.PI / 180) / Math.Pow(2, zoom)
        }
        

        private void grid1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(grid1);
            lastMouseRightX = p.X;
            lastMouseRightY = p.Y;
        }
    }
}