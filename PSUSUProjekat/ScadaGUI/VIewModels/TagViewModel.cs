using System.Windows.Media;
using DataConcentrator;

namespace ScadaGUI.ViewModels
{
    public class TagViewModel : BaseViewModel
    {
        private Tag _tag;
        private double _currentValue;
        private AlarmState _alarmState = AlarmState.Inactive;

        public Tag Tag { get { return _tag; } }
        public string TagName { get { return _tag.Name; } }
        public TagType TagType { get { return _tag.TagType; } }
        public string Description { get { return _tag.Description; } }
        public string IOAddress { get { return _tag.IOAddress; } }
        public int? ScanTime { get { return _tag.ScanTime; } }
        public bool? ScanOn { get { return _tag.ScanOn; } }
        public double? LowLimit { get { return _tag.LowLimit; } }
        public double? HighLimit { get { return _tag.HighLimit; } }
        public string Units { get { return _tag.Units; } }

        public double CurrentValue
        {
            get { return _currentValue; }
            set { _currentValue = value; OnPropertyChanged(); OnPropertyChanged("DisplayValue"); }
        }

        public string DisplayValue
        {
            get
            {
                if (_tag.TagType == TagType.DI || _tag.TagType == TagType.DO)
                    return _currentValue > 0.5 ? "ON" : "OFF";
                return string.Format("{0:F2} {1}", _currentValue, _tag.Units);
            }
        }

        public AlarmState AlarmState
        {
            get { return _alarmState; }
            set { _alarmState = value; OnPropertyChanged(); OnPropertyChanged("AlarmColor"); }
        }

        public Brush AlarmColor
        {
            get
            {
                if (_alarmState == AlarmState.Active)
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                if (_alarmState == AlarmState.Acknowledged)
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7));
                return new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
        }

        public string ScanStateText { get { return _tag.ScanOn == true ? "ON" : "OFF"; } }
        public Brush ScanStateColor
        {
            get
            {
                return _tag.ScanOn == true
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
        }

        public TagViewModel(Tag tag)
        {
            _tag = tag;
            _currentValue = tag.CurrentValue;
        }
    }
}
