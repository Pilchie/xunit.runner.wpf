using GalaSoft.MvvmLight;
using System.Windows.Input;
using System;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;

namespace xunit.runner.wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

        }

        public ICommand ExitCommand { get; } = new RelayCommand(OnExecuteExit);

        public CommandBindingCollection CommandBindings { get; }
            = CreateCommandBindings();

        private static CommandBindingCollection CreateCommandBindings()
        {
            var openBinding = new CommandBinding(ApplicationCommands.Open, OnExecuteOpen);
            CommandManager.RegisterClassCommandBinding(typeof(MainViewModel), openBinding);

            return new CommandBindingCollection
            {
                openBinding,
            };
        }

        private static void OnExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Open clicked");
        }

        private static void OnExecuteExit()
        {
            Application.Current.Shutdown();
        }
    }
}