using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf.Impl
{
    internal sealed partial class RemoteTestUtil : ITestUtil
    {
        private readonly Dispatcher _dispatcher;

        internal RemoteTestUtil(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        private static Connection StartWorkerProcess(string action, string argument)
        {
            var pipeName = $"xunit.runner.wpf.pipe.{Guid.NewGuid()}";
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = typeof(xunit.runner.worker.Program).Assembly.Location;
            processStartInfo.Arguments = $"{pipeName} {action} {argument}";
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            var process = Process.Start(processStartInfo);
            try
            {
                var stream = new NamedPipeClientStream(pipeName);
                stream.Connect();
                return new Connection(stream, process);
            }
            catch
            {
                process.Kill();
                throw;
            }
        }

        private DiscoverSession Discover(string assemblyPath, CancellationToken cancellationToken)
        {
            var connection = StartWorkerProcess(Constants.ActionDiscover, assemblyPath);
            return new DiscoverSession(connection, _dispatcher, cancellationToken);
        }

        private RunSession RunAll(string assemblyPath, CancellationToken cancellationToken)
        {
            var connection = StartWorkerProcess(Constants.ActionRunAll, assemblyPath);
            return new RunSession(connection, _dispatcher, cancellationToken);
        }

        #region ITestUtil

        ITestDiscoverSession ITestUtil.Discover(string assemblyPath, CancellationToken cancellationToken)
        {
            return Discover(assemblyPath, cancellationToken);
        }

        ITestRunSession ITestUtil.RunAll(string assemblyPath, CancellationToken cancellationToken)
        {
            return RunAll(assemblyPath, cancellationToken);
        }

        #endregion
    }
}
