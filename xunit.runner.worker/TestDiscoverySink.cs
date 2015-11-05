using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.worker
{
    internal abstract class TestDiscoverySink : LongLivedMarshalByRefObject, IMessageSink, IDisposable
    {
        public ManualResetEvent Finished { get; }

        protected TestDiscoverySink()
        {
            Finished = new ManualResetEvent(false);
        }

        public bool OnMessage(IMessageSinkMessage message)
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

        public void Dispose()
        {
            Finished.Dispose();
        }
    }
}
