using System.IO;

namespace Xunit.Runner.Wpf.ViewModel
{
    public class AssemblyAndConfigFile
    {
        public string AssemblyFileName { get; }
        public string? ConfigFileName { get; }

        public AssemblyAndConfigFile(string assemblyFileName, string? configFileName)
        {
            this.AssemblyFileName = Path.GetFullPath(assemblyFileName);
            if (configFileName != null)
            {
                this.ConfigFileName = Path.GetFullPath(configFileName);
            }
        }
    }
}