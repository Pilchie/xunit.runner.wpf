using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Xunit.Runner.Wpf.ViewModel
{
    public sealed partial class TraitCollectionView
    {
        public ObservableCollection<TraitViewModel> Collection { get; } = new ObservableCollection<TraitViewModel>();

        public void AddRange(IEnumerable<TraitViewModel> traits)
        {
            foreach (var trait in traits)
            {
                var index = Collection.BinarySearch(trait, TraitViewModel.Comparer);
                if (index < 0)
                {
                    Collection.Insert(~index, trait);
                }
                else
                {
                    // This trait already exists, add more values.
                    var originalTrait = Collection[index];
                    originalTrait.AddValues(trait.Children.Select(x => x.Text));
                }
            }
        }

        public TraitViewModel GetOrAdd(string text)
        {
            var index = this.Collection.BinarySearch(text, StringComparer.Ordinal, vm => vm.Text);

            if (index < 0)
            {
                var viewModel = new TraitViewModel(text);
                this.Collection.Insert(~index, viewModel);
                return viewModel;
            }

            return this.Collection[index];
        }

        public ISet<TraitViewModel> GetCheckedTraits()
        {
            return new HashSet<TraitViewModel>(
                Collection.SelectMany(x => x.Children).Where(x => x.IsChecked == true || x.IsChecked == false),
                comparer: TraitViewModel.EqualityComparer);
        }
    }
}
