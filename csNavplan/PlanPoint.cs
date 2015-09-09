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
    public class NpPoint : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        public float X { get { return _X; } set { _X = value; OnPropertyChanged(); } }
        float _X = 0;
        public float Y { get { return _Y; } set { _Y = value; OnPropertyChanged(); } }
        float _Y = 0;

        public NpPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
        public NpPoint(double x, double y)
        {
            X = (float)x;
            Y = (float)y;
        }

        public static explicit operator Point(NpPoint a)
        {
            return new Point(a.X, a.Y);
        }
    }

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
        public NpPoint XY { get { return _XY; } set { _XY.X = value.X; _XY.Y = value.Y; OnPropertyChanged(); } }
        NpPoint _XY = new NpPoint(0,0);

        // AB is pct point on image (arbitrary)
        public NpPoint AB { get { return _AB; } set { _AB.X = value.X; _AB.Y = value.Y; OnPropertyChanged(); } }
        NpPoint _AB = new NpPoint(0,0);

        // GPS is longitude and lattitude - if null it has NOT been entered, use UTM as local coordinates
        public NpPoint GpsCoord { get { return _GpsCoord; } set { _GpsCoord = value; OnPropertyChanged(); } }
        NpPoint _GpsCoord = null;

        // UTM is computed or designated coordinates in meters
        // if GPS is entered, UTM is computed, if manually entered it is left alone
        public NpPoint UtmCoord { get { return _UtmCoord; } set { _UtmCoord = value; OnPropertyChanged(); } }
        NpPoint _UtmCoord = new NpPoint(0,0); 

        // for visuals
        public string PointName { get { return _PointName; } set { _PointName = value; OnPropertyChanged(); } }
        string _PointName = "N/A";
    }
}
