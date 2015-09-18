using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace csNavplan
{
    public class WaypointCollection : SortableObservableCollection<Waypoint>
    {
    }

    public class Waypoint
    {
        // todo change isAction bool to wayPointType enum
        // todo would be nice to set background color based on enum
        // todo should we implement INotifyPropertyChange?
        public int Sequence { get; set; }
        // XY is actually a image percentage value, it must be converted to a local coordinates 
        public Point XY { get; set; }
        public bool isAction { get; set; }
        public string isActionString { get { return isAction ? "Action" : "";  } }
    }

    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        public SortableObservableCollection(IEnumerable<T> collection) :
            base(collection)
        { }

        public SortableObservableCollection() : base() { }

        public void Sort<TKey>(Func<T, TKey> keySelector)
        {
            Sort(Items.OrderBy(keySelector));
        }

        public void Sort<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            Sort(Items.OrderBy(keySelector, comparer));
        }

        public void SortDescending<TKey>(Func<T, TKey> keySelector)
        {
            Sort(Items.OrderByDescending(keySelector));
        }

        public void SortDescending<TKey>(Func<T, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            Sort(Items.OrderByDescending(keySelector, comparer));
        }

        public void Sort(IEnumerable<T> sortedItems)
        {
            List<T> sortedItemsList = sortedItems.ToList();
            for (int i = 0; i < sortedItemsList.Count; i++)
            {
                Items[i] = sortedItemsList[i];
            }
        }
    }
}
