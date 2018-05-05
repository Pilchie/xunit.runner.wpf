using System;
using System.ComponentModel;
using System.Windows;
using Xunit.Runner.Wpf.Persistence;

namespace Xunit.Runner.Wpf
{
    using ViewModel;

    public partial class MainWindow : Window
    {
        public static Window Instance { get; private set; }

        // WPF generates fields that are marked as non-nullable, but not definitely initialized.
#pragma warning disable CS8618
        public MainWindow()
        {
            Instance = this;

            InitializeComponent();
        }
#pragma warning restore

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Storage.RestoreWindowLayout(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Storage.SaveWindowLayout(this);

            base.OnClosing(e);
        }

        private void TestCases_SelectionChanged(Object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                var model = item as TestCaseViewModel;
                if (model != null)
                {
                    model.IsSelected = true;
                }
            }

            foreach (var item in e.RemovedItems)
            {
                var model = item as TestCaseViewModel;
                if (model != null)
                {
                    model.IsSelected = false;
                }
            }
        }
    }
}
