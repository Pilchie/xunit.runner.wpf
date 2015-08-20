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
        private sealed class Connection : IDisposable
        {
            private NamedPipeClientStream _stream;
            private Process _process;
            private ClientReader _reader;

            internal NamedPipeClientStream Stream => _stream;

            internal ClientReader Reader => _reader;

            internal Connection(NamedPipeClientStream stream, Process process)
            {
                _stream = stream;
                _process = process;
                _reader = new ClientReader(stream);
            }

            internal void Close()
            {
                if (_process != null)
                {
                    Debug.Assert(_stream != null);

                    try
                    {
                        _stream.WriteAsync(new byte[] { 0 }, 0, 1);
                    }
                    catch
                    {
                        // Signal to server we are done with the connection.  Okay to fail because
                        // it means the server isn't listening anymore.
                    }

                    _stream.Close();

                    try
                    {
                        _process.Kill();
                    }
                    catch 
                    {
                        // Inherent race condition shutting down the process.
                    }
                }
            }

            void IDisposable.Dispose()
            {
                Close();
            }
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

        private DiscoverSession Discover(Dispatcher dispatcher, string assemblyPath, CancellationToken cancellationToken)
        {
            var connection = StartWorkerProcess(Constants.ActionDiscover, assemblyPath);
            return new DiscoverSession(connection, dispatcher, cancellationToken);
        }

        private RunSession Run(Dispatcher dispatcher, string assemblyPath, CancellationToken cancellationToken)
        {
            var connection = StartWorkerProcess(Constants.ActionRun, assemblyPath);
            return new RunSession(connection, dispatcher, cancellationToken);
        }

        #region ITestUtil

        ITestDiscoverSession ITestUtil.Discover(Dispatcher dispatcher, string assemblyPath, CancellationToken cancellationToken)
        {
            return Discover(dispatcher, assemblyPath, cancellationToken);
        }

        ITestRunSession ITestUtil.Run(Dispatcher dispatcher, string assemblyPath, CancellationToken cancellationToken)
        {
            return Run(dispatcher, assemblyPath, cancellationToken);
        }

        #endregion
    }
}
