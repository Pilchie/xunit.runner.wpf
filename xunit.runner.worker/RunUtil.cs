using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.data;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.worker
{
    internal sealed class RunUtil
    {
        private class TestRunVisitor : TestMessageVisitor<ITestAssemblyFinished>
        {
            private readonly BinaryWriter _writer;

            public TestRunVisitor(BinaryWriter writer)
            {
                _writer = writer;
            }

            private void Process(string displayName, TestState state)
            {
                Console.WriteLine($"{state} - {displayName}");
                var result = new TestResultData(displayName, state);
                result.WriteTo(_writer);
            }

            protected override bool Visit(ITestFailed testFailed)
            {
                Process(testFailed.TestCase.DisplayName, TestState.Failed);
                return true;
            }

            protected override bool Visit(ITestPassed testPassed)
            {
                Process(testPassed.TestCase.DisplayName, TestState.Passed);
                return true;
            }

            protected override bool Visit(ITestSkipped testSkipped)
            {
                Process(testSkipped.TestCase.DisplayName, TestState.Skipped);
                return true;
            }
        }

        internal static void Go(string assemblyPath, Stream stream)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(
                assemblyFileName: assemblyPath,
                useAppDomain: true,
                shadowCopy: false,
                diagnosticMessageSink: new MessageVisitor()))
            using (var writer = new BinaryWriter(stream, Constants.Encoding, leaveOpen: true))
            using (var testRunVisitor = new TestRunVisitor(writer))
            {
                xunit.RunAll(testRunVisitor, TestFrameworkOptions.ForDiscovery(), TestFrameworkOptions.ForExecution());
                testRunVisitor.Finished.WaitOne();
            }
        }
    }
}
