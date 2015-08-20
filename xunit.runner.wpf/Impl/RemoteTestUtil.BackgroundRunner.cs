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
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf.Impl
{
    internal partial class RemoteTestUtil 
    {
        private sealed class BackgroundRunner
        {
            private readonly ConcurrentQueue<TestResultData> _resultQueue;
            private readonly ClientReader _reader;
            private readonly CancellationToken _cancellationToken;

            internal BackgroundRunner(ConcurrentQueue<TestResultData> resultQueue, ClientReader reader, CancellationToken cancellationToken)
            {
                _resultQueue = resultQueue;
                _reader = reader;
                _cancellationToken = cancellationToken;
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

                        var result = _reader.ReadTestResultData();
                        _resultQueue.Enqueue(result);
                    }
                    catch
                    {
                        // TODO: Happens when the connection unexpectedly closes on us.  Need to surface this
                        // to the user.
                        break;
                    }
                }

                // Signal we are done 
                _resultQueue.Enqueue(null);

                return Task.FromResult(true);
            }
        }
    }
}
