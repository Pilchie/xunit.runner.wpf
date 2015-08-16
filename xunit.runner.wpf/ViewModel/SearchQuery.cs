using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.wpf.ViewModel
{
    public class SearchQuery
    {
        public bool IncludeFailedTests = true;
        public bool IncludePassedTests = true;
        public bool IncludeSkippedTests = true;

        public string SearchString = string.Empty;
    }
}
