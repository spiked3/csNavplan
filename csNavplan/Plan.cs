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
    public class Plan : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        public string ImageFileName { get { return _ImageFileName; } set { _ImageFileName = value; OnPropertyChanged(); } }
        string _ImageFileName;
        public PlanPoint Align1 { get { return _Align1; } set { _Align1 = value; OnPropertyChanged(); } }
        PlanPoint _Align1 = new PlanPoint { PointName = "Align1" };
        public PlanPoint Align2 { get { return _Align2; } set { _Align2 = value; OnPropertyChanged(); } }
        PlanPoint _Align2 = new PlanPoint { PointName = "Align2" };
        public PlanPoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); } }
        PlanPoint _Origin = new PlanPoint { AB = new NpPoint(50, 50), XY = new NpPoint(0,0), PointName = "Origin" };   // default middle
    }
}
