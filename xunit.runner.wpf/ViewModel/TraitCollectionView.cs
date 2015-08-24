using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.wpf.ViewModel
{
    public sealed partial class TraitCollectionView
    {
        private readonly TraitViewModelComparer _comparer = new TraitViewModelComparer();
        private readonly ObservableCollection<TraitViewModel> _collection = new ObservableCollection<TraitViewModel>();

        public ObservableCollection<TraitViewModel> Collection => _collection;

        public TraitCollectionView()
        {

        }

        public void Add(Dictionary<string, List<string>> traitMap)
        {
            if (traitMap.Count == 0)
            {
                return;
            }

            foreach (var pair in traitMap)
            {
                foreach (var value in pair.Value)
                {
                    TryInsert(new TraitViewModel(pair.Key, value));
                }
            }
        }

        private void TryInsert(TraitViewModel trait)
        {
            // TODO: make it a binary search
            for (int i = 0; i < _collection.Count; i++)
            {
                var current = _collection[i];
                var result = _comparer.Compare(trait, current);
                if (result < 0)
                {
                    _collection.Insert(i, trait);
                    return;
                }

                if (result == 0)
                {
                    return;
                }
            }

            _collection.Add(trait);
        }
    }
}
