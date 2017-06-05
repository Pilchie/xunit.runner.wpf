using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
using Xunit.Runner.Wpf.Persistence;

namespace Xunit.Runner.Wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Settings settings;

        private readonly ITestUtil testUtil;
        private readonly ITestAssemblyWatcher assemblyWatcher;
        private readonly HashSet<string> allTestCaseUniqueIDs = new HashSet<string>();
        private readonly ObservableCollection<TestCaseViewModel> allTestCases = new ObservableCollection<TestCaseViewModel>();
        private readonly TraitCollectionView traitCollectionView = new TraitCollectionView();
        private CancellationTokenSource filterCancellationTokenSource = new CancellationTokenSource();

        private CancellationTokenSource cancellationTokenSource;
        private bool isBusy;
        private SearchQuery searchQuery = new SearchQuery();
        private bool autoReloadAssemblies;

        public ObservableCollection<TestAssemblyViewModel> Assemblies { get; } = new ObservableCollection<TestAssemblyViewModel>();
        public FilteredCollectionView<TestCaseViewModel, SearchQuery> FilteredTestCases { get; }
        public ObservableCollection<TraitViewModel> Traits => this.traitCollectionView.Collection;
        public bool AutoReloadAssemblies
        {
            get => autoReloadAssemblies;
            set
            {
                var oldVal = autoReloadAssemblies;
                autoReloadAssemblies = value;
                RaisePropertyChanged(nameof(AutoReloadAssemblies), oldVal, autoReloadAssemblies);
            }
        }

        public ObservableCollection<RecentAssemblyViewModel> RecentAssemblies { get; } = new ObservableCollection<RecentAssemblyViewModel>();

        private ImmutableList<TestCaseViewModel> runningTests;

        public ICommand ExitCommand { get; }
        public ICommand WindowLoadedCommand { get; }
        public RelayCommand<CancelEventArgs> WindowClosingCommand { get; }
        public RelayCommand RunAllCommand { get; }
        public RelayCommand RunSelectedCommand { get; }
        public RelayCommand CancelCommand { get; }
        public ICommand TraitCheckedChangedCommand { get; }
        public ICommand TraitSelectionChangedCommand { get; }
        public ICommand TraitsClearCommand { get; }
        public ICommand AssemblyReloadCommand { get; }
        public ICommand AssemblyReloadAllCommand { get; }
        public ICommand AssemblyRemoveCommand { get; }
        public ICommand AssemblyRemoveAllCommand { get; }
        public ICommand AutoReloadAssembliesCommand { get; }

        public CommandBindingCollection CommandBindings { get; }

        public MainViewModel()
        {
            this.settings = Settings.Load();

            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(new AssemblyAndConfigFile(@"C:\Code\TestAssembly.dll", null)));
            }

            CommandBindings = CreateCommandBindings();

            this.testUtil = new Xunit.Runner.Wpf.Impl.RemoteTestUtil(Dispatcher.CurrentDispatcher);
            this.assemblyWatcher = new Impl.TestAssemblyWatcher(Dispatcher.CurrentDispatcher);
            this.TestCasesCaption = "Test Cases (0)";

            this.FilteredTestCases = new FilteredCollectionView<TestCaseViewModel, SearchQuery>(
                allTestCases, TestCaseMatches, searchQuery, TestComparer.Instance);

            this.FilteredTestCases.CollectionChanged += TestCases_CollectionChanged;

            this.ExitCommand = new RelayCommand(OnExecuteExit);
            this.WindowLoadedCommand = new RelayCommand(OnExecuteWindowLoaded);
            this.WindowClosingCommand = new RelayCommand<CancelEventArgs>(OnExecuteWindowClosing);
            this.RunAllCommand = new RelayCommand(OnExecuteRunAll, CanExecuteRunAll);
            this.RunSelectedCommand = new RelayCommand(OnExecuteRunSelected, CanExecuteRunSelected);
            this.CancelCommand = new RelayCommand(OnExecuteCancel, CanExecuteCancel);
            this.TraitCheckedChangedCommand = new RelayCommand<TraitViewModel>(OnExecuteTraitCheckedChanged);
            this.TraitsClearCommand = new RelayCommand(OnExecuteTraitsClear);
            this.AssemblyReloadCommand = new RelayCommand(OnExecuteAssemblyReload, CanExecuteAssemblyReload);
            this.AssemblyReloadAllCommand = new RelayCommand(OnExecuteAssemblyReloadAll);
            this.AssemblyRemoveCommand = new RelayCommand(OnExecuteAssemblyRemove, CanExecuteAssemblyRemove);
            this.AssemblyRemoveAllCommand = new RelayCommand(OnExecuteAssemblyRemoveAll);
            this.AutoReloadAssembliesCommand = new RelayCommand(OnToggleAutoReloadAssemblies);

            RebuildRecentAssembliesMenu();
            AutoReloadAssemblies = this.settings.GetAutoReloadAssemblies();
            UpdateAutoReloadStatus();
        }

        private void RebuildRecentAssembliesMenu()
        {
            this.RecentAssemblies.Clear();

            foreach (var recentAssembly in this.settings.GetRecentAssemblies())
            {
                var viewModel = new RecentAssemblyViewModel(recentAssembly, new RelayCommand<RecentAssemblyViewModel>(this.OnExecuteRecentAssembly));
                this.RecentAssemblies.Add(viewModel);
            }
        }

        private async void OnExecuteRecentAssembly(RecentAssemblyViewModel recentAssembly)
        {
            var assemblyAndConfig = new AssemblyAndConfigFile(recentAssembly.FilePath, configFileName: null);

            await this.AddAssemblies(new[] { assemblyAndConfig });
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
            UpdateTestCaseInfo(useSelected: false);
            ClearSelectionFlags();
        }

        private void ClearSelectionFlags()
        {
            foreach (var test in this.allTestCases)
            {
                test.IsSelected = false;
            }
        }

        void UpdateTestCaseInfo(bool useSelected)
        {
            var count = FilteredTestCases.Count;
            if (useSelected)
            {
                var selected = FilteredTestCases.Count(tc => tc.IsSelected);
                if (selected > 0)
                {
                    count = selected;
                }
            }

            TestCasesCaption = $"Test Cases ({count:#,0})";
            MaximumProgress = count;
        }

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

            set
            {
                Set(ref testsCompleted, value);
                UpdateProgress();
            }
        }

        private TestCaseViewModel selectedTest;
        public TestCaseViewModel SelectedTestCase
        {
            get { return selectedTest; }

            set
            {
                Set(ref selectedTest, value);
            }
        }

        private void UpdateProgress()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                var tb = TaskbarManager.Instance;
                tb.SetProgressState(GetTaskBarState());
                tb.SetProgressValue(this.TestsCompleted, this.MaximumProgress);
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

            set
            {
                Set(ref maximumProgress, value);
                UpdateProgress();
            }
        }

        private TestState currentRunState;
        public TestState CurrentRunState
        {
            get { return currentRunState; }

            set
            {
                Set(ref currentRunState, value);
                UpdateProgress();
            }
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
                        this.settings.AddRecentAssembly(assembly.AssemblyFileName);

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
                    assemblyWatcher.AddAssembly(assemblyViewModel.FileName);
                }

                RebuildRecentAssembliesMenu();
            }
        }

        public bool ReloadAssemblies(IEnumerable<string> assemblies)
        {
            if (IsBusy)
            {
                return false;
            }

            var testAssemblies = Assemblies.Where(assembly => assemblies.Contains(assembly.FileName));
            Application.Current.Dispatcher.InvokeAsync(() => ReloadAssemblies(testAssemblies));

            return true;
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
                assemblyWatcher.RemoveAssembly(assembly.FileName);
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
                    this.allTestCaseUniqueIDs.Remove(this.allTestCases[i].UniqueID);
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
                RunAllCommand.RaiseCanExecuteChanged();
                RunSelectedCommand.RaiseCanExecuteChanged();
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

        private void OnExecuteWindowClosing(CancelEventArgs e)
        {
            this.settings.Save();
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

        private bool CanExecuteRunAll()
            => !IsBusy && FilteredTestCases.Any();

        private bool CanExecuteRunSelected()
            => !IsBusy && SelectedTestCase != null;

        private async void OnExecuteRunAll()
        {
            Debug.Assert(this.runningTests == null);
            UpdateTestCaseInfo(useSelected: false);

            await ExecuteTestSessionOperation(RunFilteredTests);

            this.runningTests = null;
        }

        private List<Task> RunFilteredTests()
        {
            return RunTests(FilteredTestCases.ToImmutableList());
        }

        private async void OnExecuteRunSelected()
        {
            Debug.Assert(this.runningTests == null);
            Debug.Assert(this.SelectedTestCase != null);
            UpdateTestCaseInfo(useSelected: true);

            await ExecuteTestSessionOperation(RunSelectedTests);

            this.runningTests = null;
        }

        private List<Task> RunSelectedTests()
        {
            return RunTests(ImmutableList.CreateRange(this.FilteredTestCases.Where(tc => tc.IsSelected)));
        }

        private List<Task> RunTests(ImmutableList<TestCaseViewModel> tests)
        {
            Debug.Assert(this.isBusy);
            Debug.Assert(this.cancellationTokenSource != null);
            Debug.Assert(this.runningTests == null);

            TestsCompleted = 0;
            TestsPassed = 0;
            TestsFailed = 0;
            TestsSkipped = 0;
            CurrentRunState = TestState.NotRun;
            Output = string.Empty;

            this.runningTests = tests;

            foreach (var tc in this.runningTests)
            {
                tc.State = TestState.NotRun;
            }

            var runAll = this.runningTests.Count == this.allTestCases.Count;
            var testSessionList = new List<Task>();

            foreach (var assemblyFileName in this.runningTests.Select(x => x.AssemblyFileName).Distinct())
            {
                Task task;
                if (runAll)
                {
                    task = this.testUtil.RunAll(assemblyFileName, OnTestsFinished, this.cancellationTokenSource.Token);
                }
                else
                {
                    var builder = ImmutableArray.CreateBuilder<string>();

                    foreach (var testCase in this.runningTests)
                    {
                        if (testCase.AssemblyFileName == assemblyFileName)
                        {
                            builder.Add(testCase.UniqueID);
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
                if (this.allTestCaseUniqueIDs.Contains(testCase.UniqueID))
                    continue;

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
                    testCase.UniqueID,
                    testCase.SkipReason,
                    testCase.AssemblyPath,
                    traitWorkerList);

                if (testCaseViewModel.State == TestState.Skipped)
                {
                    TestsSkipped++;
                }

                this.allTestCaseUniqueIDs.Add(testCase.UniqueID);
                this.allTestCases.Add(testCaseViewModel);
            }
        }

        private void OnTestsFinished(IEnumerable<TestResultData> testResultData)
        {
            Debug.Assert(this.runningTests != null);

            foreach (var result in testResultData)
            {
                var testCase = this.runningTests.Single(x => x.UniqueID == result.TestCaseUniqueID);
                testCase.State = result.TestState;

                TestsCompleted++;
                switch (result.TestState)
                {
                    case TestState.Passed:
                        TestsPassed++;
                        break;
                    case TestState.Failed:
                        TestsFailed++;
                        Output = Output + result.Output;
                        break;
                    case TestState.Skipped:
                        TestsSkipped++;
                        break;
                }

                if (result.TestState > CurrentRunState)
                {
                    CurrentRunState = result.TestState;
                }
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
        private void OnToggleAutoReloadAssemblies()
        {
            ToggleReloadAssemblies();
        }

        private void ToggleReloadAssemblies()
        {
            this.settings.ToggleAutoReloadAssemblies();
            AutoReloadAssemblies = this.settings.GetAutoReloadAssemblies();
            UpdateAutoReloadStatus();
        }

        private void UpdateAutoReloadStatus()
        {
            if (AutoReloadAssemblies)
            {
                assemblyWatcher.EnableWatch(ReloadAssemblies);
            }
            else
            {
                assemblyWatcher.DisableWatch();
            }
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
        {
            int result = StringComparer.OrdinalIgnoreCase.Compare(x.DisplayName, y.DisplayName);
            if (result != 0)
                return result;

            result = StringComparer.Ordinal.Compare(x.DisplayName, y.DisplayName);
            if (result != 0)
                return result;

            return StringComparer.Ordinal.Compare(x.UniqueID, y.UniqueID);
        }

        private TestComparer() { }
    }
}
