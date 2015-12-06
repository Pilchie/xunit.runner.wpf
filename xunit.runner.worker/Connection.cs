using System;
using System.IO;
using System.IO.Pipes;

namespace Xunit.Runner.Worker
{
    internal abstract class Connection : IDisposable
    {
        private bool _closed;

        internal abstract Stream Stream { get; }

        internal abstract void WaitForClientConnect();
        internal abstract void WaitForClientDone();

        protected virtual void DisposeCore()
        {

        }

        internal void Dispose()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            DisposeCore();
        }

        #region IDisposable

        void IDisposable.Dispose()
        {
            Dispose();
        }

        #endregion
    }

    internal sealed class NamedPipeConnection : Connection
    {
        private readonly NamedPipeServerStream _stream;

        internal override Stream Stream => _stream;

        internal NamedPipeConnection(string pipeName)
        {
            _stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
        }

        protected override void DisposeCore()
        {
            _stream.Dispose();
        }

        internal override void WaitForClientConnect()
        {
            _stream.WaitForConnection();
        }

        internal override void WaitForClientDone()
        {
            try
            {
                _stream.ReadByte();
            }
            catch (Exception ex)
            {
                // If there is an error reading from the client then clearly they are done
                Console.WriteLine($"Error reading client done byte {ex.Message}");
            }
        }
    }

    internal sealed class TestConnection : Connection
    {
        private readonly MemoryStream _stream = new MemoryStream();

        internal override Stream Stream => _stream;

        internal override void WaitForClientConnect()
        {

        }

        internal override void WaitForClientDone()
        {

        }
    }
}
