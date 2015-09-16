using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace csNavplan
{
    public class WaypointCollection : ObservableCollection<Waypoint>
    {

    }

    public class Waypoint
    {
        public Point XY { get; set; }
    }
}
