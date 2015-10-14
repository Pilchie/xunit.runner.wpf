using GalaSoft.MvvmLight;
using System.IO;

namespace xunit.runner.wpf.ViewModel
{
    public class TestAssemblyViewModel : ViewModelBase
    {
        private readonly AssemblyAndConfigFile _assembly;
        private bool _isSelected;
        private AssemblyState _state;

        public TestAssemblyViewModel(AssemblyAndConfigFile assembly)
        {
            _assembly = assembly;
        }

        public string FileName => _assembly.AssemblyFileName;
        public string ConfigFileName => Path.GetFileNameWithoutExtension(_assembly.ConfigFileName);
        public string DisplayName => Path.GetFileNameWithoutExtension(_assembly.AssemblyFileName);

        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value, nameof(IsSelected)); }
        }

        public AssemblyState State
        {
            get { return _state; }
            set { Set(ref _state, value, nameof(State)); }
        }
    }

    public enum AssemblyState
    {
        Ready,
        Loading
    }
}
