using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.worker
{
    internal abstract class Connection : IDisposable
    {
        private readonly Stream _stream;
        private bool _closed;

        internal Stream Stream => _stream;

        protected abstract void WaitForClientConnect();
        protected abstract void WaitForClientDone();

        protected virtual void DisposeCore()
        {

        }

        private void Dispose()
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
        internal NamedPipeConnection

    }
}
