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
        List<TestCaseViewModel> Discover(string assemblyPath);

        /// <summary>
        /// Begin a run of a unit test for the given assembly.
        /// </summary>
        ITestRunSession Run(Dispatcher dispatcher, string assemblyPath, CancellationToken cancellationToken = default(CancellationToken));
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

    internal interface ITestRunSession
    {
        bool IsRunning { get; }

        /// <summary>
        /// Raised when an idividual test is finished running.
        /// </summary>
        event EventHandler<TestResultDataEventArgs> TestFinished;

        /// <summary>
        /// Raised when the session has finished executing all of the specified tests.
        /// </summary>
        event EventHandler SessionFinished;
    }
}
