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
        private sealed class BackgroundReader<T> where T : class
        {
            private readonly ConcurrentQueue<T> _queue;
            private readonly ClientReader _reader;
            private readonly Func<ClientReader, T> _readValue;
            private readonly CancellationToken _cancellationToken;

            internal ClientReader Reader => _reader;

            private BackgroundReader(ConcurrentQueue<T> queue, ClientReader reader, Func<ClientReader, T> readValue, CancellationToken cancellationToken)
            {
                _queue = queue;
                _reader = reader;
                _readValue = readValue;
                _cancellationToken = cancellationToken;
            }

            internal static void Go(ConcurrentQueue<T> queue, ClientReader reader, Func<ClientReader, T> readValue, CancellationToken cancellationToken)
            {
                var impl = new BackgroundReader<T>(queue, reader, readValue, cancellationToken);
                Task.Run(impl.GoOnBackground);
            }

            /// <summary>
            /// This will be called on a background thread to read the results of the test from the 
            /// named pipe client stream.
            /// </summary>
            /// <returns></returns>
            internal Task GoOnBackground()
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var kind = _reader.ReadKind();
                        if (kind != TestDataKind.Value)
                        {
                            break;
                        }

                        var value = _readValue(_reader);
                        _queue.Enqueue(value);
                    }
                    catch
                    {
                        // TODO: Happens when the connection unexpectedly closes on us.  Need to surface this
                        // to the user.
                        break;
                    }
                }

                // Signal we are done 
                _queue.Enqueue(null);

                return Task.FromResult(true);
            }
        }

        private sealed class BackgroundProducer<T> where T : class
        {
            private const int MaxResultPerTick = 1000;

            private readonly Connection _connection;
            private readonly ConcurrentQueue<T> _queue;
            private readonly DispatcherTimer _timer;
            private readonly Action<List<T>> _callback;
            private readonly int _maxPerTick;
            private readonly TaskCompletionSource<bool> _taskCompletionSource;

            internal Task Task => _taskCompletionSource.Task;

            private BackgroundProducer(
                Connection connection, 
                Dispatcher dispatcher, 
                Func<ClientReader, T> readValue,
                Action<List<T>> callback,
                int maxResultPerTick = MaxResultPerTick,
                TimeSpan? interval = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                _connection = connection;
                _queue = new ConcurrentQueue<T>();
                _maxPerTick = maxResultPerTick;
                _callback = callback;
                _timer = new DispatcherTimer(
                    interval ?? TimeSpan.FromMilliseconds(100), 
                    DispatcherPriority.Normal, 
                    OnTimerTick, 
                    dispatcher);
                _taskCompletionSource = new TaskCompletionSource<bool>();

                BackgroundReader<T>.Go(_queue, connection.Reader, readValue, cancellationToken);
            }

            internal static Task Go(
                Connection connection,
                Dispatcher dispatcher,
                Func<ClientReader, T> readValue,
                Action<List<T>> callback,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var producer = new BackgroundProducer<T>(connection, dispatcher, readValue, callback, cancellationToken: cancellationToken);
                return producer.Task;
            }

            private void OnTimerTick(object sender, EventArgs e)
            {
                var i = 0;
                var list = new List<T>();
                var isDone = false;
                T value;
                while (i < _maxPerTick && _queue.TryDequeue(out value))
                {
                    if (value == null)
                    {
                        isDone = true;
                        break;
                    }

                    list.Add(value);
                }

                if (list.Count > 0)
                {
                    _callback(list);
                }

                if (isDone)
                {
                    try
                    {
                        _callback(null);
                        _timer.Stop();
                        _connection.Close();
                    }
                    finally
                    {
                        _taskCompletionSource.SetResult(true);
                    }
                }
            }
        }
    }
}
