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
    internal partial class RemoteTestUtil 
    {
        private sealed class DiscoverSession : ITestDiscoverSession
        {
            private readonly Task _task;
            private event EventHandler<TestCaseDataEventArgs> _testDiscovered;
            private event EventHandler _sessionFinished;

            internal DiscoverSession(Connection connection, Dispatcher dispatcher, CancellationToken cancellationToken)
            {
                _task = BackgroundProducer<TestCaseData>.Go(connection, dispatcher, r => r.ReadTestCaseData(), OnDiscovered, cancellationToken);
            }

            private void OnDiscovered(List<TestCaseData> list)
            {
                Debug.Assert(!_task.IsCompleted);

                if (list == null)
                {
                    _sessionFinished?.Invoke(this, EventArgs.Empty);
                    return;
                }

                foreach (var cur in list)
                {
                    _testDiscovered?.Invoke(this, new TestCaseDataEventArgs(cur));
                }
            }

            #region ITestRunSession

            Task ITestSession.Task => _task;

            event EventHandler<TestCaseDataEventArgs> ITestDiscoverSession.TestDiscovered
            {
                add { _testDiscovered += value; }
                remove { _testDiscovered -= value; }
            }

            event EventHandler ITestDiscoverSession.SessionFinished
            {
                add { _sessionFinished += value; }
                remove { _sessionFinished -= value; }
            }

            #endregion
        }
    }
}
