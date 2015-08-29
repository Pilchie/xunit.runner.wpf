using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.data;

namespace xunit.runner.worker
{
    internal sealed class Listener
    {
        private readonly string _pipeName;

        internal Listener(string pipeName)
        {
            _pipeName = pipeName;
        }

        internal void Go()
        {
            bool success;
            do
            {
                success = GoOne();
            } while (success);
        }

        private bool GoOne()
        {
            try
            {
                var namedPipe = new NamedPipeServerStream(_pipeName);
                namedPipe.WaitForConnection();
                Console.WriteLine("Connection established processing");
                ProcessConnection(namedPipe);
                Console.WriteLine("Connection completed");
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
            Debug.Assert(stream.IsConnected);

            try
            {
                var reader = new ClientReader(stream);
                var action = reader.ReadString();
                var argument = reader.ReadString();
                switch (action)
                {
                    case Constants.ActionDiscover:
                        Discover(stream, argument);
                        break;
                    case Constants.ActionRunAll:
                        RunAll(stream, argument);
                        break;
                    case Constants.ActionRunSpecific:
                        RunSpecific(stream, argument);
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

        private static void Discover(Stream stream, string assemblyPath)
        {
            Console.WriteLine($"discover started: {assemblyPath}");
            DiscoverUtil.Go(assemblyPath, stream);
            Console.WriteLine("discover ended");
        }

        private static void RunAll(Stream stream, string assemblyPath)
        {
            Console.WriteLine($"run all started: {assemblyPath}");
            RunUtil.RunAll(assemblyPath, stream);
            Console.WriteLine("run all ended");
        }

        private static void RunSpecific(Stream stream, string assemblyPath)
        {
            Console.WriteLine($"run specific started: {assemblyPath}");
            RunUtil.RunSpecific(assemblyPath, stream);
            Console.WriteLine("run specific ended");
        }
    }
}
