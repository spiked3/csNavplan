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
    // move the bitmap to here
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

        public string ImageFileName { get { return _ImageFileName; } set { _ImageFileName = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        string _ImageFileName;
        public PlanPoint ImageData { get { return _ImageData; } set { _ImageData = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _ImageData = new PlanPoint { PointName = "Image" }; 
        public PlanPoint Align1 { get { return _Align1; } set { _Align1 = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _Align1 = new PlanPoint { PointName = "Align1" };
        public PlanPoint Align2 { get { return _Align2; } set { _Align2 = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _Align2 = new PlanPoint { PointName = "Align2" };
        public PlanPoint Origin { get { return _Origin; } set { _Origin = value; OnPropertyChanged(); OnAlignmentPointChanged(); } }
        PlanPoint _Origin = new PlanPoint { AB = new Point(50, 50), XY = new Point(0,0), PointName = "Origin" };   // default middle

        public event EventHandler AlignmentPointChanged;

        double horizontalProportion, verticalProportion;

        public Point Utm2Gps(Point g)
        {
            return Utm.ToLonLat(g.X, g.Y, "10");    // +++ hardcoded zone
        }

        public Point Pct2Utm(Point a)
        {
            return new Point(a.X * horizontalProportion, a.Y * verticalProportion);
        }

        public Point Screen2Local(Point a)
        {
            var u = Pct2Utm(a);
            return new Point(a.X - Origin.UtmCoord.X, a.Y - Origin.UtmCoord.Y);
        }

        private void OnAlignmentPointChanged()
        {
            // calculate new proportions - pctg diff / utm diff
            horizontalProportion = Math.Abs(Align2.AB.X - Align1.AB.X) / Math.Abs(Align2.UtmCoord.X - Align1.UtmCoord.X);
            verticalProportion = (Align1.AB.Y - Align2.AB.Y) / (Align1.UtmCoord.Y - Align2.UtmCoord.Y);            

            if (AlignmentPointChanged != null)
                AlignmentPointChanged(this, EventArgs.Empty);
        }
    }
}
