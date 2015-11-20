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
    public partial class WayPointCRUD : UserControl
    {
        public WayPointCRUD()
        {
            InitializeComponent();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            WayPointCollection wc = (DataContext as Plan).WayPoints;
            var waypointsToRemove = waypointListView1.SelectedItems;
            while (waypointsToRemove.Count > 0)
                wc.Remove((BaseNavPoint)waypointsToRemove[0]);
            waypointListView1.Items.Refresh();
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (waypointListView1.SelectedItems.Count != 1) return;
            var moveeIdx = (DataContext as Plan).WayPoints.IndexOf((BaseNavPoint)waypointListView1.SelectedItem);
            if (moveeIdx > 0)
                (DataContext as Plan).WayPoints.Move(moveeIdx, moveeIdx - 1);
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            if (waypointListView1.SelectedItems.Count != 1) return;
            var moveeIdx = (DataContext as Plan).WayPoints.IndexOf((BaseNavPoint)waypointListView1.SelectedItem);
            if (moveeIdx < (DataContext as Plan).WayPoints.Count - 1)
                (DataContext as Plan).WayPoints.Move(moveeIdx, moveeIdx + 1);
        }

        // todo, this sucks
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            Grid grid1 = (Grid)VisualTreeHelper.GetParent((DependencyObject)e.Source);
            BaseNavPoint p = grid1.DataContext as BaseNavPoint;
            waypointListView1.SelectedItem = p;

            var d = new NavPointEditDlg(p.PctPoint, p, (p is LocalPoint ? CoordinateType.Local : CoordinateType.World));
            //d.Owner = this;
            //d.Left = lastMouseRightX;
            //d.Top = lastMouseRightY;
            d.ShowDialog();
            if (d.DialogResult ?? false)
            {

                //+ I am here
            }
        }
    }
}
