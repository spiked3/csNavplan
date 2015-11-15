using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace csNavplan
{
    public abstract class BasePoint
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

        public bool isAction { get { return _isAction; } set { _isAction = value; OnPropertyChanged(); OnPropertyChanged("isActionString"); } }
        bool _isAction = false;

        public string isActionString { get { return isAction ? "Action" : "Normal"; } }

        public abstract Point GetLocalXY(BasePoint origin);
    }

    public class LocalPoint : BasePoint
    {
        public Point XY { get { return _XY; } set { _XY = value; OnPropertyChanged(); } }
        Point _XY = new Point(double.NaN, double.NaN);

        public LocalPoint(Point pp, Point xy, bool a = false)
        {
            PctPoint = pp;
            XY = xy;
            isAction = a;
        }

        public override Point GetLocalXY(BasePoint origin)
        {
            return XY;  // asumes origin xy is 0,0
        }
    }

    public class WorldPoint : BasePoint
    {
        // only Utm for world point is stored, wgs is calculated on the fly as needed
        public Utm Utm { get { return _Utm; } set { _Utm = value; OnPropertyChanged(); } }
        Utm _Utm;

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

        public override Point GetLocalXY(BasePoint origin)
        {
            if (!(origin is WorldPoint))
                throw new InvalidOperationException();
            var o = origin as WorldPoint;
            return new Point(Utm.Easting - o.Utm.Easting, Utm.Northing - o.Utm.Northing);
        }

        public Wgs84 Wgs84 { get { return (Wgs84)Utm; } set { Utm = Utm.FromWgs84(value); } }
    }
}

