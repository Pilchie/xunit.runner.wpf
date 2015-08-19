using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        ITestRunSession Run(Dispatcher dispatcher, string assemblyPath);
    }

    internal sealed class TestResultEventArgs : EventArgs
    {
        internal readonly string TestCaseDisplayName;
        internal readonly TestState TestState;

        internal TestResultEventArgs(string displayName, TestState state)
        {
            TestCaseDisplayName = displayName;
            TestState = state;
        }
    }

    internal interface ITestRunSession
    {
        event EventHandler<TestResultEventArgs> TestFinished;
    }
}
