using System;
using System.Collections.Generic;

namespace xunit.runner.wpf
{
    public static partial class Extensions
    {
        private class FuncComparer<T> : IComparer<T>
        {
            private readonly Func<T, T, int> _comparison;

            public FuncComparer(Func<T, T, int> comparison)
            {
                _comparison = comparison;
            }

            public int Compare(T x, T y) => _comparison(x, y);
        }
    }
}
