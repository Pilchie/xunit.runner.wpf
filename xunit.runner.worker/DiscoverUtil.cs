using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using xunit.runner.data;
using System.Threading;

namespace xunit.runner.worker
{
    internal sealed class DiscoverUtil
    {
        private sealed class Impl : LongLivedMarshalByRefObject, IMessageSink, IDisposable
        {
            private readonly ITestFrameworkDiscoverer _discoverer;
            private readonly ClientWriter _writer;

            public ManualResetEvent Finished { get; private set; }

            internal Impl(ITestFrameworkDiscoverer discoverer, ClientWriter writer)
            {
                Finished = new ManualResetEvent(false);
                _discoverer = discoverer;
                _writer = writer;
            }

            public bool OnMessage(IMessageSinkMessage message)
            {
                var discoveryMessage = message as ITestCaseDiscoveryMessage;
                if (discoveryMessage != null)
                {
                    OnDiscoveryMessage(discoveryMessage);
                }

                if (message is IDiscoveryCompleteMessage)
                {
                    Finished.Set();
                }

                return _writer.IsConnected;
            }

            private bool OnDiscoveryMessage(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                var testCaseData = new TestCaseData(
                    testCase.DisplayName,
                    testCaseDiscovered.TestAssembly.Assembly.AssemblyPath,
                    testCase.Traits);

                _writer.Write(TestDataKind.Value);
                _writer.Write(testCaseData);

                return _writer.IsConnected;
            }

            public void Dispose()
            {
                ((IDisposable)Finished).Dispose();
            }
        }

        internal static void Go(string fileName, Stream stream)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(
                AppDomainSupport.Denied,
                assemblyFileName: fileName,
                diagnosticMessageSink: new MessageVisitor(),
                shadowCopy: false))
            using (var writer = new ClientWriter(stream))
            using (var impl = new Impl(xunit, writer))
            {
                xunit.Find(includeSourceInformation: false, messageSink: impl, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                impl.Finished.WaitOne();

                writer.Write(TestDataKind.EndOfData);
            }
        }
    }
}
