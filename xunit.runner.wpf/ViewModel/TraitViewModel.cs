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
        private readonly string _name;
        private readonly string _value;
        private bool _isSelected;

        public string Name => _name;
        public string Value => _value;
        public string DisplayName => $"{Name}={Value}";

        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value, nameof(IsSelected)); }
        }

        public TraitViewModel(string name, string value)
        {
            _name = name;
            _value = value;
        }
    }
}
