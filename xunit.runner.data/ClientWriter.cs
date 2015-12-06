using System;
using System.IO;

namespace Xunit.Runner.Data
{
    public sealed class ClientWriter : IDisposable
    {
        private readonly BinaryWriter _writer;
        private bool _closed;

        public bool IsConnected => !_closed;

        public ClientWriter(Stream stream)
        {
            _writer = new BinaryWriter(stream, Constants.Encoding, leaveOpen: true);
        }

        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            _writer.Dispose();
        }

        public void Write(TestDataKind kind)
        {
            WriteCore(() => _writer.Write((int)kind));
        }

        public void Write(TestCaseData testCaseData)
        {
            WriteCore(() => testCaseData.WriteTo(_writer));
        }

        public void Write(TestResultData testCaseResultData)
        {
            WriteCore(() => testCaseResultData.WriteTo(_writer));
        }

        public void Write(string str)
        {
            WriteCore(() => _writer.Write(str));
        }

        private void WriteCore(Action action)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                // Happens during rude shut down of the client.  Log to the screen and close 
                // the connection.
                Console.WriteLine(ex.Message);
                Close();
            }
        }

        #region IDisposable

        void IDisposable.Dispose()
        {
            Close();
        }

        #endregion
    }
}
