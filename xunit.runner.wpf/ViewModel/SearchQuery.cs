using System.Collections.Generic;

namespace xunit.runner.wpf.ViewModel
{
    public class SearchQuery
    {
        public bool IncludeFailedTests = true;
        public bool IncludePassedTests = true;
        public bool IncludeSkippedTests = true;
        public string SearchString = string.Empty;
        public ISet<TraitViewModel> TraitSet = new HashSet<TraitViewModel>(TraitViewModel.EqualityComparer);
    }
}
