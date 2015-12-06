using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Runner.Data;

namespace Xunit.Runner.Wpf
{
    internal interface ITestUtil
    {
        /// <summary>
        /// Discover the list of test cases which are available in the specified assembly.
        /// </summary>
        Task Discover(
            string assebmlyFileName,
            Action<IEnumerable<TestCaseData>> testsDiscovered,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begin a run of all unit tests for the given assembly.
        /// </summary>
        Task RunAll(
            string assemblyFileName,
            Action<IEnumerable<TestResultData>> testsFinished,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begin a run of specific unit tests for the given assembly.
        /// </summary>
        Task RunSpecific(
            string assemblyFileName,
            ImmutableArray<string> testCasesToRun,
            Action<IEnumerable<TestResultData>> testsFinished,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
