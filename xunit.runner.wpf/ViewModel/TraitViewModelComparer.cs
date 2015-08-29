using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.wpf.ViewModel
{
    internal sealed class TraitViewModelComparer : IEqualityComparer<TraitViewModel>, IComparer<TraitViewModel>
    {
        internal static readonly TraitViewModelComparer Instance = new TraitViewModelComparer();

        private readonly StringComparer _comparer = StringComparer.Ordinal;

        public int Compare(TraitViewModel x, TraitViewModel y)
        {
            var result = _comparer.Compare(x.Name, y.Name);
            if (result != 0)
            {
                return result;
            }

            return _comparer.Compare(x.Value, y.Value);
        }

        public bool Equals(TraitViewModel x, TraitViewModel y)
        {
            return _comparer.Equals(x.Name, y.Name)
                && _comparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(TraitViewModel obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
