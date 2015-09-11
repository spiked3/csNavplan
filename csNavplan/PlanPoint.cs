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
        public Point XY { get { return _XY; } set { _XY.X = value.X; _XY.Y = value.Y; OnPropertyChanged(); } }
        Point _XY = new Point(0,0);

        // AB is pct point on image (arbitrary)
        public Point AB { get { return _AB; } set { _AB.X = value.X; _AB.Y = value.Y; OnPropertyChanged(); } }
        Point _AB = new Point(0,0);

        // GPS is longitude and lattitude - if null it has NOT been entered, use UTM as local coordinates
        public Point GpsCoord { get { return _GpsCoord; } set { _GpsCoord = value; OnPropertyChanged(); } }
        Point _GpsCoord;

        // UTM is computed or designated coordinates in meters
        // if GPS is entered, UTM is computed, if manually entered it is left alone
        public Point UtmCoord { get { return _UtmCoord; } set { _UtmCoord = value; OnPropertyChanged(); } }
        Point _UtmCoord = new Point(0,0); 

        // for visuals
        public string PointName { get { return _PointName; } set { _PointName = value; OnPropertyChanged(); } }
        string _PointName = "N/A";
    }
}
