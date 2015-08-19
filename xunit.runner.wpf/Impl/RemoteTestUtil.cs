using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf.Impl
{
    internal sealed class RemoteTestUtil : ITestUtil
    {
        private sealed class Connection : IDisposable
        {
            private NamedPipeClientStream _stream;
            private Process _process;

            internal NamedPipeClientStream Stream => _stream;

            internal Connection(NamedPipeClientStream stream, Process process)
            {
                _stream = stream;
                _process = process;
            }

            void IDisposable.Dispose()
            {
                if (_process != null)
                {
                    Debug.Assert(_stream != null);

                    _stream.Close();

                    try
                    {
                        _process.Kill();
                    }
                    catch 
                    {
                        // Inherent race condition shutting down the process.
                    }
                }
            }
        }

        private Connection StartWorkerProcess(string action, string argument)
        {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = typeof(xunit.runner.worker.Program).Assembly.Location;
            processStartInfo.Arguments = $"{action} {argument}";
            var process = Process.Start(processStartInfo);
            try
            {
                var stream = new NamedPipeClientStream(Constants.PipeName);
                stream.Connect();
                return new Connection(stream, process);
            }
            catch
            {
                process.Kill();
                throw;
            }
        }

        private List<TestCaseViewModel> Discover(string assemblyPath)
        {
            var list = DiscoverCore(assemblyPath);
            return list
                .Select(x => new TestCaseViewModel(x.SerializedForm, x.DisplayName, x.AssemblyPath))
                .ToList();
        }

        private List<TestCaseData> DiscoverCore(string assemblyPath)
        {
            var list = new List<TestCaseData>();

            using (var connection = StartWorkerProcess(Constants.ActionDiscover, assemblyPath))
            using (var reader = new BinaryReader(connection.Stream, Encoding.UTF8, leaveOpen: true))
            {
                try
                {
                    while (true)
                    {
                        var testCaseData = TestCaseData.ReadFrom(reader);
                        list.Add(testCaseData);
                    }
                }
                catch
                {
                    // Hacky way of catching end of stream
                }
            }

            return list;
        }

        #region ITestUtil

        List<TestCaseViewModel> ITestUtil.Discover(string assemblyPath)
        {
            return Discover(assemblyPath);
        }

        #endregion
    }
}
