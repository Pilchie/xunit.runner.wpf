using System;
using System.Collections.Generic;

namespace Xunit.Runner.Wpf
{
    internal interface ITestAssemblyWatcher
    {
        /// <summary>
        /// Adds a new assembly to list of assemblies to be autoreloaded.
        /// </summary>
        void AddAssembly(string assemblyFileName);

        /// <summary>
        /// Removes an assembly from the list of assemblies ot be autoreloaded.
        /// </summary>
        void RemoveAssembly(string assemblyFileName);

        /// <summary>
        /// Enables watching of all assemblies.
        /// </summary>
        /// <param name="reloader">Action to perform when a file change is detected</param>
        void EnableWatch(Func<IEnumerable<string>, bool> reloader);

        /// <summary>
        /// Disables watching of all assemblies
        /// </summary>
        void DisableWatch();
    }
}
