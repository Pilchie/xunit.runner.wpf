using System.IO;

namespace Xunit.Runner.Data
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
        public string TestCaseUniqueID { get; set; }
        public TestState TestState { get; set; }
        public string Output { get; set; }

        public TestResultData(string displayName, string uniqueID, TestState state, string output = "")
        {
            TestCaseDisplayName = displayName;
            TestCaseUniqueID = uniqueID;
            TestState = state;
            Output = output;
        }

        public static TestResultData ReadFrom(BinaryReader reader)
        {
            var displayName = reader.ReadString();
            var uniqueID = reader.ReadString();
            var state = (TestState)reader.ReadInt32();
            var output = reader.ReadString();
            return new TestResultData(displayName, uniqueID, state, output);
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TestCaseDisplayName);
            writer.Write(TestCaseUniqueID);
            writer.Write((int)TestState);
            writer.Write(Output);
        }
    }
}
