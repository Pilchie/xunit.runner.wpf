using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.data
{
    /// <summary>
    /// Note: More severe states are higher numbers.
    /// <see cref="MainViewModel.TestRunVisitor_TestFinished(object, TestStateEventArgs)"/>
    /// </summary>
    public enum TestState
    {
        All = 0,
        NotRun,
        Passed,
        Skipped,
        Failed,
    }

    public sealed class TestResultData
    {
        public string TestCaseDisplayName { get; set; }
        public TestState TestState { get; set; }

        public TestResultData(string displayName, TestState state)
        {
            TestCaseDisplayName = displayName;
            TestState = state;
        }

        public static TestResultData ReadFrom(BinaryReader reader)
        {
            var displayName = reader.ReadString();
            var state = (TestState)reader.ReadInt32();
            return new TestResultData(displayName, state);
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TestCaseDisplayName);
            writer.Write((int)TestState);
        }
    }
}
