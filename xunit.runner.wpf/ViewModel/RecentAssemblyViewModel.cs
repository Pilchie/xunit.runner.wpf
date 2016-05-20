using System.Windows.Input;

namespace Xunit.Runner.Wpf.ViewModel
{
    public class RecentAssemblyViewModel
    {
        public string FilePath { get; }
        public ICommand Command { get; }

        public RecentAssemblyViewModel(string filePath, ICommand command)
        {
            this.FilePath = filePath;
            this.Command = command;
        }
    }
}
