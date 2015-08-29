using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf
{
    internal interface ITestUtil
    {
        /// <summary>
        /// Discover the list of test cases which are available in the specified assembly.
        /// </summary>
        Task Discover(string assemblyPath, Action<TestCaseData> callback, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begin a run of all unit tests for the given assembly.
        /// </summary>
        Task RunAll(string assemblyPath, Action<TestResultData> callback, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begin a run of specific unit tests for the given assembly.
        /// </summary>
        Task RunSpecific(string assemblyPath, ImmutableArray<string> testCaseDisplayNames, Action<TestResultData> callback, CancellationToken cancellationToken = default(CancellationToken));
    }
}
