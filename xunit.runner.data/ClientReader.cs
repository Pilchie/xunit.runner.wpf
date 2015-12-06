using System;
using System.IO;

namespace Xunit.Runner.Data
{
    public sealed class ClientReader : IDisposable
    {
        private readonly BinaryReader _reader;
        private bool _closed;
        private Exception _exception;

        public bool IsConnected => !_closed;

        public ClientReader(Stream stream)
        {
            _reader = new BinaryReader(stream, Constants.Encoding, leaveOpen: true);
        }

        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            _reader.Dispose();
        }

        public TestDataKind ReadKind()
        {
            return (TestDataKind)ReadCore(() => _reader.ReadInt32());
        }

        public TestCaseData ReadTestCaseData()
        {
            return ReadCore(() => TestCaseData.ReadFrom(_reader));
        }

        public TestResultData ReadTestResultData()
        {
            return ReadCore(() => TestResultData.ReadFrom(_reader));
        }

        public string ReadString()
        {
            return ReadCore(() => _reader.ReadString());
        }

        private T ReadCore<T>(Func<T> func)
        {
            if (_closed)
            {
                if (_exception == null)
                {
                    throw new Exception("Connection is closed");
                }

                throw new Exception("Connection is closed", _exception);
            }

            try
            {
                return func();
            }
            catch (Exception ex)
            {
                // Happens during rude shut down of the client.  Log to the screen and close 
                // the connection.
                Console.WriteLine(ex.Message);
                _exception = ex;
                Close();
                throw;
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
