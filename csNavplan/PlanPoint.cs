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
    public class PlanPoint : INotifyPropertyChanged
    {
#region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        public DispatcherTimer dt;

        public static readonly Point PointZero = new Point(0, 0);

        // XY is local coordinates in meters
        public Point Local { get { return _Local; } set { _Local.X = value.X; _Local.Y = value.Y; OnPropertyChanged(); } }
        Point _Local = new Point(0,0);

        // AB is pct point on image (arbitrary)
        public Point Pct { get { return _Pct; } set { _Pct.X = value.X; _Pct.Y = value.Y; OnPropertyChanged(); } }
        Point _Pct = new Point(0,0);

        // GPS is longitude and lattitude - if null it has NOT been entered, use UTM as local coordinates
        public Wgs84 Wgs84 { get { return _Wgs84; } set { _Wgs84 = value; OnPropertyChanged(); } }
        Wgs84 _Wgs84 = new Wgs84();

        // UTM is computed or designated coordinates in meters
        // if GPS is entered, UTM is computed, if manually entered it is left alone as basis for local coordinates
        public Utm Utm { get { return _Utm; } set { _Utm = value; OnPropertyChanged(); } }
        Utm _Utm = new Utm(); 

        // for visuals
        public string PointName { get { return _PointName; } set { _PointName = value; OnPropertyChanged(); } }
        string _PointName = "N/A";
    }
}
