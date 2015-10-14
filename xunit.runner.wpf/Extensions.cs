using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.wpf
{
    public static class Extensions
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
    }
}
