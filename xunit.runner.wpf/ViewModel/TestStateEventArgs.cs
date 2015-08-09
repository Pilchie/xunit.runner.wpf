using System;

namespace xunit.runner.wpf.ViewModel
{
    public class TestStateEventArgs : EventArgs
    {
        public static TestStateEventArgs Failed { get; } = new TestStateEventArgs(TestState.Failed);
        public static TestStateEventArgs Passed { get; } = new TestStateEventArgs(TestState.Passed);
        public static TestStateEventArgs Skipped { get; } = new TestStateEventArgs(TestState.Skipped);
        private TestStateEventArgs(TestState state)
        {
            this.State = state;
        }

        public TestState State { get; }
    }
}