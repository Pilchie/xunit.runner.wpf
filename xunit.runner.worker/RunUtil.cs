using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit.Runner.Data;
using Xunit.Abstractions;
using Xunit.Runner.Worker.MessageSinks;

namespace Xunit.Runner.Worker
{
    internal sealed class RunUtil : XunitUtil
    {
        private sealed class TestRunSink : BaseTestRunSink
        {
            private readonly ClientWriter _writer;

            public TestRunSink(ClientWriter writer)
            {
                _writer = writer;
            }

            protected override bool ShouldContinue => _writer.IsConnected;

            private void Process(string displayName, string uniqueID, TestState state, string output = "")
            {
                Console.WriteLine($"{state} - {displayName}");
                var result = new TestResultData(displayName, uniqueID, state, output);

                _writer.Write(TestDataKind.Value);
                _writer.Write(result);
            }

            protected override void OnTestFailed(ITestFailed testFailed)
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

                Process(testFailed.TestCase.DisplayName, testFailed.TestCase.UniqueID, TestState.Failed, builder.ToString());
            }

            protected override void OnTestPassed(ITestPassed testPassed)
            {
                Process(testPassed.TestCase.DisplayName, testPassed.TestCase.UniqueID, TestState.Passed);
            }

            protected override void OnTestSkipped(ITestSkipped testSkipped)
            {
                Process(testSkipped.TestCase.DisplayName, testSkipped.TestCase.UniqueID, TestState.Skipped);
            }
        }

        private sealed class TestDiscoverySink : BaseTestDiscoverySink
        {
            private readonly HashSet<string> _testCaseUniqueIDSet;
            private readonly List<ITestCase> _testCaseList;

            internal TestDiscoverySink(HashSet<string> testCaseUniqueIDSet, List<ITestCase> testCaseList)
            {
                _testCaseUniqueIDSet = testCaseUniqueIDSet;
                _testCaseList = testCaseList;
            }

            protected override void OnTestDiscovered(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                if (_testCaseUniqueIDSet.Contains(testCase.UniqueID))
                {
                    _testCaseList.Add(testCaseDiscovered.TestCase);
                }
            }
        }

        /// <summary>
        /// Read out the set of test case unique IDs to run.
        /// </summary>
        private static List<string> ReadTestCaseUniqueIDs(Stream stream)
        {
            using (var reader = new ClientReader(stream))
            {
                var list = new List<string>();
                while (reader.ReadKind() == TestDataKind.Value)
                {
                    list.Add(reader.ReadString());
                }

                return list;
            }
        }

        private static List<ITestCase> GetTestCaseList(XunitFrontController xunit, TestAssemblyConfiguration configuration, HashSet<string> testCaseNameSet)
        {
            var testCaseList = new List<ITestCase>();

            using (var sink = new TestDiscoverySink(testCaseNameSet, testCaseList))
            {
                xunit.Find(includeSourceInformation: false, messageSink: sink,
                    discoveryOptions: TestFrameworkOptions.ForDiscovery(configuration));

                sink.Finished.WaitOne();
            }

            return testCaseList;
        }

        internal static void RunAll(string assemblyFileName, Stream stream)
        {
            Go(assemblyFileName, stream, AppDomainSupport.IfAvailable,
                (xunit, configuration, writer) =>
                {
                    using (var sink = new TestRunSink(writer))
                    {
                        xunit.RunAll(sink,
                            discoveryOptions: TestFrameworkOptions.ForDiscovery(configuration),
                            executionOptions: TestFrameworkOptions.ForExecution(configuration));

                        sink.Finished.WaitOne();

                        writer.Write(TestDataKind.EndOfData);
                    }
                });
        }

        internal static void RunSpecific(string assemblyFileName, Stream stream)
        {
            var testCaseUniqueIDSet = new HashSet<string>(ReadTestCaseUniqueIDs(stream), StringComparer.Ordinal);

            Go(assemblyFileName, stream, AppDomainSupport.IfAvailable,
                (xunit, configuration, writer) =>
                {
                    using (var sink = new TestRunSink(writer))
                    {
                        var testCaseList = GetTestCaseList(xunit, configuration, testCaseUniqueIDSet);

                        xunit.RunTests(testCaseList, sink,
                            executionOptions: TestFrameworkOptions.ForExecution(configuration));

                        sink.Finished.WaitOne();

                        writer.Write(TestDataKind.EndOfData);
                    }
                });
        }
    }
}
