using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Xunit.Runner.Wpf.Impl
{
    internal sealed class TestAssemblyWatcher : ITestAssemblyWatcher
    {
        private readonly object sync = new object();
        private readonly IDictionary<string, FileSystemWatcher> watchedAssemblies = new Dictionary<string, FileSystemWatcher>();
        private readonly Dispatcher dispatcher;
        private bool isEnabled = false;
        private ReloadDebouncer debouncer;

        public TestAssemblyWatcher(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public void AddAssembly(string assemblyFileName)
        {
            // Assumptions about adding and removing assemblies are broken if this isn't true
            Debug.Assert(string.Equals(assemblyFileName, Path.GetFullPath(assemblyFileName), StringComparison.Ordinal));

            lock (sync)
            {
                if (watchedAssemblies.ContainsKey(assemblyFileName))
                {
                    // Already watching this assembly, nothing to do but return
                    return;
                }

                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(assemblyFileName),
                    NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                    Filter = Path.GetFileName(assemblyFileName)
                };

                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.Created += new FileSystemEventHandler(OnChanged);

                watchedAssemblies[assemblyFileName] = watcher;

                if (isEnabled)
                {
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        public void RemoveAssembly(string assemblyFileName)
        {
            lock (sync)
            {
                if (watchedAssemblies.ContainsKey(assemblyFileName))
                {
                    watchedAssemblies[assemblyFileName].Dispose();
                    watchedAssemblies.Remove(assemblyFileName);
                }
            }
        }

        public void EnableWatch(Func<IEnumerable<string>, bool> reloader)
        {
            lock (sync)
            {
                isEnabled = true;

                foreach (var watcher in watchedAssemblies.Values)
                {
                    watcher.EnableRaisingEvents = true;
                }

                this.debouncer = new ReloadDebouncer(dispatcher, reloader);
            }
        }

        public void DisableWatch()
        {
            lock (sync)
            {
                isEnabled = false;

                foreach (var watcher in watchedAssemblies.Values)
                {
                    watcher.EnableRaisingEvents = false;
                }

                this.debouncer?.Cancel();
                this.debouncer = null;
            }
        }

        private void OnChanged(object source, FileSystemEventArgs args)
        {
            debouncer?.AddAssembly(args.FullPath);
        }

        /// <summary>
        /// Because, during a build of a number of projects many file system events will be triggered for potentially many
        /// test assemblies, we need to batch our update requests. This class will do this, waiting for 100 ms after receiving
        /// a new reload request to send the reload requests. This timer resets every time a reload request is received. Note
        /// that if you continuously rebuild, this will technicially never finish batching and nothing will reload, but this
        /// assumes that file events will stop at some point.
        ///
        /// If the reloader returns false, meaning that the reload was not kicked off successfully, we back off for a full second
        /// before reattempting to queue the updates.
        /// </summary>
        private class ReloadDebouncer
        {
            private readonly object sync = new object();
            private readonly Dispatcher dispatcher;
            private readonly Func<IEnumerable<string>, bool> reloader;

            private ISet<string> assembliesToReload = new HashSet<string>();
            private bool newAssemblyAdded = false;
            private bool running = false;
            private bool cancelled = false;

            public ReloadDebouncer(Dispatcher dispatcher, Func<IEnumerable<string>, bool> reloader)
            {
                this.dispatcher = dispatcher;
                this.reloader = reloader;
            }

            public void AddAssembly(string assembly)
            {
                lock (sync)
                {
                    assembliesToReload.Add(assembly);

                    if (!Start())
                    {
                        newAssemblyAdded = true;
                    }
                }
            }

            public void Cancel()
            {
                running = false;
            }

            private bool Start()
            {
                if (running)
                {
                    return false;
                }

                running = true;
                Task.Run((Action)Debounce);
                return true;
            }

            private async void Debounce()
            {
                bool backOff = false;

                do
                {
                    await Task.Delay(backOff ? 1000 : 100);
                    backOff = false;

                    lock (sync)
                    {
                        void Reset()
                        {
                            assembliesToReload = new HashSet<string>();
                            running = false;
                        }

                        if (cancelled)
                        {
                            Reset();
                            return;
                        }

                        // New assemblies added, so we need to wait again
                        if (newAssemblyAdded)
                        {
                            newAssemblyAdded = false;
                            continue;
                        }

                        // No new assemblies added, time to alert and exit
                        if (!dispatcher.Invoke(() => reloader(assembliesToReload)))
                        {
                            // If the reloader returned false, it's still busy from the last reload request or other user action.
                            // Back off for a full second to give it time, then continue as previous
                            backOff = true;
                            continue;
                        }
                        Reset();
                    }

                } while (running);
            }
        }
    }
}
