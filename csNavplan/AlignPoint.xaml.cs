using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace csNavplan
{
    public partial class AlignPoint : UserControl
    {
        public AlignPoint()
        {
            InitializeComponent();
        }

        private void Gps_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlanPoint p = DataContext as PlanPoint;
            if (p != null)
                ScheduleUtmUpdate(p);
        }

        static readonly TimeSpan OneSecond = new TimeSpan(0, 0, 1);

        private void ScheduleUtmUpdate(PlanPoint p)
        {
            if (p.dt == null)
            {
                p.dt = new DispatcherTimer(OneSecond, DispatcherPriority.Background, UtmUpdate, this.Dispatcher);
                p.dt.Tag = p;
                p.dt.Start();
            }
            else
            {                
                p.dt.Stop();
                p.dt.Start();
            }            
        }

        private void UtmUpdate(object sender, EventArgs e)
        {
            DispatcherTimer dt = sender as DispatcherTimer;
            PlanPoint p = dt.Tag as PlanPoint;
            if (p == null) System.Diagnostics.Debugger.Break();

            if (p.GpsCoord == null)
            {
                p.GpsCoord = new Point(0, 0);
                return;
            }

            Utm u = new Utm(p.GpsCoord.Y, p.GpsCoord.X);
            p.UtmCoord = new Point(u.Easting, u.Northing);

            p.dt.Stop();
            p.dt = null;
        }

        private void Utm_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}
