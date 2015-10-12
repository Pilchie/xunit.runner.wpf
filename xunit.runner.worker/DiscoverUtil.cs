using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using xunit.runner.data;

namespace xunit.runner.worker
{
    internal sealed class DiscoverUtil
    {
        private sealed class Impl : TestMessageVisitor<IDiscoveryCompleteMessage>
        {
            private readonly ITestFrameworkDiscoverer _discoverer;
            private readonly ClientWriter _writer;
            private readonly Dictionary<string, List<string>> _traitMap;

            internal Impl(ITestFrameworkDiscoverer discoverer, ClientWriter writer)
            {
                _discoverer = discoverer;
                _writer = writer;
                _traitMap = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            }

            protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                var testCaseData = new TestCaseData(
                    _discoverer.Serialize(testCase),
                    testCase.DisplayName,
                    testCaseDiscovered.TestAssembly.Assembly.AssemblyPath,
                    testCase.Traits);

                Console.WriteLine(testCase.DisplayName);
                _writer.Write(TestDataKind.Value);
                _writer.Write(testCaseData);

                return _writer.IsConnected;
            }
        }

        internal static void Go(string fileName, Stream stream)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(
                AppDomainSupport.IfAvailable,
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
