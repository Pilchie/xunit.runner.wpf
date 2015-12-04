using GalaSoft.MvvmLight;
using System.Collections.Immutable;
using xunit.runner.data;
using System.Collections.Generic;

namespace xunit.runner.wpf.ViewModel
{
    public class TestCaseViewModel : ViewModelBase
    {
        private TestState _state = TestState.NotRun;

        public TestCaseViewModel(string displayName, string assemblyFileName, IEnumerable<TraitViewModel> traits)
        {
            this.DisplayName = displayName;
            this.AssemblyFileName = assemblyFileName;
            this.Traits = traits.ToImmutableArray();
        }

        public string DisplayName { get; }

        public TestState State
        {
            get { return _state; }
            set { Set(ref _state, value); }
        }

        public string AssemblyFileName { get; }

        public ImmutableArray<TraitViewModel> Traits { get; }
    }
}
