using System;
using System.IO;
using Xunit.Runner.Data;

namespace Xunit.Runner.Worker
{
    internal abstract class XunitUtil
    {
        protected static void Go(string assemblyFileName, Stream stream, AppDomainSupport appDomainSupport,
            Action<XunitFrontController, TestAssemblyConfiguration, ClientWriter> action)
        {
            using (AssemblyHelper.SubscribeResolve())
            using (var xunit = new XunitFrontController(appDomainSupport, assemblyFileName, shadowCopy: false))
            using (var writer = new ClientWriter(stream))
            {
                var configuration = ConfigReader.Load(assemblyFileName);
                action(xunit, configuration, writer);
            }
        }
    }
}
