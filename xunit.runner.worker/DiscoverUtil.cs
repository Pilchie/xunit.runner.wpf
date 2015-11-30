using System.IO;
using Xunit;
using Xunit.Abstractions;
using xunit.runner.data;

namespace xunit.runner.worker
{
    internal sealed class DiscoverUtil
    {
        private sealed class Impl : TestDiscoverySink
        {
            private readonly ClientWriter _writer;

            internal Impl(ClientWriter writer)
            {
                _writer = writer;
            }

            protected override bool ShouldContinue => _writer.IsConnected;

            protected override void OnTestDiscovered(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                var testCaseData = new TestCaseData(
                    testCase.DisplayName,
                    testCaseDiscovered.TestAssembly.Assembly.AssemblyPath,
                    testCase.Traits);

                _writer.Write(TestDataKind.Value);
                _writer.Write(testCaseData);
            }
        }

        internal static void Go(string assemblyFileName, Stream stream)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(
                AppDomainSupport.Denied,
                assemblyFileName: assemblyFileName,
                diagnosticMessageSink: new MessageVisitor(),
                shadowCopy: false))
            using (var writer = new ClientWriter(stream))
            using (var impl = new Impl(writer))
            {
                var testAssemblyConfiguration = ConfigReader.Load(assemblyFileName);
                xunit.Find(includeSourceInformation: false, messageSink: impl, discoveryOptions: TestFrameworkOptions.ForDiscovery(testAssemblyConfiguration));
                impl.Finished.WaitOne();

                writer.Write(TestDataKind.EndOfData);
            }
        }
    }
}
