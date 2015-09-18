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

namespace csNavplan
{
    /// <summary>
    /// Interaction logic for WayPointCRUD.xaml
    /// </summary>
    public partial class WayPointCRUD : UserControl
    {
        public WayPointCRUD()
        {
            InitializeComponent();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            WaypointCollection wc = (DataContext as Plan).Waypoints;
            var waypointsToRemove = waypointListView1.SelectedItems;
            while (waypointsToRemove.Count > 0)
                wc.Remove((Waypoint)waypointsToRemove[0]);
        }

        private void Renum_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as Plan).ResetSequenceNumbers();
            waypointListView1.Items.Refresh();
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (waypointListView1.SelectedItems.Count != 1) return;
            var moveeIdx = (DataContext as Plan).Waypoints.IndexOf((Waypoint)waypointListView1.SelectedItem);
            if (moveeIdx > 0)
                (DataContext as Plan).Waypoints.Move(moveeIdx, moveeIdx - 1);
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            if (waypointListView1.SelectedItems.Count != 1) return;
            var moveeIdx = (DataContext as Plan).Waypoints.IndexOf((Waypoint)waypointListView1.SelectedItem);
            if (moveeIdx < (DataContext as Plan).Waypoints.Count - 1)
                (DataContext as Plan).Waypoints.Move(moveeIdx, moveeIdx + 1);
        }
    }
}
