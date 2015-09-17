using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        public string ImageFileName { get { return _ImageFileName; } set { _ImageFileName = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        string _ImageFileName;

        // imagedata stores the Gps of image center used to fetch from google
        // AB is the target size 
        public PlanPoint ImageData { get { return _ImageData; } set { _ImageData = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _ImageData = new PlanPoint { PointName = "Image" };
        public PlanPoint Align1 { get { return _Align1; } set { _Align1 = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _Align1 = new PlanPoint { PointName = "Align1" };
        public PlanPoint Align2 { get { return _Align2; } set { _Align2 = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _Align2 = new PlanPoint { PointName = "Align2" };
        public PlanPoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _Origin = new PlanPoint { AB = new Point(50, 50), XY = new Point(0, 0), PointName = "Origin" };   // default middle

        public WaypointCollection Waypoints { get { return _Waypoints; } set { _Waypoints = value; OnPropertyChanged(); } }
        WaypointCollection _Waypoints = new WaypointCollection(); 

        public event EventHandler AlignmentChanged;

        static Pen gridPen = new Pen(Brushes.Gray, 1.0);

        [JsonIgnore]
        public Brush BackgroundBrush;

        [JsonIgnore]
        public Rect ImageUtmRect;

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

        public Point Pct2Utm(Point a)
        {
            return new Point(ImageUtmRect.X + a.X  * ImageUtmRect.Width, ImageUtmRect.Y + a.Y * ImageUtmRect.Height);
        }

        internal Point Utm2Local(Point utm)
        {
            return new Point(utm.X - Origin.UtmCoord.X, utm.Y - Origin.UtmCoord.Y);
        }

        public Point Pct2Local(Point a)
        {
            return Utm2Local(Pct2Utm(a));
        }

        public void OnAlignmentPointChanged()
        {
            Utm middleUtm = new Utm(ImageData.GpsCoord.X, ImageData.GpsCoord.X);

            // calculate new proportions - pctg diff / utm diff
            double horizontalProportion = Math.Abs(Align2.AB.X - Align1.AB.X) / Math.Abs(Align2.UtmCoord.X - Align1.UtmCoord.X);
            double verticalProportion = (Align1.AB.Y - Align2.AB.Y) / (Align1.UtmCoord.Y - Align2.UtmCoord.Y);

            double imgWidthMeters = ImageData.AB.X * horizontalProportion;
            double imgHeightMeters = ImageData.AB.Y * verticalProportion;

            var left = middleUtm.Easting - imgWidthMeters / 2;
            var top = middleUtm.Northing - imgHeightMeters / 2;

            ImageUtmRect = new Rect(left, top, left + imgWidthMeters, top + imgHeightMeters);

            if (AlignmentChanged != null)
                AlignmentChanged(this, EventArgs.Empty);
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
                    int zoom = 19; // todo I hate maps
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
                    BackgroundBrush = new SolidColorBrush(Colors.WhiteSmoke);
                    MainWindow.Status = "Create empty background brush";
                }
            }

            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, pc.ActualWidth, pc.ActualHeight));

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

        #region LoadSave_Notimplemented
        public void Save()
        {
            throw new NotImplementedException();
        }
        public void SaveAs(string fileName)
        {
            throw new NotImplementedException();
        }
        public static Plan Load(string fileName)
        {
            throw new NotImplementedException();
        }
#endregion
    }
}
