using System.Threading;
using Xunit.Abstractions;

namespace Xunit.Runner.Worker.MessageSinks
{
    internal abstract class BaseTestRunSink : BaseMessageSink
    {
        public ManualResetEvent Finished { get; }

        protected BaseTestRunSink()
        {
            Finished = new ManualResetEvent(false);
        }

        protected override void DisposeCore(bool disposing)
        {
            Finished.Dispose();
        }

        protected override bool OnMessage(IMessageSinkMessage message)
        {
            var testFailed = message as ITestFailed;
            if (testFailed != null)
            {
                OnTestFailed(testFailed);
            }

            var testPassed = message as ITestPassed;
            if (testPassed != null)
            {
                OnTestPassed(testPassed);
            }

            var testSkipped = message as ITestSkipped;
            if (testSkipped != null)
            {
                OnTestSkipped(testSkipped);
            }

            if (message is ITestAssemblyFinished)
            {
                Finished.Set();
            }

            return ShouldContinue;
        }

        protected virtual bool ShouldContinue => true;

        protected abstract void OnTestFailed(ITestFailed testFailed);
        protected abstract void OnTestPassed(ITestPassed testPassed);
        protected abstract void OnTestSkipped(ITestSkipped testSkipped);
    }
}
