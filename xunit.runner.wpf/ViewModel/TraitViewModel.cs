using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace Xunit.Runner.Wpf.ViewModel
{
    public partial class TraitViewModel : ViewModelBase
    {
        private readonly TraitViewModel _parent;
        private bool? _isChecked;
        private bool _isExpanded;
        private string _text;

        public ObservableCollection<TraitViewModel> Children { get; }

        public TraitViewModel(string text)
            : this(null, text)
        {
        }

        private TraitViewModel(TraitViewModel parent, string text)
        {
            this._parent = parent;
            this._isChecked = null;
            this._isExpanded = true;
            this._text = text;
            this.Children = new ObservableCollection<TraitViewModel>();
        }

        private void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == this._isChecked)
            {
                return;
            }

            this._isChecked = value;

            if (updateChildren )
            {
                foreach (var child in this.Children)
                {
                    child.SetIsChecked(value, updateChildren: true, updateParent: false);
                }
            }

            if (updateParent && _parent != null)
            {
                _parent.VerifyCheckState();
            }

            this.RaisePropertyChanged(nameof(IsChecked));
        }

        private void VerifyCheckState()
        {
            bool? state = null;
            var isFirst = true;

            foreach (var child in this.Children)
            {
                if (isFirst)
                {
                    state = child.IsChecked;
                    isFirst = false;
                }
                else if (state != child.IsChecked)
                {
                    state = null;
                    break;
                }
            }

            this.SetIsChecked(state, updateChildren: false, updateParent: true);
        }

        public void AddValues(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                var index = this.Children.BinarySearch(value, StringComparer.Ordinal.Compare, v => v.Text);
                if (index < 0)
                {
                    this.Children.Insert(~index, new TraitViewModel(this, value));
                }
            }
        }

        public TraitViewModel GetOrAdd(string text)
        {
            var index = this.Children.BinarySearch(text, StringComparer.Ordinal, vm => vm.Text);

            if (index < 0)
            {
                var viewModel = new TraitViewModel(this, text);
                this.Children.Insert(~index, viewModel);
                return viewModel;
            }

            return this.Children[index];
        }

        public bool? IsChecked
        {
            get { return _isChecked; }
            set { SetIsChecked(value, updateChildren: true, updateParent: true); }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { Set(ref _isExpanded, value); }
        }

        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value); }
        }
    }
}
