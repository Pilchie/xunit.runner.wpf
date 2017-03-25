// Copied from https://github.com/xunit/devices.xunit/blob/master/src/xunit.runner.devices/Utilities/FilteredCollectionView.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Xunit.Runner.Wpf
{
    public class FilteredCollectionView<T, TFilterArg> : IList<T>, IList, INotifyCollectionChanged, IDisposable
    {
        private readonly ObservableCollection<T> dataSource;
        private readonly List<T> filteredList;
        private readonly Func<T, TFilterArg, bool> filter;
        private readonly IComparer<T> sort;

        public FilteredCollectionView(ObservableCollection<T> dataSource, Func<T, TFilterArg, bool> filter, TFilterArg filterArgument, IComparer<T> sort)
        {
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (sort == null) throw new ArgumentNullException(nameof(sort));

            this.dataSource = dataSource;
            this.filter = filter;
            this.filterArgument = filterArgument;
            this.filteredList = new List<T>();
            this.sort = sort;

            this.dataSource.CollectionChanged += this.dataSource_CollectionChanged;

            foreach (var item in this.dataSource)
            {
                this.OnAdded(item);
            }
        }

        /// <summary>
        /// Raised when one of the items selected by the filter is changed.
        /// </summary>
        /// <remarks>
        /// The sender is reported to be the item changed.
        /// </remarks>
        public event EventHandler<PropertyChangedEventArgs> ItemChanged;

        protected virtual void OnItemChanged(T sender, PropertyChangedEventArgs args)
        {
            this.ItemChanged?.Invoke(sender, args);
        }

        private TFilterArg filterArgument;
        public TFilterArg FilterArgument
        {
            get { return this.filterArgument; }
            set
            {
                this.filterArgument = value;
                this.RefreshFilter();
            }
        }

        private void RefreshFilter()
        {
            this.filteredList.Clear();

            foreach (var item in this.dataSource)
            {
                if (this.filter(item, this.filterArgument))
                {
                    this.filteredList.Add(item);
                }
            }

            this.filteredList.Sort(sort);
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void dataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (T item in e.NewItems)
                    {
                        this.OnAdded(item);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems)
                    {
                        this.OnRemoved(item);
                    }

                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (T item in e.OldItems)
                    {
                        this.OnRemoved(item);
                    }

                    foreach (T item in e.NewItems)
                    {
                        this.OnAdded(item);
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
                default:
                    break;
            }
        }

        private void OnAdded(T item)
        {
            if (this.filter(item, this.filterArgument))
            {
                int index = this.filteredList.BinarySearch(item, sort);
                if (index < 0)
                {
                    this.filteredList.Insert(~index, item);
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, ~index));
                }
            }

            var observable = item as INotifyPropertyChanged;
            if (observable != null)
            {
                observable.PropertyChanged += this.dataSource_ItemChanged;
            }
        }

        private void OnRemoved(T item)
        {
            var observable = item as INotifyPropertyChanged;
            if (observable != null)
            {
                observable.PropertyChanged -= this.dataSource_ItemChanged;
            }

            int index = this.filteredList.BinarySearch(item, sort);
            if (index >= 0)
            {
                this.filteredList.RemoveAt(index);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            }
        }

        private void dataSource_ItemChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = (T)sender;
            int index = this.filteredList.BinarySearch(item, sort);
            if (this.filter(item, this.FilterArgument))
            {
                if (index < 0)
                {
                    this.filteredList.Insert(~index, item);
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, ~index));
                }
            }
            else if (index >= 0)
            {
                this.filteredList.RemoveAt(index);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            }

            this.OnItemChanged(item, e);
        }

        public void Dispose()
        {
            this.dataSource.CollectionChanged -= this.dataSource_CollectionChanged;

            foreach (var item in this.dataSource.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= this.dataSource_ItemChanged;
            }

            this.filteredList.Clear();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            this.CollectionChanged?.Invoke(this, args);
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item) => this.filteredList.BinarySearch(item, sort) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.filteredList.CopyTo(array, arrayIndex);
        }

        public int Count => this.filteredList.Count;

        public bool IsReadOnly => true;

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator() => this.filteredList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int IndexOf(T item)
        {
            int location = this.filteredList.BinarySearch(item, sort);
            if (location < 0)
                return -1;

            return location;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get
            {
                return this.filteredList[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object value) => this.Contains((T)value);

        int IList.IndexOf(object value) => this.IndexOf((T)value);

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        bool IList.IsFixedSize => false;

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            this.filteredList.CopyTo((T[])array, index);
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;
    }
}
