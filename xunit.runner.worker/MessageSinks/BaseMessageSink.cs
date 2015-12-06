using System;
using Xunit.Abstractions;

namespace Xunit.Runner.Worker.MessageSinks
{
    /// <summary>
    /// An Xunit <see cref="IMessageSink"/> implementation without the dispatch overhead of <see cref="TestMessageVisitor"/>
    /// and <see cref="TestMessageVisitor{TCompleteMessage}"/>.
    /// </summary>
    internal abstract class BaseMessageSink : LongLivedMarshalByRefObject, IMessageSink, IDisposable
    {
        private bool _disposed;

        protected BaseMessageSink()
        {
        }

        ~BaseMessageSink()
        {
            Dispose(false);
        }

        protected virtual void DisposeCore(bool disposing)
        {
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            DisposeCore(disposing);

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        protected abstract bool OnMessage(IMessageSinkMessage message);

        bool IMessageSink.OnMessage(IMessageSinkMessage message)
        {
            return OnMessage(message);
        }
    }
}
