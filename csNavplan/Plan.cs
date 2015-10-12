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

        public string ImageFileName { get { return _ImageFileName; } set { _ImageFileName = value; OnPropertyChanged();  } }
        string _ImageFileName;

        // imagedata stores the Wgs84 of image center used to fetch from google        
        // AB is the target size 
        public PlanPoint ImageData { get { return _ImageData; } set { _ImageData = value; OnPropertyChanged(); } }
        PlanPoint _ImageData = new PlanPoint { PointName = "Image" };
        public PlanPoint Align1 { get { return _Align1; } set { _Align1 = value; OnPropertyChanged();  } }
        PlanPoint _Align1 = new PlanPoint { PointName = "Align1" };
        public PlanPoint Align2 { get { return _Align2; } set { _Align2 = value; OnPropertyChanged(); } }
        PlanPoint _Align2 = new PlanPoint { PointName = "Align2" };
        public PlanPoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); } }
        PlanPoint _Origin = new PlanPoint { Pct = new Point(50, 50), Local = new Point(0, 0), PointName = "Origin" };   // default middle

        public WaypointCollection Waypoints { get { return _Waypoints; } set { _Waypoints = value; OnPropertyChanged(); } }
        WaypointCollection _Waypoints = new WaypointCollection();

        public double UtmLeft { get { return _UtmLeft; } set { _UtmLeft = value; OnPropertyChanged(); } }
        double _UtmLeft;
        public double UtmTop { get { return _UtmTop; } set { _UtmTop = value; OnPropertyChanged(); } }
        double _UtmTop;
        public double UtmWidth { get { return _UtmWidth; } set { _UtmWidth = value; OnPropertyChanged(); } }
        double _UtmWidth;
        public double UtmHeight { get { return _UtmHeight; } set { _UtmHeight = value; OnPropertyChanged(); } }
        double _UtmHeight;

        static Pen gridPen = new Pen(Brushes.Gray, 1.0);

        [JsonIgnore]
        public Brush BackgroundBrush;

        string LastGoogleMapUri;        // hack

        public bool SaveImage(string filename)
        {
            try
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(new Uri(LastGoogleMapUri)));  // hack

                using (var filestream = new FileStream(filename, FileMode.OpenOrCreate))
                    encoder.Save(filestream);
                ImageFileName = filename;    // so it will load as image next run
                return true;
            }
            catch (Exception)
            {
            }
            return false;
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
            double utmDistanceBetweenAlignsX = Math.Abs(Align2.Utm.Easting - Align1.Utm.Easting);
            double pctDistanceBetweenAlignsX = Math.Abs(Align2.Pct.X - Align1.Pct.X);
            UtmWidth = 1.0 / pctDistanceBetweenAlignsX * utmDistanceBetweenAlignsX;

            double utmDistanceBetweenAlignsY = Math.Abs(Align2.Utm.Northing - Align1.Utm.Northing);
            double pctDistanceBetweenAlignsY = Math.Abs(Align2.Pct.Y - Align1.Pct.Y);
            UtmHeight = 1.0 / pctDistanceBetweenAlignsY * utmDistanceBetweenAlignsY;

            if (ImageHasWgs84)
            {
                Utm middleUtm = Utm.FromWgs84(ImageData.Wgs84);
                UtmLeft = middleUtm.Easting - (UtmWidth / 2);
                UtmTop = middleUtm.Northing - (UtmHeight / 2);
            }
            else
            {
                //---R
                UtmLeft = -ImageData.Pct.X / 2;
                UtmTop = -ImageData.Pct.Y / 2;
            }

            var t = $"Plan::RecalcUtmRect = ({UtmLeft:F5}, {UtmTop:F5}, {UtmWidth:F3}, {UtmHeight:F3})";
            MainWindow.Toast(t);
            System.Diagnostics.Trace.WriteLine(t);
        }

        internal void RecalcOrigin()
        {
            // todo it would also be nice if origin entered via Wgs84 would trigger placement on map, but that would be true for any point
            Origin.Utm = new Utm
            {
                Easting = UtmLeft + (Origin.Pct.X * UtmWidth),
                Northing = UtmTop + (Origin.Pct.Y * UtmHeight),
                Zone = "10T"
            };   // hack hardcoded zone

            var t = $"Plan::RecalcOrigin = ({Origin.Utm})";
            MainWindow.Toast(t);
            System.Diagnostics.Trace.WriteLine(t);
        }

        public void RenderBackground(DrawingContext dc, PlanCanvas pc, double gridSpacing)
        {
            // draw image, fetch from google if it has not been already fetched
            if (BackgroundBrush == null)
            {
                ImageData.Pct = new Point(pc.ActualWidth, pc.ActualHeight);

                if (ImageFileName?.Length > 0)
                {
                    BackgroundBrush = new ImageBrush(new BitmapImage(new Uri(ImageFileName)));
                    MainWindow.Toast("Loaded local Image for background");
                }
                else if (ImageHasWgs84)
                {
                    int zoom = (int)ImageData.Local.X; // todo not the best UI, gets value from an unused point
                    if (zoom == 0) zoom = 19;

                    ImageFileName = ""; // indicates we did not load an image from disk

                    // https://groups.google.com/forum/#!topic/google-maps-js-api-v3/hDRO4oHVSeM
                    LastGoogleMapUri = "https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&" +
                        $"center={ImageData.Wgs84.Latitude},{ImageData.Wgs84.Longitude}&" +
                        $"size={(int)ImageData.Pct.X}x{(int)ImageData.Pct.Y}&" +
                        $"zoom={zoom}&" +
                        $"key={API_KEY}";

                    BackgroundBrush = new ImageBrush(new BitmapImage(new Uri(LastGoogleMapUri)));
                    MainWindow.Toast("Fetched GoogleMap for background");
                }
                else
                {
                    BackgroundBrush = new SolidColorBrush(Colors.DarkGray);
                    MainWindow.Toast("Create empty background brush");
                }
            }

            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, pc.ActualWidth, pc.ActualHeight));

            //---Y
            var startX = Origin.Pct.X * pc.ActualWidth;
            var startY = Origin.Pct.Y * pc.ActualHeight;

            var oneMeterX = pc.ActualWidth / UtmWidth;
            var oneMeterY = pc.ActualHeight / UtmHeight;

            var reps = Math.Max(UtmWidth / gridSpacing, UtmHeight / gridSpacing);

            for (int i=0; i < (int)(reps + 1); i++)
            {
                dc.DrawLine(gridPen, new Point(startX + (i * oneMeterX * gridSpacing), 0), new Point(startX + (i * oneMeterX * gridSpacing), pc.ActualHeight));
                dc.DrawLine(gridPen, new Point(startX - (i * oneMeterX * gridSpacing), 0), new Point(startX - (i * oneMeterX * gridSpacing), pc.ActualHeight));

                dc.DrawLine(gridPen, new Point(0, startY + (i * oneMeterY * gridSpacing)), new Point(pc.ActualWidth, startY + (i * oneMeterY * gridSpacing)));
                dc.DrawLine(gridPen, new Point(0, startY - (i * oneMeterY * gridSpacing)), new Point(pc.ActualWidth, startY - (i * oneMeterY * gridSpacing)));
            }
        }

        public bool ImageHasWgs84 { get { return Math.Abs(ImageData.Wgs84.Longitude) + Math.Abs(ImageData.Wgs84.Latitude) > 0; } }

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
            double X=0, Y=0;
            // todo someday maybe some sort of templates
            StringBuilder b = new StringBuilder();
            b.AppendLine("Pilot = Pilot.Factory(\"192.168.42.1\");");
            b.AppendLine("Pilot.OnPilotReceive += Pilot_OnReceive;");
            b.AppendLine("Pilot.Send(new { Cmd = \"CONFIG\", Geom = new float[] { 336.2F, 450F } });");
            b.AppendLine($"Pilot.Send(new {{ Cmd = \"RESET\", Hdg = {initialHeading:F1} }});");
            b.AppendLine("Pilot.Send(new { Cmd = \"ESC\", Value = 1 });");
            foreach (Waypoint w in Waypoints)
            {
                Point local = Pct2Local(w.XY);

                //b.AppendLine($"//Send(new {{ Cmd = \"GOTO\", X={local.X:F3}, Y={local.Y:F3}, Pwr = 40.0F }});");    // gotoxy version

                var _x = local.X - X;
                var _y = local.Y - Y;

                var hdgToNxt = (Math.Atan2(_y, _x) * 180.0 / Math.PI) + 90.0;
                var distToNext = Math.Sqrt((_x * _x) + (_y * _y));

                b.AppendLine($"Pilot.Send(new {{ Cmd = \"ROT\", Hdg={hdgToNxt:F1}, Pwr = 40.0F }});");    // Turn/Move version
                b.AppendLine("Pilot.waitForEvent();");
                b.AppendLine($"Pilot.Send(new {{ Cmd = \"MOV\", Dist={distToNext:F1}, Pwr = 40.0F }});");
                b.AppendLine("Pilot.waitForEvent();");

                X = local.X;
                Y = local.Y;
            }
            b.AppendLine("Pilot.Send(new { Cmd = \"ESC\", Value = 0 });");
            return b.ToString();
        }
    }
}
