using System;

namespace xunit.runner.wpf.ViewModel
{
    public class TestStateEventArgs : EventArgs
    {
        public static TestStateEventArgs Failed(string results) => new TestStateEventArgs(TestState.Failed, results);
        public static TestStateEventArgs Passed { get; } = new TestStateEventArgs(TestState.Passed);
        public static TestStateEventArgs Skipped { get; } = new TestStateEventArgs(TestState.Skipped);
        private TestStateEventArgs(TestState state, string results = null)
        {
            this.State = state;
            this.Results = results;
        }

        public TestState State { get; }
        public string Results { get; }
    }
}