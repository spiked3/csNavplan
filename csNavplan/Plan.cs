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

        public string ImageFileName { get { return _ImageFileName; } set { _ImageFileName = value; OnPropertyChanged(); RecalcAlignment(); } }
        string _ImageFileName;

        // imagedata stores the Gps of image center used to fetch from google
        // AB is the target size 
        public PlanPoint ImageData { get { return _ImageData; } set { _ImageData = value; OnPropertyChanged(); RecalcAlignment(); } }
        PlanPoint _ImageData = new PlanPoint { PointName = "Image" };
        public PlanPoint Align1 { get { return _Align1; } set { _Align1 = value; OnPropertyChanged(); RecalcAlignment(); } }
        PlanPoint _Align1 = new PlanPoint { PointName = "Align1" };
        public PlanPoint Align2 { get { return _Align2; } set { _Align2 = value; OnPropertyChanged(); RecalcAlignment(); } }
        PlanPoint _Align2 = new PlanPoint { PointName = "Align2" };
        public PlanPoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); RecalcAlignment(); } }
        PlanPoint _Origin = new PlanPoint { AB = new Point(50, 50), XY = new Point(0, 0), PointName = "Origin" };   // default middle

        public WaypointCollection Waypoints { get { return _Waypoints; } set { _Waypoints = value; OnPropertyChanged(); } }
        WaypointCollection _Waypoints = new WaypointCollection(); 

        static Pen gridPen = new Pen(Brushes.Gray, 1.0);

        [JsonIgnore]
        public Brush BackgroundBrush;

        [JsonIgnore]
        public Rect ImageUtmRect { get { return _ImageUtmRect; } set { _ImageUtmRect = value; OnPropertyChanged(); } }
        Rect _ImageUtmRect; 
        
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
            return new Utm { Easting = ImageUtmRect.X + (a.X * ImageUtmRect.Width),
                Northing = ImageUtmRect.Y + (a.Y * ImageUtmRect.Height), Zone = "10T" };  // hack hardcoded zone
        }

        internal Point Utm2Local(Utm utm)
        {
            return new Point(utm.Easting - Origin.UtmCoord.Easting, utm.Northing - Origin.UtmCoord.Northing);
        }

        public Point Pct2Local(Point a)
        {
            return Utm2Local(Pct2Utm(a));
        }

        public void RecalcAlignment()
        {
            // todo I think this is good
            Utm middleUtm = Utm.FromLonLat(ImageData.GpsCoord.X, ImageData.GpsCoord.X);

            double utmDistanceBetweenAlignsX = Math.Abs(Align2.UtmCoord.Easting - Align1.UtmCoord.Easting);
            double pctDistanceBetweenAlignsX = Math.Abs(Align2.AB.X - Align1.AB.X);
            double utmImageWidthX = 1.0 / pctDistanceBetweenAlignsX * utmDistanceBetweenAlignsX;

            double utmDistanceBetweenAlignsY = Math.Abs(Align2.UtmCoord.Northing - Align1.UtmCoord.Northing);
            double pctDistanceBetweenAlignsY = Math.Abs(Align2.AB.Y - Align1.AB.Y);
            double utmImageWidthY = 1.0 / pctDistanceBetweenAlignsY * utmDistanceBetweenAlignsY;

            double left = Origin.UtmCoord.Easting - (Origin.AB.X * utmImageWidthX);
            double top = Origin.UtmCoord.Northing - (Origin.AB.Y * utmImageWidthY);

            ImageUtmRect = new Rect(left, top, utmImageWidthX, utmImageWidthY);
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
                else if (ImageData.GpsCoord.X + ImageData.GpsCoord.X > 0)
                {
                    int zoom = (int)ImageData.XY.X; // todo not the best UI 
                    ImageFileName = ""; // indicates we did not load an image from disk

                    // https://groups.google.com/forum/#!topic/google-maps-js-api-v3/hDRO4oHVSeM
                    LastGoogleMapUri = "https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&" +
                        $"center={ImageData.GpsCoord.X},{ImageData.GpsCoord.Y}&" +
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

        public void Save()
        {
            if (string.IsNullOrEmpty(PlanFilename))
                SaveAs();
            else
            {
                IsDirty = false;
                string planString = JsonConvert.SerializeObject(this);
                File.WriteAllText(PlanFilename, planString);
            }
        }
        public void SaveAs()
        {
            SaveFileDialog d = new SaveFileDialog { Filter = "XML Files|*.xml|All Files|*.*", DefaultExt = "xml" };
            if (d.ShowDialog() ?? false)
            {
                PlanFilename = d.FileName;
                Save();
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

        internal string GetNavXml(float initialHeading)
        {
            // todo someday maybe some sort of templates
            StringBuilder b = new StringBuilder();
            b.AppendLine("Pilot = Pilot.Factory(\"192.168.42.1\");");
            b.AppendLine("Pilot.OnPilotReceive += Pilot_OnReceive;");
            b.AppendLine($"Send(new {{ Cmd = \"RESET\", Hdg = {initialHeading:F1} }});");
            b.AppendLine("Send(new { Cmd = \"CONFIG\", Geom = new float[] { 336.2F, 450F } });");
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
