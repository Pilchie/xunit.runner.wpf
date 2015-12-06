using System.Threading;
using Xunit.Abstractions;

namespace Xunit.Runner.Worker.MessageSinks
{
    internal abstract class BaseTestDiscoverySink : BaseMessageSink
    {
        public ManualResetEvent Finished { get; }

        protected BaseTestDiscoverySink()
        {
            Finished = new ManualResetEvent(false);
        }

        protected override void DisposeCore(bool disposing)
        {
            Finished.Dispose();
        }

        protected override bool OnMessage(IMessageSinkMessage message)
        {
            var discoveryMessage = message as ITestCaseDiscoveryMessage;
            if (discoveryMessage != null)
            {
                OnTestDiscovered(discoveryMessage);
            }

            if (message is IDiscoveryCompleteMessage)
            {
                Finished.Set();
            }

            return ShouldContinue;
        }

        protected virtual bool ShouldContinue => true;

        protected abstract void OnTestDiscovered(ITestCaseDiscoveryMessage testCaseDiscovered);
    }
}
