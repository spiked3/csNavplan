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
    // todo save brushes

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

        public CoordinateType CoordinateType { get { return _CoordinateType; } set { _CoordinateType = value; OnPropertyChanged(); } }
        CoordinateType _CoordinateType = CoordinateType.Local; 

        public bool IsDirty { get { return _IsDirty; } set { _IsDirty = value; OnPropertyChanged(); } }
        bool _IsDirty = false;

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

        public bool isAligned { get { return Align1 != null && Align2 != null; } }

        [ExpandableObject]
        public BasePoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); } }
        BasePoint _Origin;

        public bool isOriginValid { get { return isAligned && Origin != null; } }

        public WayPointCollection Waypoints { get { return _Waypoints; } set { _Waypoints = value; OnPropertyChanged(); } }
        WayPointCollection _Waypoints = new WayPointCollection();

        // size of the original image in meters, based on 2 align points
        public Point ViewSize { get { return _ViewSize; } set { _ViewSize = value; OnPropertyChanged(); } }
        Point _ViewSize;

        // XY location in meters of lower left based on Align1, of original image
        public Point ViewOrigin { get { return _ViewOrigin; } set { _ViewOrigin = value; OnPropertyChanged(); } }
        Point _ViewOrigin; 

        // todo implement ruler
        public BasePoint RulerStart { get { return _RulerStart; } set { _RulerStart = value; OnPropertyChanged(); } }
        BasePoint _RulerStart;
        public BasePoint RulerEnd { get { return _RulerEnd; } set { _RulerEnd = value; OnPropertyChanged(); } }
        BasePoint _RulerEnd;

        [JsonIgnore]
        static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };
        [JsonIgnore]
        public Brush BackgroundBrush;

        public string ForegroundColor { get { return _ForegroundColor; } set { _ForegroundColor = value; OnPropertyChanged(); } }
        string _ForegroundColor = Colors.Black.ToString(); 

        [JsonIgnore]
        public Pen GridPen { get { return _GridPen; } set { _GridPen = value; OnPropertyChanged(); } }
        Pen _GridPen = new Pen(Brushes.Gray, 1.0);

        // todo finish up google
        public string GoogleUri { get { return _GoogleUri; } set { _GoogleUri = value; OnPropertyChanged(); } }
        string _GoogleUri = ""; 

        // ViewSize is what dimensions in meters the view represents, now matter how it is shown
        // ViewOrigin is calculated from align1
        public void RecalcView()
        {
            if (isAligned)
            {
                System.Diagnostics.Debug.Assert(Align1.GetType() == Align2.GetType());

                double pctDistanceBetweenAlignsX = Math.Abs(Align2.PctPoint.X - Align1.PctPoint.X);
                double pctDistanceBetweenAlignsY = Math.Abs(Align2.PctPoint.Y - Align1.PctPoint.Y);

                if (Align1 is WorldPoint)
                    ViewSize = new Point(
                        Math.Abs(((WorldPoint)Align2).Utm.Easting - ((WorldPoint)Align1).Utm.Easting) / pctDistanceBetweenAlignsX,
                        Math.Abs(((WorldPoint)Align2).Utm.Northing - ((WorldPoint)Align1).Utm.Northing) / pctDistanceBetweenAlignsY
                    );
                else
                    ViewSize = new Point(
                        Math.Abs(((LocalPoint)Align2).XY.X - ((LocalPoint)Align1).XY.X) / pctDistanceBetweenAlignsX,
                        Math.Abs(((LocalPoint)Align2).XY.Y - ((LocalPoint)Align1).XY.Y) / pctDistanceBetweenAlignsY
                    );

                if (Align1 is WorldPoint)
                {
                    var a1 = (WorldPoint)Align1; 
                    ViewOrigin = 
                        new Point(a1.Utm.Easting - (a1.PctPoint.X * ViewSize.X ), a1.Utm.Northing - (a1.PctPoint.Y * ViewSize.Y));
                }
                else
                    ViewOrigin = new Point(((LocalPoint)Align1).XY.X, ((LocalPoint)Align1).XY.Y);

                var t = $"Plan::RecalcView Origin({ViewOrigin.X:F5}, {ViewOrigin.Y:F5}), Size({ViewSize.X:F5}, {ViewSize.Y:F5})";
                MainWindow.Message(t);
                System.Diagnostics.Trace.WriteLine(t);
            }
        }

        //percents are view relative, XY are coordinate relative (up y is positive)
        // locals work, dont touch
        public BasePoint NavPointAtPctPoint(Point pp)
        {
            if (!isAligned)
            {
                System.Diagnostics.Debugger.Break();
                return null;
            }

            // size in meters
            var xPos = pp.X * ViewSize.X;
            var yPos = ViewSize.Y - (pp.Y * ViewSize.Y);

            if (CoordinateType == CoordinateType.Local)
                return new LocalPoint(pp, new Point(xPos, yPos));
            else
                return new WorldPoint(pp, new Utm(ViewOrigin.X + xPos, ViewOrigin.Y + yPos, ((WorldPoint)Align1).Utm.Zone));
        }

        public Point PctPointAtNavPoint(BasePoint point)
        {
            if (!isAligned)
            {
                System.Diagnostics.Debugger.Break();
                throw new NotImplementedException();
            }
            if (CoordinateType == CoordinateType.Local)
                return new Point(((LocalPoint)point).XY.X / ViewSize.X,
                    1 - ((LocalPoint)point).XY.Y / ViewSize.Y);
            else
            {
                //+here, 
                var x = ((WorldPoint)point).Utm.Easting - ViewOrigin.X;
                var y = ((WorldPoint)point).Utm.Northing + ViewOrigin.Y;
                return new Point(x / ViewSize.X, 1 -(y / ViewSize.Y));
            }
        }

        public void RenderBackground(DrawingContext dc, PlanCanvas pc, double gridSpacing)
        {
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

                var oneMeterX = pc.ActualWidth / ViewSize.X;
                var oneMeterY = pc.ActualHeight / ViewSize.Y;

                var reps = Math.Max(ViewSize.X / gridSpacing, ViewSize.Y / gridSpacing);

                for (int i = 0; i < (int)(reps + 1); i++)
                {
                    dc.DrawLine(GridPen, new Point(startX + (i * oneMeterX * gridSpacing), 0), new Point(startX + (i * oneMeterX * gridSpacing), pc.ActualHeight));
                    dc.DrawLine(GridPen, new Point(startX - (i * oneMeterX * gridSpacing), 0), new Point(startX - (i * oneMeterX * gridSpacing), pc.ActualHeight));

                    dc.DrawLine(GridPen, new Point(0, startY + (i * oneMeterY * gridSpacing)), new Point(pc.ActualWidth, startY + (i * oneMeterY * gridSpacing)));
                    dc.DrawLine(GridPen, new Point(0, startY - (i * oneMeterY * gridSpacing)), new Point(pc.ActualWidth, startY - (i * oneMeterY * gridSpacing)));
                }
            }
        }

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
                    p.RecalcView();
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
            bool firstTime = true;
            foreach (BasePoint w in Waypoints)
            {
                if (!firstTime)
                    b.Append(",");
                else
                    firstTime = false;
                //Point local = Pct2Local(w.XY);
                Point local = w.GetLocalXY(Origin);
                b.AppendLine($"[{local.X}, {local.Y}, {(w.isAction ? 1 : 0)}]");
            }
            return b.AppendLine($"]}}").ToString();
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

        public void Load()
        {
            Brush = new ImageBrush(new BitmapImage(new Uri(FileName)));
        }
    }
    public enum CoordinateType { Local, World }
}
