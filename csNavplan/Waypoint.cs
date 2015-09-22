using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace csNavplan
{
    // todo Waypoint should sub-class PlanPoint?
    public class Waypoint : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        // todo change isAction bool to wayPointType enum
        // todo would be nice to set background color based on enum

        public int Sequence { get { return _Sequence; } set { _Sequence = value; OnPropertyChanged(); } }
        int _Sequence = 0;

        public Point XY { get { return _XY; } set { _XY = value; OnPropertyChanged(); } }
        Point _XY;

        public Point Local { get { return _Local; } set { _Local = value; OnPropertyChanged(); } }
        Point _Local = new Point(); 

        public bool isAction { get { return _isAction; } set { _isAction = value; OnPropertyChanged(); OnPropertyChanged("isActionString"); } }
        bool _isAction = false;

        public string isActionString { get { return isAction ? "Action" : ""; } }
    }

}
