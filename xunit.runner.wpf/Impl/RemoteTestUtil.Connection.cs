using System;
using System.IO.Pipes;
using Xunit.Runner.Data;

namespace Xunit.Runner.Wpf.Impl
{
    internal sealed partial class RemoteTestUtil : ITestUtil
    {
        private sealed class Connection : IDisposable
        {
            private NamedPipeClientStream? _stream;
            private ClientReader _reader;

            internal NamedPipeClientStream Stream => _stream ?? throw new ObjectDisposedException(nameof(Connection));

            internal ClientReader Reader => _reader;

            internal Connection(NamedPipeClientStream stream)
            {
                _stream = stream;
                _reader = new ClientReader(stream);
            }

            internal void Dispose()
            {
                if (_stream == null)
                {
                    return;
                }

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
                _stream = null;
            }

            void IDisposable.Dispose()
            {
                Dispose();
            }
        }
    }
}
