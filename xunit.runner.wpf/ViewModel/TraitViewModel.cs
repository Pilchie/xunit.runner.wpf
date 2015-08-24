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
        public readonly string _name;
        public readonly string _value;

        public string Name => _name;
        public string Value => _value;
        public string DisplayName => $"{Name}={Value}";

        public TraitViewModel(string name, string value)
        {
            _name = name;
            _value = value;
        }
    }
}
