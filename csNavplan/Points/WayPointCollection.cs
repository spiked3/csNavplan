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
    public class WayPointCollection : SortableObservableCollection<BasePoint>
    {
        
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
