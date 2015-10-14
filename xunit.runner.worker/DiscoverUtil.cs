using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using xunit.runner.data;
using System.Threading;

namespace xunit.runner.worker
{
    internal sealed class DiscoverUtil
    {
        private sealed class DiscoverySink : MarshalByRefObject, IMessageSink, IDisposable
        {
            private readonly ITestFrameworkDiscoverer _discoverer;
            private readonly ClientWriter _writer;
            private readonly Dictionary<string, List<string>> _traitMap;

            public ManualResetEvent Finished { get; }

            internal DiscoverySink(ITestFrameworkDiscoverer discoverer, ClientWriter writer)
            {
                this.Finished = new ManualResetEvent(false);

                _discoverer = discoverer;
                _writer = writer;
                _traitMap = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            }

            public bool OnMessage(IMessageSinkMessage message)
            {
                if (message is IDiscoveryCompleteMessage)
                {
                    // we've gotten the completion message. Signal and return false to receive no more messages.
                    this.Finished.Set();
                    return false;
                }

                var testCaseDiscovered = message as ITestCaseDiscoveryMessage;
                if (testCaseDiscovered != null)
                {
                    var testCase = testCaseDiscovered.TestCase;
                    var testCaseData = new TestCaseData(
                        testCase.DisplayName,
                        testCaseDiscovered.TestAssembly.Assembly.AssemblyPath,
                        testCase.Traits);

                    Console.WriteLine(testCase.DisplayName);
                    _writer.Write(TestDataKind.Value);
                    _writer.Write(testCaseData);
                }

                return _writer.IsConnected;
            }

            public void Dispose()
            {
                this.Finished.Dispose();
            }
        }

        internal static void Go(string fileName, Stream stream)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(
                AppDomainSupport.IfAvailable,
                assemblyFileName: fileName,
                shadowCopy: false))
            using (var writer = new ClientWriter(stream))
            using (var impl = new DiscoverySink(xunit, writer))
            {
                xunit.Find(includeSourceInformation: false, messageSink: impl, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                impl.Finished.WaitOne();
                writer.Write(TestDataKind.EndOfData);
            }
        }
    }
}
