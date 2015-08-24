using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.data;

namespace xunit.runner.wpf.ViewModel
{
    public class TestCaseViewModel : ViewModelBase
    {
        private TestState _state = TestState.NotRun;

        public TestCaseViewModel(string testCase, string displayName, string assemblyFileName, ImmutableArray<TraitViewModel> traits)
        {
            this.TestCase = testCase;
            this.DisplayName = displayName;
            this.AssemblyFileName = assemblyFileName;
            this.Traits = traits;
        }

        public string DisplayName { get; }

        public TestState State
        {
            get { return _state; }
            set { Set(ref _state, value); }
        }

        public string AssemblyFileName { get; }

        public string TestCase { get; }

        public ImmutableArray<TraitViewModel> Traits { get; }
    }
}
