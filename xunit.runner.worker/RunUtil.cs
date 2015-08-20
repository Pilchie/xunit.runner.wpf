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
            private readonly ClientWriter _writer;

            public TestRunVisitor(ClientWriter writer)
            {
                _writer = writer;
            }

            private void Process(string displayName, TestState state, string output = "")
            {
                Console.WriteLine($"{state} - {displayName}");
                var result = new TestResultData(displayName, state, output);

                _writer.Write(TestDataKind.Value);
                _writer.Write(result);
            }

            protected override bool Visit(ITestFailed testFailed)
            {
                var displayName = testFailed.TestCase.DisplayName;
                var builder = new StringBuilder();
                builder.AppendLine($"{displayName} FAILED:");
                for (int i = 0; i < testFailed.ExceptionTypes.Length; i++)
                {
                    builder.AppendLine($"\tException type: '{testFailed.ExceptionTypes[i]}', number: '{i}', parent: '{testFailed.ExceptionParentIndices[i]}'");
                    builder.AppendLine($"\tException message:");
                    builder.AppendLine(testFailed.Messages[i]);
                    builder.AppendLine($"\tException stacktrace");
                    builder.AppendLine(testFailed.StackTraces[i]);
                }
                builder.AppendLine();

                Process(testFailed.TestCase.DisplayName, TestState.Failed, builder.ToString());

                return _writer.IsConnected;
            }

            protected override bool Visit(ITestPassed testPassed)
            {
                Process(testPassed.TestCase.DisplayName, TestState.Passed);
                return _writer.IsConnected;
            }

            protected override bool Visit(ITestSkipped testSkipped)
            {
                Process(testSkipped.TestCase.DisplayName, TestState.Skipped);
                return _writer.IsConnected;
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
            using (var writer = new ClientWriter(stream))
            using (var testRunVisitor = new TestRunVisitor(writer))
            {
                xunit.RunAll(testRunVisitor, TestFrameworkOptions.ForDiscovery(), TestFrameworkOptions.ForExecution());
                testRunVisitor.Finished.WaitOne();
                writer.Write(TestDataKind.EndOfData);
            }
        }
    }
}
