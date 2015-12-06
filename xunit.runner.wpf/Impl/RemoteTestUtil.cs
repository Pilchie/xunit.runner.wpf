using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using xunit.runner.data;

namespace xunit.runner.wpf.Impl
{
    internal sealed partial class RemoteTestUtil : ITestUtil
    {
        private struct ProcessInfo
        {
            internal readonly string PipeName;
            internal readonly Process Process;

            internal ProcessInfo(string pipeName, Process process)
            {
                PipeName = pipeName;
                Process = process;
            }
        }

        private readonly Dispatcher _dispatcher;
        private ProcessInfo? _processInfo;

        internal RemoteTestUtil(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _processInfo = StartWorkerProcess();
        }

        private async Task<Connection> CreateConnection(string action, string argument)
        {
            var pipeName = GetPipeName();

            try
            {
                var stream = new NamedPipeClientStream(pipeName);
                await stream.ConnectAsync();

                var writer = new ClientWriter(stream);
                writer.Write(action);
                writer.Write(argument);

                return new Connection(stream);
            }
            catch
            {
                try
                {
                    _processInfo?.Process.Kill();
                }
                catch
                {
                    // Inherent race condition here.  Just need to make sure the process is 
                    // dead as it can't even handle new connections.
                }

                throw;
            }
        }

        private string GetPipeName()
        {
            var process = _processInfo?.Process;
            if (process != null && !process.HasExited)
            {
                return _processInfo.Value.PipeName;
            }

            _processInfo = StartWorkerProcess();
            return _processInfo.Value.PipeName;
        }

        private static ProcessInfo StartWorkerProcess()
        {
            var pipeName = $"xunit.runner.wpf.pipe.{Guid.NewGuid()}";
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = typeof(xunit.runner.worker.Program).Assembly.Location;
            processStartInfo.Arguments = $"{pipeName} {Process.GetCurrentProcess().Id}";
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            var process = Process.Start(processStartInfo);
            return new ProcessInfo(pipeName, process);
        }

        private void RecycleProcess()
        {
            var process = _processInfo?.Process;
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }

            _processInfo = StartWorkerProcess();
        }

        private async Task Discover(string assemblyPath, Action<List<TestCaseData>> callback, CancellationToken cancellationToken)
        {
            var connection = await CreateConnection(Constants.ActionDiscover, assemblyPath);
            await ProcessResultsCore(connection, r => r.ReadTestCaseData(), callback, cancellationToken);

            RecycleProcess();
        }

        private async Task RunCore(string actionName, string assemblyPath, ImmutableArray<string> testCaseDisplayNames, Action<List<TestResultData>> callback, CancellationToken cancellationToken)
        {
            var connection = await CreateConnection(actionName, assemblyPath);

            if (!testCaseDisplayNames.IsDefaultOrEmpty)
            {
                var backgroundWriter = new BackgroundWriter<string>(new ClientWriter(connection.Stream), testCaseDisplayNames, (w, s) => w.Write(s), cancellationToken);
                await backgroundWriter.WriteAsync();
            }

            await ProcessResultsCore(connection, r => r.ReadTestResultData(), callback, cancellationToken);
        }

        private async Task ProcessResultsCore<T>(Connection connection, Func<ClientReader, T> readValue, Action<List<T>> callback, CancellationToken cancellationToken)
            where T : class
        {
            var queue = new ConcurrentQueue<T>();
            var backgroundReader = new BackgroundReader<T>(queue, new ClientReader(connection.Stream), readValue);
            var backgroundProducer = new BackgroundProducer<T>(connection, _dispatcher, queue, callback);

            await backgroundReader.ReadAsync(cancellationToken);
            await backgroundProducer.Task;
        }

        #region ITestUtil

        Task ITestUtil.Discover(string assemblyPath, Action<IEnumerable<TestCaseData>> callback, CancellationToken cancellationToken)
        {
            return Discover(assemblyPath, callback, cancellationToken);
        }

        Task ITestUtil.RunAll(string assemblyPath, Action<IEnumerable<TestResultData>> callback, CancellationToken cancellationToken)
        {
            return RunCore(Constants.ActionRunAll, assemblyPath, ImmutableArray<string>.Empty, callback, cancellationToken);
        }

        Task ITestUtil.RunSpecific(string assemblyPath, ImmutableArray<string> testCaseDisplayNames, Action<IEnumerable<TestResultData>> callback, CancellationToken cancellationToken)
        {
            return RunCore(Constants.ActionRunSpecific, assemblyPath, testCaseDisplayNames, callback, cancellationToken);
        }

        #endregion
    }
}
