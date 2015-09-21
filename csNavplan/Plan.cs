using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace csNavplan
{
    public class Plan : INotifyPropertyChanged
    {
        static readonly string API_KEY = "AIzaSyAik-F33VKp4evJfuwDD_YTmh38bBZRcOw";

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        public bool IsDirty { get { return _IsDirty; } set { _IsDirty = value; OnPropertyChanged(); } }
        bool _IsDirty = false; 

        public string PlanFilename { get { return _PlanFilename; } set { _PlanFilename = value; OnPropertyChanged(); } }
        string _PlanFilename;

        public string ImageFileName { get { return _ImageFileName; } set { _ImageFileName = value; OnPropertyChanged(); RecalcUtmRect(); } }
        string _ImageFileName;

        // imagedata stores the Wgs84 of image center used to fetch from google        
        // AB is the target size 
        public PlanPoint ImageData { get { return _ImageData; } set { _ImageData = value; OnPropertyChanged(); RecalcUtmRect(); } }
        PlanPoint _ImageData = new PlanPoint { PointName = "Image" };
        public PlanPoint Align1 { get { return _Align1; } set { _Align1 = value; OnPropertyChanged(); RecalcUtmRect(); } }
        PlanPoint _Align1 = new PlanPoint { PointName = "Align1" };
        public PlanPoint Align2 { get { return _Align2; } set { _Align2 = value; OnPropertyChanged(); RecalcUtmRect(); } }
        PlanPoint _Align2 = new PlanPoint { PointName = "Align2" };
        public PlanPoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); RecalcUtmRect(); } }
        PlanPoint _Origin = new PlanPoint { AB = new Point(50, 50), XY = new Point(0, 0), PointName = "Origin" };   // default middle

        public WaypointCollection Waypoints { get { return _Waypoints; } set { _Waypoints = value; OnPropertyChanged(); } }
        WaypointCollection _Waypoints = new WaypointCollection(); 

        static Pen gridPen = new Pen(Brushes.Gray, 1.0);

        [JsonIgnore]
        public Brush BackgroundBrush;

        public double UtmLeft { get { return _UtmLeft; } set { _UtmLeft = value; OnPropertyChanged(); } }
        double _UtmLeft;
        public double UtmTop { get { return _UtmTop; } set { _UtmTop = value; OnPropertyChanged(); } }
        double _UtmTop;
        public double UtmWidth { get { return _UtmWidth; } set { _UtmWidth = value; OnPropertyChanged(); } }
        double _UtmWidth;
        public double UtmHeight { get { return _UtmHeight; } set { _UtmHeight = value; OnPropertyChanged(); } }
        double _UtmHeight; 

        string LastGoogleMapUri;        // hack
        public int GridDivisions = 10;      // todo 

        public void SaveImage(string filename)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(new Uri(LastGoogleMapUri)));  // hack

            using (var filestream = new FileStream(filename, FileMode.OpenOrCreate))
                encoder.Save(filestream);
            ImageFileName = filename;    // so it will load as image next run
        }

        public Utm Pct2Utm(Point a)
        {
            return new Utm { Easting = UtmLeft + (a.X * UtmWidth),
                Northing = UtmTop + (a.Y * UtmHeight), Zone = "10T" };  // hack hardcoded zone
        }

        internal Point Utm2Local(Utm utm)
        {
            return new Point(utm.Easting - Origin.Utm.Easting, utm.Northing - Origin.Utm.Northing);
        }

        public Point Pct2Local(Point a)
        {
            return Utm2Local(Pct2Utm(a));
        }

        public void RecalcUtmRect()
        {
            System.Diagnostics.Trace.WriteLine($"Plan::RecalcUtmRect");
            Utm middleUtm = Utm.FromWgs84(ImageData.Wgs84);

            double utmDistanceBetweenAlignsX = Math.Abs(Align2.Utm.Easting - Align1.Utm.Easting);
            double pctDistanceBetweenAlignsX = Math.Abs(Align2.AB.X - Align1.AB.X);
            UtmWidth = 1.0 / pctDistanceBetweenAlignsX * utmDistanceBetweenAlignsX;

            double utmDistanceBetweenAlignsY = Math.Abs(Align2.Utm.Northing - Align1.Utm.Northing);
            double pctDistanceBetweenAlignsY = Math.Abs(Align2.AB.Y - Align1.AB.Y);
            UtmHeight = 1.0 / pctDistanceBetweenAlignsY * utmDistanceBetweenAlignsY;

            UtmLeft = middleUtm.Easting - (UtmWidth / 2);
            UtmTop = middleUtm.Northing - (UtmHeight / 2);

            RecalcOrigin();
        }

        internal void RecalcOrigin()
        {
            System.Diagnostics.Trace.WriteLine($"Plan::RecalcOrigin");

            var u = new Utm
            {
                Easting = UtmLeft + (Origin.AB.X * UtmWidth),
                Northing = UtmTop + (Origin.AB.Y * UtmHeight),
                Zone = "10T"
            };   // hack hardcoded zone

            Origin.Wgs84 = u.AsWgs84();
        }

        public void RenderBackground(DrawingContext dc, PlanCanvas pc)
        {
            // draw image, fetch from google if it has not been already fetched
            if (BackgroundBrush == null)
            {
                ImageData.AB = new Point(pc.ActualWidth, pc.ActualHeight);

                if (ImageFileName?.Length > 0)
                {
                    BackgroundBrush = new ImageBrush(new BitmapImage(new Uri(ImageFileName)));
                    MainWindow.Status = "Loaded local Image for background";
                }
                else if (Math.Abs(ImageData.Wgs84.Longitude) + Math.Abs(ImageData.Wgs84.Latitude) > 0)
                {
                    int zoom = (int)ImageData.XY.X; // todo not the best UI 
                    ImageFileName = ""; // indicates we did not load an image from disk

                    // https://groups.google.com/forum/#!topic/google-maps-js-api-v3/hDRO4oHVSeM
                    LastGoogleMapUri = "https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&" +
                        $"center={ImageData.Wgs84.Latitude},{ImageData.Wgs84.Longitude}&" +
                        $"size={(int)ImageData.AB.X}x{(int)ImageData.AB.Y}&" +
                        $"zoom={zoom}&" +
                        $"key={API_KEY}";

                    BackgroundBrush = new ImageBrush(new BitmapImage(new Uri(LastGoogleMapUri)));
                    MainWindow.Status = "Fetched GoogleMap for background";
                }
                else
                {
                    BackgroundBrush = new SolidColorBrush(Colors.DarkGray);
                    MainWindow.Status = "Create empty background brush";
                }
            }

            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, pc.ActualWidth, pc.ActualHeight));

            //---R
            // todo should be fixed distance, eg draw 10 meter increments from origin
            for (double x = 0, y = 0; x < pc.ActualWidth; x += pc.ActualWidth / GridDivisions, y += pc.ActualHeight / GridDivisions)
            {
                dc.DrawLine(gridPen, new Point(x, 0), new Point(x, pc.ActualHeight));
                dc.DrawLine(gridPen, new Point(0, y), new Point(pc.ActualWidth, y));
            }
         }

        internal void ResetSequenceNumbers()
        {
            for (var i = 0; i < Waypoints.Count; i++)
                Waypoints[i].Sequence = i+1;                        
        }

        public void Save(Window owner)
        {
            if (string.IsNullOrEmpty(PlanFilename))
                SaveAs(owner);
            else
            {
                IsDirty = false;
                string planString = JsonConvert.SerializeObject(this);
                File.WriteAllText(PlanFilename, planString);
            }
        }

        public void SaveAs(Window owner)
        {
            SaveFileDialog d = new SaveFileDialog { Filter = "XML Files|*.xml|All Files|*.*", DefaultExt = "xml" };            
            if (d.ShowDialog(owner) ?? false)
            {
                PlanFilename = d.FileName;
                Save(owner);
            }
        }

        public static Plan Load(string filename)
        {
            Plan p = null;
            if (File.Exists(filename))
            {
                try
                {
                    // Open the file to read from.
                    string planString = File.ReadAllText(filename);
                    p = JsonConvert.DeserializeObject<Plan>(planString);
                    p.PlanFilename = filename;
                                        
                    p.IsDirty = false;
                }
                catch (Exception)
                {
                    return new Plan();
                }
            }
            return p;
        }

        internal string GetNavCode(float initialHeading)
        {
            // todo someday maybe some sort of templates
            StringBuilder b = new StringBuilder();
            b.AppendLine("Pilot = Pilot.Factory(\"192.168.42.1\");");
            b.AppendLine("Pilot.OnPilotReceive += Pilot_OnReceive;");
            b.AppendLine("Send(new { Cmd = \"CONFIG\", Geom = new float[] { 336.2F, 450F } });");
            b.AppendLine($"Send(new {{ Cmd = \"RESET\", Hdg = {initialHeading:F1} }});");
            b.AppendLine("Send(new { Cmd = \"ESC\", Value = 1 });");
            foreach (Waypoint w in Waypoints)
            {
                Point local = Pct2Local(w.XY);
                b.AppendLine($"Send(new {{ Cmd = \"GOTOXY\", X={local.X:F3}, Y={local.Y:F3}, Pwr = 40.0F }});");
                b.AppendLine("waitForEvent();");
            }
            b.AppendLine("Send(new { Cmd = \"ESC\", Value = 0 });");
            return b.ToString();
        }
    }
}
