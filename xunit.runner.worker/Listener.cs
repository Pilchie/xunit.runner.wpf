using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit.Runner.Data;

namespace Xunit.Runner.Worker
{
    internal sealed class Listener
    {
        private readonly string _pipeName;
        private readonly List<Task> _taskList = new List<Task>();

        internal Listener(string pipeName)
        {
            _pipeName = pipeName;
        }

        internal void Go()
        {
            bool success;
            do
            {
                _taskList.RemoveAll(x => x.IsCompleted);
                success = GoOne();
            }
            while (success);

            // Wait for the existing tasks to complete before stopping the listener
            Task.WaitAll(_taskList.ToArray());
        }

        private bool GoOne()
        {
            try
            {
                var namedPipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, maxNumberOfServerInstances: -1);
                namedPipe.WaitForConnection();
                _taskList.Add(Task.Run(() => ProcessConnection(namedPipe)));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating named pipe {ex.Message}");
                return false;
            }
        }

        private static void ProcessConnection(NamedPipeServerStream stream)
        {
            Console.WriteLine("Connection established processing");
            ProcessConnectionCore(stream);
            Console.WriteLine("Connection completed");
        }

        private static void ProcessConnectionCore(NamedPipeServerStream stream)
        {
            Debug.Assert(stream.IsConnected);

            try
            {
                var reader = new ClientReader(stream);
                var action = reader.ReadString();
                var assemblyFileName = reader.ReadString();

                switch (action)
                {
                    case Constants.ActionDiscover:
                        Discover(stream, assemblyFileName);
                        break;
                    case Constants.ActionRunAll:
                        RunAll(stream, assemblyFileName);
                        break;
                    case Constants.ActionRunSpecific:
                        RunSpecific(stream, assemblyFileName);
                        break;
                    default:
                        Debug.Fail($"Invalid action {action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // Happens during a rude disconnect by the client
                Console.WriteLine(ex.Message);
            }
            finally
            {
                stream.Dispose();
            }
        }

        private static void Discover(Stream stream, string assemblyFileName)
        {
            Console.WriteLine($"discover started: {assemblyFileName}");
            DiscoverUtil.Go(assemblyFileName, stream);
            Console.WriteLine("discover ended");
        }

        private static void RunAll(Stream stream, string assemblyFileName)
        {
            Console.WriteLine($"run all started: {assemblyFileName}");
            RunUtil.RunAll(assemblyFileName, stream);
            Console.WriteLine("run all ended");
        }

        private static void RunSpecific(Stream stream, string assemblyFileName)
        {
            Console.WriteLine($"run specific started: {assemblyFileName}");
            RunUtil.RunSpecific(assemblyFileName, stream);
            Console.WriteLine("run specific ended");
        }
    }
}
