using GalaSoft.MvvmLight;
using System.Windows.Input;
using System;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace xunit.runner.wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ObservableCollection<TestCaseViewModel> allTestCases = new ObservableCollection<TestCaseViewModel>();
        private CancellationTokenSource filterCancellationTokenSource = new CancellationTokenSource();

        private bool isBusy;
        private bool isCancelRequested;
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(@"C:\Code\TestAssembly.dll"));
            }

            CommandBindings = CreateCommandBindings();
            this.MethodsCaption = "Methods (0)";

            TestCases = new FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>>(
                allTestCases, TestCaseMatches, Tuple.Create(string.Empty, TestState.All), TestComparer.Instance);

            this.TestCases.CollectionChanged += TestCases_CollectionChanged;
            this.RunCommand = new RelayCommand(OnExecuteRun, CanExecuteRun);
            this.CancelCommand = new RelayCommand(OnExecuteCancel, CanExecuteCancel);
        }

        private static bool TestCaseMatches(TestCaseViewModel testCase, Tuple<string, TestState> filterTextAndTestState)
            => testCase.DisplayName.Contains(filterTextAndTestState.Item1) && (testCase.State & filterTextAndTestState.Item2) == filterTextAndTestState.Item2;

        private void TestCases_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MethodsCaption = $"Methods ({TestCases.Count})";
            MaximumProgress = TestCases.Count;
        }

        public ICommand ExitCommand { get; } = new RelayCommand(OnExecuteExit);
        public RelayCommand RunCommand { get; }
        public RelayCommand CancelCommand { get; }

        public CommandBindingCollection CommandBindings { get; }

        private string methodsCaption;
        public string MethodsCaption
        {
            get { return methodsCaption; }
            private set { Set(ref methodsCaption, value); }
        }

        private int testsCompleted = 0;
        public int TestsCompleted
        {
            get { return testsCompleted; }
            set { Set(ref testsCompleted, value); }
        }

        private int testsPassed = 0;
        public int TestsPassed
        {
            get { return testsPassed; }
            set { Set(ref testsPassed, value); }
        }

        private int testsFailed = 0;
        public int TestsFailed
        {
            get { return testsFailed; }
            set { Set(ref testsFailed, value); }
        }

        private int testsSkipped = 0;
        public int TestsSkipped
        {
            get { return testsSkipped; }
            set { Set(ref testsSkipped, value); }
        }

        private int maximumProgress = int.MaxValue;
        public int MaximumProgress
        {
            get { return maximumProgress; }
            set { Set(ref maximumProgress, value); }
        }

        private TestState currentRunState;
        public TestState CurrentRunState
        {
            get { return currentRunState; }
            set { Set(ref currentRunState, value); }
        }

        private string searchQuery = string.Empty;
        public string SearchQuery
        {
            get { return searchQuery; }
            set
            {
                if (Set(ref searchQuery, value))
                {
                    FilterAfterDelay();
                }
            }
        }

        private TestState resultFilter = TestState.All;
        public TestState ResultFilter
        {
            get { return resultFilter; }
            set
            {
                if (Set(ref resultFilter, value))
                {
                    this.FilterAfterDelay();
                }
            }
        }

        private void FilterAfterDelay()
        {
            filterCancellationTokenSource.Cancel();
            filterCancellationTokenSource = new CancellationTokenSource();
            var token = filterCancellationTokenSource.Token;

            Task
                .Delay(TimeSpan.FromMilliseconds(500), token)
                .ContinueWith(
                    x =>
                    {
                        TestCases.FilterArgument = Tuple.Create(SearchQuery, ResultFilter);
                    },
                    token,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private CommandBindingCollection CreateCommandBindings()
        {
            var openBinding = new CommandBinding(ApplicationCommands.Open, OnExecuteOpen);
            CommandManager.RegisterClassCommandBinding(typeof(MainViewModel), openBinding);

            return new CommandBindingCollection
            {
                openBinding,
            };
        }

        public ObservableCollection<TestAssemblyViewModel> Assemblies { get; } = new ObservableCollection<TestAssemblyViewModel>();
        public FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>> TestCases { get; }

        private void OnExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Unit Test Assemblies|*.dll",
            };

            if (fileDialog.ShowDialog(Application.Current.MainWindow) != true)
            {
                return;
            }

            var fileName = fileDialog.FileName;

            try
            {
                using (AssemblyHelper.SubscribeResolve())
                {
                    var xunit = new XunitFrontController(
                        useAppDomain: true,
                        assemblyFileName: fileName,
                        shadowCopy: false);

                    using (var testDiscoveryVisitor = new TestDiscoveryVisitor(xunit))
                    {
                        xunit.Find(includeSourceInformation: false, messageSink: testDiscoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                        testDiscoveryVisitor.Finished.WaitOne();
                        allTestCases.AddRange(testDiscoveryVisitor.TestCases);
                    }
                }

                Assemblies.Add(new TestAssemblyViewModel(fileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.ToString());
            }
        }

        private class TestDiscoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
        {
            ITestFrameworkDiscoverer discoverer;

            public List<TestCaseViewModel> TestCases { get; } = new List<TestCaseViewModel>();

            public TestDiscoveryVisitor(ITestFrameworkDiscoverer discoverer)
            {
                this.discoverer = discoverer;
            }

            public IDictionary<string, IList<string>> Traits { get; } = new Dictionary<string, IList<string>>();

            protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                TestCases.Add(new TestCaseViewModel(discoverer.Serialize(testCase), testCase.DisplayName, testCaseDiscovered.TestAssembly.Assembly.AssemblyPath));

                foreach (var k in testCase.Traits.Keys)
                {
                    IList<string> value;
                    if (!Traits.TryGetValue(k, out value))
                    {
                        value = new List<string>();
                        Traits[k] = value;
                    }

                    value.AddRange(testCase.Traits[k]);
                }

                return true;
            }
        }

        private class TestRunVisitor : TestMessageVisitor<ITestAssemblyFinished>
        {
            private readonly Func<bool> isCancelRequested;
            private readonly IEnumerable<TestCaseViewModel> testCases;

            public event EventHandler<TestStateEventArgs> TestFinished;

            public TestRunVisitor(IEnumerable<TestCaseViewModel> testCases, Func<bool> isCancelRequested)
            {
                this.testCases = testCases;
                this.isCancelRequested = isCancelRequested;
            }

            protected override bool Visit(ITestFailed testFailed)
            {
                var testCase = testCases.Single(tc => tc.DisplayName == testFailed.TestCase.DisplayName);
                testCase.State = TestState.Failed;
                TestFinished?.Invoke(this, TestStateEventArgs.Failed);
                return !isCancelRequested();
            }

            protected override bool Visit(ITestPassed testPassed)
            {
                var testCase = testCases.Single(tc => tc.DisplayName == testPassed.TestCase.DisplayName);
                testCase.State = TestState.Passed;
                TestFinished?.Invoke(this, TestStateEventArgs.Passed);
                return !isCancelRequested();
            }

            protected override bool Visit(ITestSkipped testSkipped)
            {
                var testCase = testCases.Single(tc => tc.DisplayName == testSkipped.TestCase.DisplayName);
                testCase.State = TestState.Skipped;
                TestFinished?.Invoke(this, TestStateEventArgs.Skipped);
                return !isCancelRequested();
            }
        }

        private bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RunCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        private bool IsCancelRequested
        {
            get { return isCancelRequested; }
            set
            {
                isCancelRequested = value;
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        private static void OnExecuteExit()
        {
            Application.Current.Shutdown();
        }

        private bool CanExecuteRun()
            => !IsBusy && TestCases.Any();

        private async void OnExecuteRun()
        {
            try
            {
                IsBusy = true;
                testsCompleted = 0;
                CurrentRunState = TestState.NotRun;
                await Task.Run(() => RunTestsInBackground());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                IsBusy = false;
                IsCancelRequested = false;
            }
        }

        private void RunTestsInBackground()
        {
            foreach (var tc in TestCases)
            {
                tc.State = TestState.NotRun;
            }

            var selectedAssemblies = TestCases.ToLookup(tc => tc.AssemblyFileName);
            using (AssemblyHelper.SubscribeResolve())
            {
                foreach (var assembly in selectedAssemblies)
                {
                    var xunit = new XunitFrontController(
                        assemblyFileName: assembly.Key,
                        useAppDomain: true,
                        shadowCopy: false);

                    using (var testRunVisitor = new TestRunVisitor(allTestCases, () => IsCancelRequested))
                    {
                        testRunVisitor.TestFinished += TestRunVisitor_TestFinished;
                        xunit.RunTests(assembly.Select(tcvm => xunit.Deserialize(tcvm.TestCase)).ToArray(), testRunVisitor, TestFrameworkOptions.ForExecution());
                        testRunVisitor.Finished.WaitOne();
                    }
                }
            }
        }

        private void TestRunVisitor_TestFinished(object sender, TestStateEventArgs e)
        {
            TestsCompleted++;
            switch (e.State)
            {
                case TestState.Passed:
                    TestsPassed++;
                    break;
                case TestState.Failed:
                    TestsFailed++;
                    break;
                case TestState.Skipped:
                    TestsSkipped++;
                    break;
            }

            if (e.State > CurrentRunState)
            {
                CurrentRunState = e.State;
            }
        }

        private bool CanExecuteCancel() => IsBusy && !IsCancelRequested;

        private void OnExecuteCancel()
        {
            this.IsCancelRequested = true;
        }

        public bool IsPassedFilterChecked
        {
            get { return ResultFilter == TestState.Passed; }
            set { UpdateFilter(value, TestState.Passed); }
        }

        public bool IsFailedFilterChecked
        {
            get { return ResultFilter == TestState.Failed; }
            set { UpdateFilter(value, TestState.Failed); }
        }

        public bool IsSkippedFilterChecked
        {
            get { return ResultFilter == TestState.Skipped; }
            set { UpdateFilter(value, TestState.Skipped); }
        }

        private void UpdateFilter(bool value, TestState newState)
        {
            if (value && ResultFilter != newState)
            {
                ResultFilter = newState;
                RaisePropertyChanged(nameof(IsPassedFilterChecked));
                RaisePropertyChanged(nameof(IsFailedFilterChecked));
                RaisePropertyChanged(nameof(IsSkippedFilterChecked));
            }
        }
    }

    /// <summary>
    /// Note: More severe states are higher numbers.
    /// <see cref="MainViewModel.TestRunVisitor_TestFinished(object, TestStateEventArgs)"/>
    /// </summary>
    public enum TestState
    {
        All = 0,
        NotRun,
        Passed,
        Skipped,
        Failed,
    }

    public class TestComparer : IComparer<TestCaseViewModel>
    {
        public static TestComparer Instance { get; } = new TestComparer();

        public int Compare(TestCaseViewModel x, TestCaseViewModel y)
            => StringComparer.Ordinal.Compare(x.DisplayName, y.DisplayName);

        private TestComparer() { }
    }
}