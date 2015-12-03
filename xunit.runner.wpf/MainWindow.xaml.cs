using System;
using System.ComponentModel;
using System.Windows;

namespace xunit.runner.wpf
{
    public partial class MainWindow : Window
    {
        public static Window Instance { get; private set; }

        public MainWindow()
        {
            Instance = this;

            InitializeComponent();
        }

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
    }
}
