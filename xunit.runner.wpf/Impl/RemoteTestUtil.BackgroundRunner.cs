using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit.Runner.Data;

namespace Xunit.Runner.Wpf.Impl
{
    internal partial class RemoteTestUtil 
    {
        private sealed class BackgroundWriter<T>
        {
            private readonly ClientWriter _writer;
            private readonly ImmutableArray<T> _data;
            private readonly Action<ClientWriter, T> _writeValue;
            private readonly CancellationToken _cancellationToken;

            internal BackgroundWriter(ClientWriter writer, ImmutableArray<T> data, Action<ClientWriter, T> writeValue, CancellationToken cancellationToken)
            {
                _writer = writer;
                _writeValue = writeValue;
                _data = data;
                _cancellationToken = cancellationToken;
            }

            internal Task WriteAsync()
            {
                return Task.Run(() => GoOnBackground(), _cancellationToken);
            }

            private void GoOnBackground()
            {
                foreach (var item in _data)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    _writer.Write(TestDataKind.Value);
                    _writeValue(_writer, item);
                }

                _writer.Write(TestDataKind.EndOfData);
            }
        }

        /// <summary>
        /// Utility for reading a collection of <see cref="{T}"/> values from the given 
        /// <see cref="ClientReader"/> value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class BackgroundReader<T> where T : class
        {
            private readonly ConcurrentQueue<T?> _queue;
            private readonly ClientReader _reader;
            private readonly Func<ClientReader, T> _readValue;

            internal ClientReader Reader => _reader;

            internal BackgroundReader(ConcurrentQueue<T?> queue, ClientReader reader, Func<ClientReader, T> readValue)
            {
                _queue = queue;
                _reader = reader;
                _readValue = readValue;
            }

            internal Task ReadAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.Run(() => GoOnBackground(cancellationToken), cancellationToken);
            }

            /// <summary>
            /// This will be called on a background thread to read the results of the test from the 
            /// named pipe client stream.
            /// </summary>
            /// <returns></returns>
            private void GoOnBackground(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
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
            }
        }

        private sealed class BackgroundProducer<T> where T : class
        {
            private const int MaxResultPerTick = 1000;

            private readonly Connection _connection;
            private readonly ConcurrentQueue<T?> _queue;
            private readonly DispatcherTimer _timer;
            private readonly Action<List<T>> _callback;
            private readonly int _maxPerTick;
            private readonly TaskCompletionSource<bool> _taskCompletionSource;

            internal Task Task => _taskCompletionSource.Task;

            internal BackgroundProducer(
                Connection connection, 
                Dispatcher dispatcher, 
                ConcurrentQueue<T?> queue,
                Action<List<T>> callback,
                int maxResultPerTick = MaxResultPerTick,
                TimeSpan? interval = null)
            {
                _connection = connection;
                _queue = queue;
                _maxPerTick = maxResultPerTick;
                _callback = callback;
                _timer = new DispatcherTimer(
                    interval ?? TimeSpan.FromMilliseconds(500), 
                    DispatcherPriority.Normal, 
                    OnTimerTick, 
                    dispatcher);
                _taskCompletionSource = new TaskCompletionSource<bool>();
            }

            private void OnTimerTick(object sender, EventArgs e)
            {
                var i = 0;
                var list = new List<T>();
                var isDone = false;
                while (i < _maxPerTick && _queue.TryDequeue(out T? value))
                {
                    if (value == null)
                    {
                        isDone = true;
                        break;
                    }

                    list.Add(value);
                }

                _callback(list);

                if (isDone)
                {
                    try
                    {
                        _timer.Stop();
                        _connection.Dispose();
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
