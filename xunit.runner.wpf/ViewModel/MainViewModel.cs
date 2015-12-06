using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Taskbar;
using Xunit.Runner.Data;

namespace Xunit.Runner.Wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ITestUtil testUtil;
        private readonly ObservableCollection<TestCaseViewModel> allTestCases = new ObservableCollection<TestCaseViewModel>();
        private readonly TraitCollectionView traitCollectionView = new TraitCollectionView();
        private CancellationTokenSource filterCancellationTokenSource = new CancellationTokenSource();

        private CancellationTokenSource cancellationTokenSource;
        private bool isBusy;
        private SearchQuery searchQuery = new SearchQuery();

        public ObservableCollection<TestAssemblyViewModel> Assemblies { get; } = new ObservableCollection<TestAssemblyViewModel>();
        public FilteredCollectionView<TestCaseViewModel, SearchQuery> FilteredTestCases { get; }
        public ObservableCollection<TraitViewModel> Traits => this.traitCollectionView.Collection;

        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(new AssemblyAndConfigFile(@"C:\Code\TestAssembly.dll", null)));
            }

            CommandBindings = CreateCommandBindings();
            this.testUtil = new Xunit.Runner.Wpf.Impl.RemoteTestUtil(Dispatcher.CurrentDispatcher);
            this.TestCasesCaption = "Test Cases (0)";

            this.FilteredTestCases = new FilteredCollectionView<TestCaseViewModel, SearchQuery>(
                allTestCases, TestCaseMatches, searchQuery, TestComparer.Instance);

            this.FilteredTestCases.CollectionChanged += TestCases_CollectionChanged;
            this.WindowLoadedCommand = new RelayCommand(OnExecuteWindowLoaded);
            this.RunCommand = new RelayCommand(OnExecuteRun, CanExecuteRun);
            this.CancelCommand = new RelayCommand(OnExecuteCancel, CanExecuteCancel);
            this.TraitCheckedChangedCommand = new RelayCommand<TraitViewModel>(OnExecuteTraitCheckedChanged);
            this.TraitsClearCommand = new RelayCommand(OnExecuteTraitsClear);
            this.AssemblyReloadCommand = new RelayCommand(OnExecuteAssemblyReload, CanExecuteAssemblyReload);
            this.AssemblyReloadAllCommand = new RelayCommand(OnExecuteAssemblyReloadAll);
            this.AssemblyRemoveCommand = new RelayCommand(OnExecuteAssemblyRemove, CanExecuteAssemblyRemove);
            this.AssemblyRemoveAllCommand = new RelayCommand(OnExecuteAssemblyRemoveAll);
        }

        private static bool TestCaseMatches(TestCaseViewModel testCase, SearchQuery searchQuery)
        {
            if (testCase.DisplayName.IndexOf(searchQuery.SearchString, StringComparison.CurrentCultureIgnoreCase) < 0)
            {
                return false;
            }

            if (searchQuery.TraitSet.Count > 0)
            {
                var anyMatch = false;
                foreach (var cur in testCase.Traits)
                {
                    if (searchQuery.TraitSet.Contains(cur))
                    {
                        anyMatch = true;
                        break;
                    }
                }

                if (!anyMatch)
                {
                    return false;
                }
            }

            var noFilter = !(searchQuery.FilterFailedTests | searchQuery.FilterPassedTests | searchQuery.FilterSkippedTests);

            switch (testCase.State)
            {
                case TestState.Passed:
                    return noFilter || searchQuery.FilterPassedTests;

                case TestState.Skipped:
                    return noFilter || searchQuery.FilterSkippedTests;

                case TestState.Failed:
                    return noFilter || searchQuery.FilterFailedTests;

                case TestState.NotRun:
                    return noFilter;

                default:
                    Debug.Assert(false, "What state is this test case in?");
                    return true;
            }
        }

        private void TestCases_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TestCasesCaption = $"Test Cases ({FilteredTestCases.Count:#,0})";
            MaximumProgress = FilteredTestCases.Count;
        }

        public ICommand ExitCommand { get; } = new RelayCommand(OnExecuteExit);
        public ICommand WindowLoadedCommand { get; }
        public RelayCommand RunCommand { get; }
        public RelayCommand CancelCommand { get; }
        public ICommand TraitCheckedChangedCommand { get; }
        public ICommand TraitSelectionChangedCommand { get; }
        public ICommand TraitsClearCommand { get; }
        public ICommand AssemblyReloadCommand { get; }
        public ICommand AssemblyReloadAllCommand { get; }
        public ICommand AssemblyRemoveCommand { get; }
        public ICommand AssemblyRemoveAllCommand { get; }

        public CommandBindingCollection CommandBindings { get; }

        public List<TestAssemblyViewModel> SelectedAssemblies
        {
            get { return Assemblies.Where(x => x.IsSelected).ToList(); }
        }

        private string testCasesCaption;
        public string TestCasesCaption
        {
            get { return testCasesCaption; }
            private set { Set(ref testCasesCaption, value); }
        }

        private int testsCompleted = 0;
        public int TestsCompleted
        {
            get { return testsCompleted; }
            set { Set(ref testsCompleted, value);

                if (TaskbarManager.IsPlatformSupported)
                {
                    var tb = TaskbarManager.Instance;
                    tb.SetProgressState(GetTaskBarState());
                    tb.SetProgressValue(value, this.MaximumProgress);
                }
            }
        }

        private TaskbarProgressBarState GetTaskBarState()
        {
            switch (this.CurrentRunState)
            {
                case TestState.Failed:
                    return TaskbarProgressBarState.Error;
                case TestState.Skipped:
                    return TaskbarProgressBarState.Paused;
                default:
                    return TaskbarProgressBarState.Normal;
            }
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

        private string output = string.Empty;
        public string Output
        {
            get { return output; }
            set { Set(ref output, value); }
        }

        public string FilterString
        {
            get { return searchQuery.SearchString; }
            set
            {
                if (Set(ref searchQuery.SearchString, value))
                {
                    FilterAfterDelay();
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
                        FilteredTestCases.FilterArgument = searchQuery;
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

        private async void OnExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Unit Test Assemblies|*.dll",
                Multiselect = true
            };

            if (fileDialog.ShowDialog(Application.Current.MainWindow) != true)
            {
                return;
            }

            var assemblies = fileDialog.FileNames.Select(x => new AssemblyAndConfigFile(x, configFileName: null));
            await AddAssemblies(assemblies);
        }

        private async Task AddAssemblies(IEnumerable<AssemblyAndConfigFile> assemblies)
        {
            if (!assemblies.Any())
            {
                return;
            }

            var newAssemblyViewModels = new List<TestAssemblyViewModel>();

            try
            {
                await this.ExecuteTestSessionOperation(() =>
                {
                    var taskList = new List<Task>();
                    foreach (var assembly in assemblies)
                    {
                        taskList.Add(this.testUtil.Discover(assembly.AssemblyFileName, this.OnTestsDiscovered, this.cancellationTokenSource.Token));

                        var assemblyViewModel = new TestAssemblyViewModel(assembly);

                        newAssemblyViewModels.Add(assemblyViewModel);
                        this.Assemblies.Add(assemblyViewModel);

                        assemblyViewModel.State = AssemblyState.Loading;
                    }

                    return taskList;
                });
            }
            finally
            {
                foreach (var assemblyViewModel in newAssemblyViewModels)
                {
                    assemblyViewModel.State = AssemblyState.Ready;
                }
            }
        }

        private async Task ReloadAssemblies(IEnumerable<TestAssemblyViewModel> assemblies)
        {
            try
            {
                await ExecuteTestSessionOperation(() =>
                {
                    var taskList = new List<Task>();
                    foreach (var assemblyViewModel in assemblies)
                    {
                        assemblyViewModel.State = AssemblyState.Loading;

                        var assemblyFileName = assemblyViewModel.FileName;
                        RemoveAssemblyTestCases(assemblyFileName);

                        taskList.Add(this.testUtil.Discover(assemblyFileName, OnTestsDiscovered, cancellationTokenSource.Token));
                    }

                    return taskList;
                });

                RebuildTraits();
            }
            finally
            {
                foreach (var assemblyViewModel in assemblies)
                {
                    assemblyViewModel.State = AssemblyState.Ready;
                }
            }
        }

        private void RemoveAssemblies(IEnumerable<TestAssemblyViewModel> assemblies)
        {
            foreach (var assembly in assemblies.ToList())
            {
                RemoveAssemblyTestCases(assembly.FileName);
                Assemblies.Remove(assembly);
            }

            RebuildTraits();
        }

        private void RemoveAssemblyTestCases(string assemblyPath)
        {
            var i = 0;
            while (i < this.allTestCases.Count)
            {
                if (string.Compare(this.allTestCases[i].AssemblyFileName, assemblyPath, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.allTestCases.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Reloading an assembly could have changed the traits.  There is no easy way 
        /// to selectively edit this list (traits can cross assembly boundaries).  Just 
        /// do a full reload instead.
        /// way to 
        /// </summary>
        private void RebuildTraits()
        {
            this.traitCollectionView.Collection.Clear();
            foreach (var testCase in this.allTestCases)
            {
                this.traitCollectionView.AddRange(testCase.Traits);
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

        private static void OnExecuteExit()
        {
            Application.Current.Shutdown();
        }

        private async void OnExecuteWindowLoaded()
        {
            await AddAssemblies(ParseCommandLine(Environment.GetCommandLineArgs().Skip(1)));
        }

        private IEnumerable<AssemblyAndConfigFile> ParseCommandLine(IEnumerable<string> enumerable)
        {
            while (enumerable.Any())
            {
                var assemblyFileName = enumerable.First();
                enumerable = enumerable.Skip(1);

                var configFileName = (string)null;
                if (IsConfigFile(enumerable.FirstOrDefault()))
                {
                    configFileName = enumerable.First();
                    enumerable = enumerable.Skip(1);
                }

                yield return new AssemblyAndConfigFile(assemblyFileName, configFileName);
            }
        }

        private bool IsConfigFile(string fileName)
            => (fileName?.EndsWith(".config", StringComparison.OrdinalIgnoreCase) ?? false) ||
               (fileName?.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ?? false);

        private bool CanExecuteRun()
            => !IsBusy && FilteredTestCases.Any();

        private async void OnExecuteRun()
        {
            await ExecuteTestSessionOperation(RunTests);
        }

        private List<Task> RunTests()
        {
            Debug.Assert(this.isBusy);
            Debug.Assert(this.cancellationTokenSource != null);

            TestsCompleted = 0;
            TestsPassed = 0;
            TestsFailed = 0;
            TestsSkipped = 0;
            CurrentRunState = TestState.NotRun;
            Output = string.Empty;

            foreach (var tc in FilteredTestCases)
            {
                tc.State = TestState.NotRun;
            }

            var runAll = FilteredTestCases.Count == this.allTestCases.Count;
            var testSessionList = new List<Task>();

            foreach (var assemblyFileName in FilteredTestCases.Select(x => x.AssemblyFileName).Distinct())
            {
                Task task;
                if (runAll)
                {
                    task = this.testUtil.RunAll(assemblyFileName, OnTestsFinished, this.cancellationTokenSource.Token);
                }
                else
                {
                    var builder = ImmutableArray.CreateBuilder<string>();

                    foreach (var testCase in FilteredTestCases)
                    {
                        if (testCase.AssemblyFileName == assemblyFileName)
                        {
                            builder.Add(testCase.DisplayName);
                        }
                    }

                    task = this.testUtil.RunSpecific(assemblyFileName, builder.ToImmutable(), OnTestsFinished, this.cancellationTokenSource.Token);
                }

                testSessionList.Add(task);
            }

            return testSessionList;
        }

        private async Task ExecuteTestSessionOperation(Func<List<Task>> operation)
        {
            Debug.Assert(!this.IsBusy);
            Debug.Assert(this.cancellationTokenSource == null);

            try
            {
                this.IsBusy = true;
                this.cancellationTokenSource = new CancellationTokenSource();

                var taskList = operation();
                await Task.WhenAll(taskList);
            }
            catch (Exception ex)
            {
                this.cancellationTokenSource?.Cancel();
                MessageBox.Show(Application.Current.MainWindow, ex.ToString());
            }
            finally
            {
                this.cancellationTokenSource = null;
                this.IsBusy = false;
            }
        }

        private void OnTestsDiscovered(IEnumerable<TestCaseData> testCases)
        {
            var traitWorkerList = new List<TraitViewModel>();

            foreach (var testCase in testCases)
            {
                traitWorkerList.Clear();

                // Get or create traits.
                if (testCase.TraitMap?.Count > 0)
                {
                    foreach (var kvp in testCase.TraitMap)
                    {
                        var name = kvp.Key;
                        var values = kvp.Value;

                        var parentTraitViewModel = traitCollectionView.GetOrAdd(name);

                        foreach (var value in values)
                        {
                            var traitViewModel = parentTraitViewModel.GetOrAdd(value);
                            traitWorkerList.Add(traitViewModel);
                        }
                    }
                }

                var testCaseViewModel = new TestCaseViewModel(
                    testCase.DisplayName,
                    testCase.SkipReason,
                    testCase.AssemblyPath,
                    traitWorkerList);

                if (testCaseViewModel.State == TestState.Skipped)
                {
                    TestsSkipped++;
                }

                this.allTestCases.Add(testCaseViewModel);
            }
        }

        private void OnTestsFinished(IEnumerable<TestResultData> testResultData)
        {
            foreach (var data in testResultData)
            {
                OnTestFinished(data);
            }
        }

        private void OnTestFinished(TestResultData testResultData)
        {
            var testCase = FilteredTestCases.Single(x => x.DisplayName == testResultData.TestCaseDisplayName);
            testCase.State = testResultData.TestState;

            TestsCompleted++;
            switch (testResultData.TestState)
            {
                case TestState.Passed:
                    TestsPassed++;
                    break;
                case TestState.Failed:
                    TestsFailed++;
                    Output = Output + testResultData.Output;
                    break;
                case TestState.Skipped:
                    TestsSkipped++;
                    break;
            }

            if (testResultData.TestState > CurrentRunState)
            {
                CurrentRunState = testResultData.TestState;
            }
        }

        private bool CanExecuteCancel()
        {
            return this.cancellationTokenSource != null && !this.cancellationTokenSource.IsCancellationRequested;
        }

        private void OnExecuteCancel()
        {
            Debug.Assert(CanExecuteCancel());
            this.cancellationTokenSource.Cancel();
        }

        private void OnExecuteTraitCheckedChanged(TraitViewModel trait)
        {
            this.searchQuery.TraitSet = this.traitCollectionView.GetCheckedTraits();
            FilterAfterDelay();
        }

        private void OnExecuteTraitsClear()
        {
            foreach (var cur in this.traitCollectionView.Collection)
            {
                cur.IsChecked = false;
            }
        }

        private bool CanExecuteAssemblyReload()
        {
            return SelectedAssemblies.Count > 0;
        }

        private async void OnExecuteAssemblyReload()
        {
            await ReloadAssemblies(SelectedAssemblies);
        }

        private async void OnExecuteAssemblyReloadAll()
        {
            await ReloadAssemblies(Assemblies);
        }

        private bool CanExecuteAssemblyRemove()
        {
            return SelectedAssemblies.Count > 0;
        }

        private void OnExecuteAssemblyRemove()
        {
            RemoveAssemblies(SelectedAssemblies);
        }

        private void OnExecuteAssemblyRemoveAll()
        {
            RemoveAssemblies(Assemblies.ToArray());
        }

        public bool FilterPassedTests
        {
            get { return searchQuery.FilterPassedTests; }
            set
            {
                if (Set(ref searchQuery.FilterPassedTests, value))
                {
                    FilterAfterDelay();
                }
            }
        }

        public bool FilterFailedTests
        {
            get { return searchQuery.FilterFailedTests; }
            set
            {
                if (Set(ref searchQuery.FilterFailedTests, value))
                {
                    FilterAfterDelay();
                }
            }
        }

        public bool FilterSkippedTests
        {
            get { return searchQuery.FilterSkippedTests; }
            set
            {
                if (Set(ref searchQuery.FilterSkippedTests, value))
                {
                    FilterAfterDelay();
                }
            }
        }
    }

    public class TestComparer : IComparer<TestCaseViewModel>
    {
        public static TestComparer Instance { get; } = new TestComparer();

        public int Compare(TestCaseViewModel x, TestCaseViewModel y)
            => StringComparer.Ordinal.Compare(x.DisplayName, y.DisplayName);

        private TestComparer() { }
    }
}
