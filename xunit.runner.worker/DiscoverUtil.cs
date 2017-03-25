using System.IO;
using Xunit.Abstractions;
using Xunit.Runner.Data;
using Xunit.Runner.Worker.MessageSinks;

namespace Xunit.Runner.Worker
{
    internal sealed class DiscoverUtil : XunitUtil
    {
        private sealed class TestDiscoverySink : BaseTestDiscoverySink
        {
            private readonly ClientWriter _writer;

            internal TestDiscoverySink(ClientWriter writer)
            {
                _writer = writer;
            }

            protected override bool ShouldContinue => _writer.IsConnected;

            protected override void OnTestDiscovered(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                var testCaseData = new TestCaseData(
                    testCase.DisplayName,
                    testCase.UniqueID,
                    testCase.SkipReason,
                    testCaseDiscovered.TestAssembly.Assembly.AssemblyPath,
                    testCase.Traits);

                _writer.Write(TestDataKind.Value);
                _writer.Write(testCaseData);
            }
        }

        internal static void Go(string assemblyFileName, Stream stream)
        {
            Go(assemblyFileName, stream, AppDomainSupport.Denied,
                (xunit, configuration, writer) =>
                {
                    using (var sink = new TestDiscoverySink(writer))
                    {
                        xunit.Find(includeSourceInformation: false, messageSink: sink,
                            discoveryOptions: TestFrameworkOptions.ForDiscovery(configuration));

                        sink.Finished.WaitOne();

                        writer.Write(TestDataKind.EndOfData);
                    }
                });
        }
    }
}
