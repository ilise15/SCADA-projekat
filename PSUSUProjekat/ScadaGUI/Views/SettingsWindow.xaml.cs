using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DataConcentrator;
using ScadaGUI.ViewModels;

namespace ScadaGUI.Views
{
    public partial class SettingsWindow : Window
    {
        private MainViewModel _vm;

        public SettingsWindow(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            LoadTimezones();
            UpdateTraceLabel();

            var lang = LocalizationService.Instance.Language;
            LangBox.SelectedIndex = lang == AppLanguage.Serbian ? 0 : 1;
            DateFmtBox.SelectedIndex = (int)LocalizationService.Instance.DateFormat;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "TraceWord") UpdateTraceLabel();
            };
        }

        private void LoadTimezones()
        {
            var zones = TimeZoneInfo.GetSystemTimeZones().ToList();
            TzBox.ItemsSource = zones;
            TzBox.DisplayMemberPath = "DisplayName";
            TzBox.SelectedValuePath = "Id";
            TzBox.SelectedValue = LocalizationService.Instance.TimeZoneId;
        }

        private void UpdateTraceLabel()
        {
            if (TraceWordLabel != null)
                TraceWordLabel.Text = "TraceWord (numerički): " + _vm.TraceWord;
        }

        private void LangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LocalizationService.Instance.SetLanguage(
                LangBox.SelectedIndex == 0 ? AppLanguage.Serbian : AppLanguage.English);
        }

        private void DateFmt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LocalizationService.Instance.SetDateFormat((DateFormatType)DateFmtBox.SelectedIndex);
        }

        private void Tz_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tz = TzBox.SelectedItem as TimeZoneInfo;
            if (tz != null) LocalizationService.Instance.SetTimezone(tz.Id);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
