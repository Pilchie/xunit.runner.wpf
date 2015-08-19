using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf.Impl
{
    internal partial class RemoteTestUtil 
    {
        private sealed class RunSession : ITestRunSession
        {
            private const int MaxResultPerTick = 100;

            private readonly Connection _connection;
            private readonly ConcurrentQueue<TestResultData> _resultQueue;
            private readonly DispatcherTimer _timer;
            private bool _closed;
            private event EventHandler<TestResultEventArgs> _testFinished;
            private event EventHandler _sessionFinished;

            internal RunSession(Connection connection, Dispatcher dispatcher, ConcurrentQueue<TestResultData> resultQueue)
            {
                _connection = connection;
                _resultQueue = resultQueue;
                _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, OnTimerTick, dispatcher);
            }

            private void OnTimerTick(object sender, EventArgs e)
            {
                var i = 0;
                TestResultData data;
                while (i < MaxResultPerTick && _resultQueue.TryDequeue(out data))
                {
                    if (data == null)
                    {
                        Close();
                        break;
                    }

                    _testFinished?.Invoke(this, new TestResultEventArgs(data.TestCaseDisplayName, data.TestState));
                }
            }

            internal void Close()
            {
                if (_closed)
                {
                    return;
                }

                _closed = true;
                _timer.Stop();
                ((IDisposable)_connection).Dispose();

                _sessionFinished?.Invoke(this, EventArgs.Empty);
            }

            #region ITestRunSession

            bool ITestRunSession.IsRunning => !_closed;

            event EventHandler<TestResultEventArgs> ITestRunSession.TestFinished
            {
                add { _testFinished += value; }
                remove { _testFinished -= value; }
            }

            event EventHandler ITestRunSession.SessionFinished
            {
                add { _sessionFinished += value; }
                remove { _sessionFinished -= value; }
            }

            #endregion
        }
    }
}
