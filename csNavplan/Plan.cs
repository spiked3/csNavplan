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
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace csNavplan
{

    // todo this;
       /*
       if you have align1 & 2 - you have the Utm rect of the entire image
       when you right click a point, other than align points, you can calculate the Utm by pct, and thus the gps as well
       if you just hit ok in the dialog, those values are used. 
       if you edit utm, if valid the gps is updated. it might not be a valid utm if we are using local points
       if you edit gps, utm is updated
       in both cases, screen pct points are recalculated, and draw at the correct position
       */

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

        //public bool IsValid { get { return _IsValid; } set { _IsValid = value; OnPropertyChanged(); } }
        //bool _IsValid = false; 

        public string PlanFilename { get { return _PlanFilename; } set { _PlanFilename = value; OnPropertyChanged(); } }
        string _PlanFilename;

        [ExpandableObject]
        public PlanImage PlanImage { get { return _PlanImage; } set { _PlanImage = value; OnPropertyChanged(); } }
        PlanImage _PlanImage;

        [ExpandableObject]
        public BasePoint Align1 { get { return _Align1; } set { _Align1 = value; OnPropertyChanged(); } }
        BasePoint _Align1;

        [ExpandableObject]
        public BasePoint Align2 { get { return _Align2; } set { _Align2 = value; OnPropertyChanged(); } }
        BasePoint _Align2;

        [ExpandableObject]
        public BasePoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); } }
        BasePoint _Origin; 

        public WayPointCollection Waypoints { get { return _Waypoints; } set { _Waypoints = value; OnPropertyChanged(); } }
        WayPointCollection _Waypoints = new WayPointCollection();

        // size of the original image in meters, based on 2 align points
        public Size ViewSize { get { return _ViewSize; } set { _ViewSize = value; OnPropertyChanged(); } }
        Size _ViewSize; 

        static Pen gridPen = new Pen(Brushes.Gray, 1.0);

        [JsonIgnore]
        public Brush BackgroundBrush;

        string LastGoogleMapUri;        // hack for saving image

        public bool SaveImage(string filename)
        {
            try
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(new Uri(LastGoogleMapUri)));  // hack for saving image
                using (var filestream = new FileStream(filename, FileMode.OpenOrCreate))
                    encoder.Save(filestream);
                PlanImage.FileName = filename;    // so it will load as image next run
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Break();
            }
            return false;
        }

        //public Utm Pct2Utm(Point a)
        //{
        //    return new Utm { Easting = UtmLeft + (a.X * UtmWidth),
        //        Northing = UtmTop + (a.Y * UtmHeight), Zone = "10T" };  // hack hardcoded zone
        //}

        //public Point Utm2Local(Utm utm)
        //{
        //    return new Point((utm?.Easting - Origin?.Utm.Easting ?? double.NaN), (utm?.Northing - Origin?.Utm.Northing ?? double.NaN));
        //}

        //public Point Pct2Local(Point a)
        //{
        //    return Utm2Local(Pct2Utm(a));
        //}

        // view rect is what dimensions in meters the view represents, now matter how it shown

        public void RecalcViewSize()
        {
            if (Align1 != null && Align2 != null && Align1.GetType() == Align2.GetType())
            {
                // todo in progress
                double pctDistanceBetweenAlignsX = Math.Abs(Align2.PctPoint.X - Align1.PctPoint.X);
                double pctDistanceBetweenAlignsY = Math.Abs(Align2.PctPoint.Y - Align1.PctPoint.Y);

                if (Align1 is WorldPoint)
                    ViewSize = new Size(
                        (((WorldPoint)Align2).Utm.Easting - ((WorldPoint)Align1).Utm.Easting) / pctDistanceBetweenAlignsX,
                        (((WorldPoint)Align2).Utm.Northing - ((WorldPoint)Align1).Utm.Northing) / pctDistanceBetweenAlignsY
                    );
                else
                    ViewSize = new Size(
                        (((LocalPoint)Align2).XY.X - ((LocalPoint)Align1).XY.X) / pctDistanceBetweenAlignsX,
                        (((LocalPoint)Align2).XY.Y - ((LocalPoint)Align1).XY.Y) / pctDistanceBetweenAlignsY
                    );
            }

            var t = $"Plan::RecalcViewSize = ({ViewSize.Width:F5}, {ViewSize.Height:F5})";
            MainWindow.Message(t);
            System.Diagnostics.Trace.WriteLine(t);
        }

        //internal void RecalcOrigin()
        //{
        //    if (IsValid)
        //    {
        //        Origin.Utm = new Utm
        //        {
        //            Easting = UtmLeft + (Origin.pctPoint.X * UtmWidth),
        //            Northing = UtmTop + (Origin.pctPoint.Y * UtmHeight),
        //            Zone = "10T"
        //        };   // hack hardcoded zone

        //        var t = $"Plan::RecalcOrigin = ({Origin.Utm})";
        //        MainWindow.Toast(t);
        //        System.Diagnostics.Trace.WriteLine(t);
        //    }
        //}

        public void RenderBackground(DrawingContext dc, PlanCanvas pc, double gridSpacing)
        {
            // draw image, fetch from google if it has not been already fetched
            if (BackgroundBrush == null && PlanImage != null)
            {
                if (PlanImage.FileName?.Length > 0)
                {
                    PlanImage.Load();
                    BackgroundBrush = (ImageBrush)PlanImage;
                    PlanImage.Width = PlanImage.Brush.ImageSource.Width;
                    PlanImage.Height = PlanImage.Brush.ImageSource.Height;
                    MainWindow.Message("Loaded local Image for background");
                }
                else if (PlanImage.originalWgs84 != null)
                {
                    //int zoom = (int)ImageData.Local.X; // todo not the best UI, gets value from an unused point
                    int zoom = 19;  // todo kludge

                    PlanImage.FileName = ""; // indicates we did not load an image from disk

                    // https://groups.google.com/forum/#!topic/google-maps-js-api-v3/hDRO4oHVSeM

                    LastGoogleMapUri = "https://maps.googleapis.com/maps/api/staticmap?maptype=satellite&" +
                        $"center={PlanImage.originalWgs84.Latitude},{PlanImage.originalWgs84.Longitude}&" +
                        $"size={(int)pc.ActualWidth}x{(int)pc.ActualHeight}&" +
                        $"zoom={zoom}&" +
                        $"key={API_KEY}";

                    BackgroundBrush = (ImageBrush)PlanImage;
                    PlanImage.Width = pc.ActualWidth;
                    PlanImage.Height = pc.ActualHeight;
                    MainWindow.Message("Fetched GoogleMap for background");
                }
            }
            else if (BackgroundBrush == null)
            {
                BackgroundBrush = new SolidColorBrush(Colors.DarkGray);
                if (PlanImage != null)
                {
                    PlanImage.Width = pc.ActualWidth;
                    PlanImage.Height = pc.ActualHeight;
                }
                MainWindow.Message("Created empty background brush");
            }

            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, pc.ActualWidth, pc.ActualHeight));

            if (Origin != null)
            {
                var startX = Origin.PctPoint.X * pc.ActualWidth;
                var startY = Origin.PctPoint.Y * pc.ActualHeight;

                // todo
                //var oneMeterX = pc.ActualWidth / UtmWidth;
                //var oneMeterY = pc.ActualHeight / UtmHeight;

                //var reps = Math.Max(UtmWidth / gridSpacing, UtmHeight / gridSpacing);

                //for (int i = 0; i < (int)(reps + 1); i++)
                //{
                //    dc.DrawLine(gridPen, new Point(startX + (i * oneMeterX * gridSpacing), 0), new Point(startX + (i * oneMeterX * gridSpacing), pc.ActualHeight));
                //    dc.DrawLine(gridPen, new Point(startX - (i * oneMeterX * gridSpacing), 0), new Point(startX - (i * oneMeterX * gridSpacing), pc.ActualHeight));

                //    dc.DrawLine(gridPen, new Point(0, startY + (i * oneMeterY * gridSpacing)), new Point(pc.ActualWidth, startY + (i * oneMeterY * gridSpacing)));
                //    dc.DrawLine(gridPen, new Point(0, startY - (i * oneMeterY * gridSpacing)), new Point(pc.ActualWidth, startY - (i * oneMeterY * gridSpacing)));
                //}
            }
        }

        //internal void ResetSequenceNumbers()
        //{
        //    for (var i = 0; i < Waypoints.Count; i++)
        //        Waypoints[i].Idx = i+1;                        
        //}

        static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        public void Save(Window owner)
        {
            if (string.IsNullOrEmpty(PlanFilename))
                SaveAs(owner);
            else
            {
                IsDirty = false;
                string planString = JsonConvert.SerializeObject(this, settings);
                File.WriteAllText(PlanFilename, planString);
            }
        }

        public void SaveAs(Window owner)
        {
            SaveFileDialog d = new SaveFileDialog { Filter = "JSON Files|*.json|All Files|*.*", DefaultExt = "json" };            
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
                    p = JsonConvert.DeserializeObject<Plan>(planString, settings);
                    p.PlanFilename = filename;                                        
                    p.IsDirty = false;
                }
                catch (Exception)
                {
                    System.Diagnostics.Debugger.Break();
                    return new Plan();
                }
            }
            return p;
        }

        public string WayPointsAsJson(float initialHeading)
        {
            // todo someday maybe some sort of templates
            StringBuilder b = new StringBuilder($"{{\"ResetHdg\":{initialHeading},\"WayPoints\":[");
            //bool firstTime = true;
            //foreach (Waypoint w in Waypoints)
            //{
            //    if (!firstTime)
            //        b.Append(",");
            //    else
            //        firstTime = false;
            //    //Point local = Pct2Local(w.XY);
            //    Point local = w.LocalXY;
            //    b.AppendLine($"[{local.X}, {local.Y}, {(w.isAction?1:0)}]");    // Turn/Move version
            //}
            return b.AppendLine($"]}}").ToString();
        }

        internal string GetNavCode(float initialHeading)
        {
            //double X=0, Y=0;
            // todo someday maybe some sort of templates
            StringBuilder b = new StringBuilder();
            //b.AppendLine("Pilot = Pilot.Factory(\"192.168.42.1\");");
            //b.AppendLine("Pilot.OnPilotReceive += Pilot_OnReceive;");
            //// old b.AppendLine("Pilot.Send(new { Cmd = \"CONFIG\", Geom = new float[] { 336.2F, 450F } });");
            //b.AppendLine("Pilot.Send(new { Cmd = \"CONFIG\", Geom = new float[] { 336.2F, 450F }, M1 = new int[] { 1, -1 }, M2 = new int[] { -1, 1 } });");

            //b.AppendLine($"Pilot.Send(new {{ Cmd = \"RESET\", Hdg = {initialHeading:F1} }});");
            //b.AppendLine("Pilot.Send(new { Cmd = \"ESC\", Value = 1 });");
            //foreach (Waypoint w in Waypoints)
            //{
            //    Point local = w.LocalXY;

            //    //b.AppendLine($"//Send(new {{ Cmd = \"GOTO\", X={local.X:F3}, Y={local.Y:F3}, Pwr = 40.0F }});");    // gotoxy version

            //    var _x = local.X - X;
            //    var _y = local.Y - Y;

            //    var hdgToNxt = (Math.Atan2(_y, _x) * 180.0 / Math.PI) + 90.0;
            //    var distToNext = Math.Sqrt((_x * _x) + (_y * _y));

            //    b.AppendLine($"Pilot.Send(new {{ Cmd = \"ROT\", Hdg={hdgToNxt:F1}, Pwr = 40.0F }});");    // Turn/Move version
            //    b.AppendLine("Pilot.waitForEvent();");
            //    b.AppendLine($"Pilot.Send(new {{ Cmd = \"MOV\", Dist={distToNext:F1}, Pwr = 40.0F }});");
            //    b.AppendLine("Pilot.waitForEvent();");

            //    X = local.X;
            //    Y = local.Y;
            //}
            //b.AppendLine("Pilot.Send(new { Cmd = \"ESC\", Value = 0 });");
            return b.ToString();
        }
    }

    public class PlanImage
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        [JsonIgnore]
        public ImageBrush Brush { get { return _Brush; } set { _Brush = value; OnPropertyChanged(); } }
        ImageBrush _Brush;

        public static explicit operator ImageBrush(PlanImage a) { return a.Brush; }

        public string FileName { get { return _FileName; } set { _FileName = value; OnPropertyChanged(); } }
        string _FileName;

        public double Width { get { return _ImageWidth; } set { _ImageWidth = value; OnPropertyChanged(); } }
        double _ImageWidth = 0;
        public double Height { get { return _Height; } set { _Height = value; OnPropertyChanged(); } }
        double _Height = 0;

        public Wgs84 originalWgs84 { get { return _originalWgs84; } set { _originalWgs84 = value; OnPropertyChanged(); } }

        Wgs84 _originalWgs84 = null;

        public void Load()
        {
            Brush = new ImageBrush(new BitmapImage(new Uri(FileName)));
        }
    }
}
