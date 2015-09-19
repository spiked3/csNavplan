using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace csNavplan
{
    public class WaypointCollection : SortableObservableCollection<Waypoint>
    {
        
    }

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

        public bool isAction { get { return _isAction; } set { _isAction = value; OnPropertyChanged(); OnPropertyChanged("isActionString"); } }
        bool _isAction = false; 

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
