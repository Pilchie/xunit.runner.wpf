using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private sealed class RunSession : ITestRunSession
        {
            private readonly Task _task;
            private event EventHandler<TestResultDataEventArgs> _testFinished;
            private event EventHandler _sessionFinished;

            internal RunSession(Connection connection, Dispatcher dispatcher, ImmutableArray<string> testCaseDisplayNames, CancellationToken cancellationToken)
            {
                var queue = CreateQueue(connection, testCaseDisplayNames, cancellationToken);
                var backgroundProducer = new BackgroundProducer<TestResultData>(connection, dispatcher, queue, OnDataProduced);
                _task = backgroundProducer.Task;
            }

            /// <summary>
            /// Create the <see cref="ConcurrentQueue{T}"/> which will be populated with the <see cref="TestResultData"/>
            /// as it arrives from the worker. 
            /// </summary>
            private static ConcurrentQueue<TestResultData> CreateQueue(Connection connection, ImmutableArray<string> testCaseDisplayNames, CancellationToken cancellationToken)
            {
                var queue = new ConcurrentQueue<TestResultData>();
                var unused = CreateQueueCore(queue, connection, testCaseDisplayNames, cancellationToken);
                return queue;
            }

            private static async Task CreateQueueCore(ConcurrentQueue<TestResultData> queue, Connection connection, ImmutableArray<string> testCaseDisplayNames, CancellationToken cancellationToken)
            {
                try
                {
                    if (!testCaseDisplayNames.IsDefaultOrEmpty)
                    {
                        var backgroundWriter = new BackgroundWriter<string>(new ClientWriter(connection.Stream), testCaseDisplayNames, (w, s) => w.Write(s), cancellationToken);
                        await backgroundWriter.WriteAsync();
                    }

                    var backgroundReader = new BackgroundReader<TestResultData>(queue, new ClientReader(connection.Stream), r => r.ReadTestResultData(), cancellationToken);
                    await backgroundReader.ReadAsync();
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.Message);

                    // Signal data completed
                    queue.Enqueue(null);
                }
            }

            private void OnDataProduced(List<TestResultData> list)
            {
                Debug.Assert(!_task.IsCompleted);
                if (list == null)
                {
                    _sessionFinished?.Invoke(this, EventArgs.Empty);
                    return;
                }

                foreach (var cur in list)
                {
                    _testFinished?.Invoke(this, new wpf.TestResultDataEventArgs(cur));
                }
            }

            #region ITestRunSession

            Task ITestSession.Task => _task;

            event EventHandler<TestResultDataEventArgs> ITestRunSession.TestFinished
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
