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

            internal void Dispose()
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
                Dispose();
            }
        }
    }
}
