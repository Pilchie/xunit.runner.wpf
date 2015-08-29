using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.wpf.ViewModel
{
    public sealed class TraitViewModel : ViewModelBase
    {
        private bool _isSelected;

        public string Name { get; }
        public string Value { get; }
        public string DisplayName { get; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        public TraitViewModel(string name, string value)
        {
            Name = name;
            Value = value;
            DisplayName = $"{Name}={Value}";
        }
    }
}
