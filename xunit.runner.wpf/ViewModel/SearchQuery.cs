using System.Collections.Generic;

namespace Xunit.Runner.Wpf.ViewModel
{
    public class SearchQuery
    {
        public bool FilterRunningTests = false;
        public bool FilterFailedTests = false;
        public bool FilterPassedTests = false;
        public bool FilterSkippedTests = false;
        public string SearchString = string.Empty;
        public ISet<TraitViewModel> TraitSet = new HashSet<TraitViewModel>(TraitViewModel.EqualityComparer);
    }
}
