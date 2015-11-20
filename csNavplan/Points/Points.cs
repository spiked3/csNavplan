using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace csNavplan
{
    // ViewPoint is relative to view
    public class ViewPoint
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        public Point PctPoint { get { return _PctPoint; } set { _PctPoint = value; OnPropertyChanged(); } }
        Point _PctPoint = new Point(double.NaN, double.NaN);

        public Point XY { get { return _XY; } set { _XY = value; OnPropertyChanged(); } }
        Point _XY; 
    }

    // BaseNavPoint is relative to navigation (local or world)
    public abstract class BaseNavPoint
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        public Plan Plan { get { return _Plan; } set { _Plan = value; OnPropertyChanged(); } }
        Plan _Plan; 

        public Point PctPoint { get { return _PctPoint; } set { _PctPoint = value; OnPropertyChanged(); } }
        Point _PctPoint = new Point(double.NaN, double.NaN);

        public abstract Point XY { get; set; } 

        public bool isAction { get { return _isAction; } set { _isAction = value; OnPropertyChanged(); OnPropertyChanged("isActionString"); } }
        bool _isAction = false;

        public string isActionString { get { return isAction ? "Action" : "Normal"; } }
    }

    public class LocalPoint : BaseNavPoint
    {
        [JsonConstructor]
        public LocalPoint(Point pp, Point xy, bool a = false)
        {
            PctPoint = pp;
            XY = xy;
            isAction = a;
        }

        public override Point XY
        {
            get
            {
                System.Diagnostics.Debug.Assert(Plan != null);
                LocalPoint o = (LocalPoint)origin;
                return new Point(XY.X - o.XY.X, XY.Y - o.XY.Y);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        [ExpandableObject]
        public class WorldPoint : BaseNavPoint
        {
            // only Utm for world point is stored, wgs is calculated on the fly as needed
            public Utm Utm { get { return _Utm; } set { _Utm = value; OnPropertyChanged(); } }
            Utm _Utm;

            [JsonConstructor]
            public WorldPoint(Point pct, Utm utm, bool isaction = false)
            {
                PctPoint = pct;
                Utm = utm;
                isAction = isaction;
            }

            public WorldPoint(Point pp, Wgs84 ll, bool a = false)
            {
                PctPoint = pp;
                Utm = ll.GetUtm();
                isAction = a;
            }

            public override Point XY
            {
                get
                {
                    if (origin == null)
                        return new Point(0, 0);
                    WorldPoint o = (WorldPoint)origin;
                    return new Point(Utm.Easting - o.Utm.Easting, Utm.Northing - o.Utm.Northing);
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public Wgs84 Wgs84 { get { return (Wgs84)Utm; } set { Utm = Utm.FromWgs84(value); } }
        }
    }
}

