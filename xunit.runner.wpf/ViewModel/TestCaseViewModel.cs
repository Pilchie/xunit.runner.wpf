using System.Collections.Generic;
using System.Collections.Immutable;
using GalaSoft.MvvmLight;
using Xunit.Runner.Data;

namespace Xunit.Runner.Wpf.ViewModel
{
    public class TestCaseViewModel : ViewModelBase
    {
        private TestState _state = TestState.NotRun;

        public string DisplayName { get; }
        public string SkipReason { get; }
        public string AssemblyFileName { get; }
        public ImmutableArray<TraitViewModel> Traits { get; }
        public bool IsSelected { get; set; }

        public bool HasSkipReason => !string.IsNullOrEmpty(this.SkipReason);

        public TestState State
        {
            get { return _state; }
            set { Set(ref _state, value); }
        }

        public TestCaseViewModel(string displayName, string skipReason, string assemblyFileName, IEnumerable<TraitViewModel> traits)
        {
            this.DisplayName = displayName;
            this.SkipReason = skipReason;
            this.AssemblyFileName = assemblyFileName;
            this.Traits = traits.ToImmutableArray();

            if (!string.IsNullOrEmpty(skipReason))
            {
                _state = TestState.Skipped;
            }
        }
    }
}
