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
            private bool _continue = true;

            public TestRunVisitor(BinaryWriter writer)
            {
                _writer = writer;
            }

            private void Process(string displayName, TestState state, string output = "")
            {
                Console.WriteLine($"{state} - {displayName}");
                var result = new TestResultData(displayName, state, output);

                try
                {
                    result.WriteTo(_writer);
                }
                catch (Exception ex)
                {
                    // This happens during a rude shutdown from the client.
                    Console.Error.WriteLine(ex.Message);
                    _continue = false;
                }
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

                return _continue;
            }

            protected override bool Visit(ITestPassed testPassed)
            {
                Process(testPassed.TestCase.DisplayName, TestState.Passed);
                return _continue;
            }

            protected override bool Visit(ITestSkipped testSkipped)
            {
                Process(testSkipped.TestCase.DisplayName, TestState.Skipped);
                return _continue;
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
