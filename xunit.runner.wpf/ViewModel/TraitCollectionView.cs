using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace xunit.runner.wpf.ViewModel
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

        public ISet<TraitViewModel> GetCheckedTraits()
        {
            return new HashSet<TraitViewModel>(
                Collection.SelectMany(x => x.Children).Where(x => x.IsChecked == true),
                comparer: TraitViewModel.EqualityComparer);
        }
    }
}
