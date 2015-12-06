using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xunit.Runner.Wpf
{
    public static partial class Extensions
    {
        public static void AddRange<TList, TEnumerable>(this ICollection<TList> list, IEnumerable<TEnumerable> items) where TEnumerable : TList
        {
            foreach (var i in items)
            {
                list.Add(i);
            }
        }

        public static void AddRange<TList, TEnumerable>(this ObservableCollection<TList> list, IEnumerable<TEnumerable> items) where TEnumerable : TList
        {
            foreach (var i in items)
            {
                list.Add(i);
            }
        }

        public static int BinarySearch<T, TValue>(this ObservableCollection<T> collection, int index, int length, TValue value, IComparer<TValue> comparer, Func<T, TValue> selector)
        {
            comparer = comparer ?? Comparer<TValue>.Default;

            var low = index;
            var high = (index + length) - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) / 2);
                var comp = comparer.Compare(selector(collection[mid]), value);

                if (comp == 0)
                {
                    return mid;
                }

                if (comp < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return ~low;
        }

        public static int BinarySearch<T, TValue>(this ObservableCollection<T> collection, TValue value, IComparer<TValue> comparer, Func<T, TValue> selector)
        {
            return collection.BinarySearch(0, collection.Count, value, comparer, selector);
        }

        public static int BinarySearch<T, TValue>(this ObservableCollection<T> collection, int index, int length, TValue value, Func<TValue, TValue, int> comparison, Func<T, TValue> selector)
        {
            return collection.BinarySearch(index, length, value, new FuncComparer<TValue>(comparison), selector);
        }

        public static int BinarySearch<T, TValue>(this ObservableCollection<T> collection, TValue value, Func<TValue, TValue, int> comparison, Func<T, TValue> selector)
        {
            return collection.BinarySearch(0, collection.Count, value, new FuncComparer<TValue>(comparison), selector);
        }

        public static int BinarySearch<T, TValue>(this ObservableCollection<T> collection, TValue value, Func<T, TValue> selector)
        {
            return collection.BinarySearch(0, collection.Count, value, comparer: null, selector: selector);
        }

        public static int BinarySearch<T>(this ObservableCollection<T> collection, int index, int length, T value, IComparer<T> comparer)
        {
            return collection.BinarySearch(index, length, value, comparer, x => x);
        }

        public static int BinarySearch<T>(this ObservableCollection<T> collection, T value, IComparer<T> comparer)
        {
            return collection.BinarySearch(0, collection.Count, value, comparer, x => x);
        }

        public static int BinarySearch<T>(this ObservableCollection<T> collection, T value)
        {
            return collection.BinarySearch(0, collection.Count, value, Comparer<T>.Default, x => x);
        }

        public static int BinarySearch<T>(this ObservableCollection<T> collection, int index, int length, T value, Func<T, T, int> comparison)
        {
            return collection.BinarySearch(index, length, value, new FuncComparer<T>(comparison), x => x);
        }

        public static int BinarySearch<T>(this ObservableCollection<T> collection, T value, Func<T, T, int> comparison)
        {
            return collection.BinarySearch(0, collection.Count, value, new FuncComparer<T>(comparison), x => x);
        }
    }
}
