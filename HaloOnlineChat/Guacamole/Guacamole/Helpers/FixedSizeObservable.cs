using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Guacamole.Helpers
{
    public class FixedSizeObservable<T> : ObservableCollection<T>
    {

        private int _maxSize;

        public FixedSizeObservable(int maxSize)
        {
            _maxSize = maxSize;
        }


        protected override void InsertItem(int index, T item)
        {
                if (Count >= _maxSize)
                    base.RemoveAt(0);
                base.InsertItem(Count, item);
        }
    }
}
