using System;
using System.Collections.Generic;
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
        ITestDiscoverSession Discover(string assemblyPath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begin a run of all unit tests for the given assembly.
        /// </summary>
        ITestRunSession RunAll(string assemblyPath, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Begin a run of specific unit tests for the given assembly.
        /// </summary>
        ITestRunSession RunSpecific(string assemblyPath, IEnumerable<string> testCaseDisplayNames, CancellationToken cancellationToken = default(CancellationToken));
    }

    internal interface ITestSession
    {
        /// <summary>
        /// Task which will be resolved when the session is completed.
        /// </summary>
        Task Task { get; }
    }

    internal interface ITestRunSession : ITestSession
    {
        /// <summary>
        /// Raised when an idividual test is finished running.
        /// </summary>
        event EventHandler<TestResultDataEventArgs> TestFinished;

        /// <summary>
        /// Raised when the session has finished executing all of the specified tests.
        /// </summary>
        event EventHandler SessionFinished;
    }

    internal interface ITestDiscoverSession : ITestSession
    {
        /// <summary>
        /// Raised when an idividual test is finished running.
        /// </summary>
        event EventHandler<TestCaseDataEventArgs> TestDiscovered;

        /// <summary>
        /// Raised when the session has finished executing all of the specified tests.
        /// </summary>
        event EventHandler SessionFinished;
    }

    internal sealed class TestCaseDataEventArgs : EventArgs
    {
        internal readonly TestCaseData TestCaseData;

        internal TestCaseDataEventArgs(TestCaseData data)
        {
            TestCaseData = data;
        }
    }

    internal sealed class TestResultDataEventArgs : EventArgs
    {
        internal readonly TestResultData TestResultData;

        internal string TestCaseDisplayName => TestResultData.TestCaseDisplayName;
        internal TestState TestState => TestResultData.TestState;
        internal string Output => TestResultData.Output;

        internal TestResultDataEventArgs(TestResultData testResultData)
        {
            TestResultData = testResultData;
        }
    }

}
