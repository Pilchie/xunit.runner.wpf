using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf
{
    internal interface ITestUtil
    {
        /// <summary>
        /// Discover the list of test cases which are available in the specified assembly.
        /// </summary>
        List<TestCaseViewModel> Discover(string assemblyPath);
    }
}
