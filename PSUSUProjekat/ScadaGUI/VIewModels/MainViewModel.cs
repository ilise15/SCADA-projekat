using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
using DataConcentrator;
using Microsoft.Win32;

namespace ScadaGUI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DataConcentratorService _dc = DataConcentratorService.Instance;
        private LocalizationService _loc = LocalizationService.Instance;

        public ObservableCollection<TagViewModel> Tags { get; } = new ObservableCollection<TagViewModel>();
        public ObservableCollection<ActivatedAlarm> RecentAlarms { get; } = new ObservableCollection<ActivatedAlarm>();

        // Localization labels
        public string TagsLabel { get { return _loc.Get("Tags"); } }
        public string AlarmsLabel { get { return _loc.Get("Alarms"); } }
        public string AddTagLabel { get { return _loc.Get("Add") + " tag"; } }
        public string ReportLabel { get { return _loc.Get("Report"); } }
        public string FilterLabel { get { return _loc.Get("Filter"); } }
        public string ExportLabel { get { return _loc.Get("Export"); } }
        public string ImportLabel { get { return _loc.Get("Import"); } }
        public string SettingsLabel { get { return _loc.Get("Settings"); } }
        public string LogoutLabel { get { return _loc.Get("Logout"); } }

        public Visibility IsAdminVisible
        {
            get { return AuthService.Instance.IsAdmin ? Visibility.Visible : Visibility.Collapsed; }
        }

        // Settings
        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get { return _isDarkMode; }
            set
            {
                _isDarkMode = value;
                OnPropertyChanged();
                App.ApplyTheme(value);
                SystemLogger.Log(TraceFlags.TagUpdate, "Theme changed to " + (value ? "dark" : "light"));
            }
        }

        private double _alarmVolume = 0.5;
        public double AlarmVolume
        {
            get { return _alarmVolume; }
            set { _alarmVolume = value; OnPropertyChanged(); }
        }

        private bool _alarmSoundEnabled = true;
        public bool AlarmSoundEnabled
        {
            get { return _alarmSoundEnabled; }
            set { _alarmSoundEnabled = value; OnPropertyChanged(); }
        }

        private string _selectedSoundFile = "alarm1";
        public string SelectedSoundFile
        {
            get { return _selectedSoundFile; }
            set { _selectedSoundFile = value; OnPropertyChanged(); }
        }

        public string[] SoundOptions { get { return new[] { "alarm1", "alarm2", "alarm3" }; } }

        // Trace flags
        private int _traceWord;
        public int TraceWord
        {
            get { return _traceWord; }
            set
            {
                _traceWord = value;
                SystemLogger.ActiveFlags = (TraceFlags)value;
                OnPropertyChanged();
            }
        }

        public bool TraceLogin
        {
            get { return HasFlag(TraceFlags.Login); }
            set { SetFlag(TraceFlags.Login, value); OnPropertyChanged(); }
        }
        public bool TraceAlarmAck
        {
            get { return HasFlag(TraceFlags.AlarmAck); }
            set { SetFlag(TraceFlags.AlarmAck, value); OnPropertyChanged(); }
        }
        public bool TraceTagAdd
        {
            get { return HasFlag(TraceFlags.TagAdd); }
            set { SetFlag(TraceFlags.TagAdd, value); OnPropertyChanged(); }
        }
        public bool TraceTagUpdate
        {
            get { return HasFlag(TraceFlags.TagUpdate); }
            set { SetFlag(TraceFlags.TagUpdate, value); OnPropertyChanged(); }
        }
        public bool TraceImportExport
        {
            get { return HasFlag(TraceFlags.ImportExport); }
            set { SetFlag(TraceFlags.ImportExport, value); OnPropertyChanged(); }
        }
        public bool TraceError
        {
            get { return HasFlag(TraceFlags.Error); }
            set { SetFlag(TraceFlags.Error, value); OnPropertyChanged(); }
        }
        public bool TraceAlarmRaised
        {
            get { return HasFlag(TraceFlags.AlarmRaised); }
            set { SetFlag(TraceFlags.AlarmRaised, value); OnPropertyChanged(); }
        }
        public bool TraceScanChange
        {
            get { return HasFlag(TraceFlags.ScanChange); }
            set { SetFlag(TraceFlags.ScanChange, value); OnPropertyChanged(); }
        }
        public bool TraceWriteToTag
        {
            get { return HasFlag(TraceFlags.WriteToTag); }
            set { SetFlag(TraceFlags.WriteToTag, value); OnPropertyChanged(); }
        }

        private bool HasFlag(TraceFlags f) { return ((TraceFlags)_traceWord & f) != 0; }
        private void SetFlag(TraceFlags f, bool val)
        {
            if (val) TraceWord = _traceWord | (int)f;
            else TraceWord = _traceWord & ~(int)f;
        }

        // Commands
        public ICommand AddTagCommand { get; private set; }
        public ICommand RemoveTagCommand { get; private set; }
        public ICommand ShowDetailsCommand { get; private set; }
        public ICommand ToggleScanCommand { get; private set; }
        public ICommand WriteValueCommand { get; private set; }
        public ICommand AcknowledgeAlarmCommand { get; private set; }
        public ICommand GenerateReportCommand { get; private set; }
        public ICommand ExportConfigCommand { get; private set; }
        public ICommand ImportConfigCommand { get; private set; }
        public ICommand ShowHistoryCommand { get; private set; }
        public ICommand ShowFilterCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }

        public MainViewModel()
        {
            _traceWord = (int)SystemLogger.ActiveFlags;

            _dc.TagValueChanged += OnTagValueChanged;
            _dc.AlarmRaised += OnAlarmRaised;

            LocalizationService.Instance.LanguageChanged += (s, e) =>
            {
                OnPropertyChanged("TagsLabel");
                OnPropertyChanged("AlarmsLabel");
                OnPropertyChanged("AddTagLabel");
                OnPropertyChanged("ReportLabel");
                OnPropertyChanged("FilterLabel");
                OnPropertyChanged("ExportLabel");
                OnPropertyChanged("ImportLabel");
                OnPropertyChanged("SettingsLabel");
                OnPropertyChanged("LogoutLabel");
            };

            RefreshTags();
            RefreshAlarms();

            AddTagCommand = new RelayCommand(_ => OpenAddWindow(), _ => AuthService.Instance.IsAdmin);
            RemoveTagCommand = new RelayCommand(p => RemoveTag(p as TagViewModel),
                p => p is TagViewModel && AuthService.Instance.IsAdmin);
            ShowDetailsCommand = new RelayCommand(p => ShowDetails(p as TagViewModel));
            ToggleScanCommand = new RelayCommand(p => ToggleScan(p as TagViewModel),
                p => p is TagViewModel && ((TagViewModel)p).Tag.IsInput && AuthService.Instance.IsAdmin);
            WriteValueCommand = new RelayCommand(p => ShowWriteDialog(p as TagViewModel),
                p => p is TagViewModel && !((TagViewModel)p).Tag.IsInput && AuthService.Instance.IsAdmin);
            AcknowledgeAlarmCommand = new RelayCommand(p => AcknowledgeAlarm(p as ActivatedAlarm),
                p => AuthService.Instance.IsAdmin);
            GenerateReportCommand = new RelayCommand(_ => GenerateReport());
            ExportConfigCommand = new RelayCommand(_ => ExportConfig(), _ => AuthService.Instance.IsAdmin);
            ImportConfigCommand = new RelayCommand(_ => ImportConfig(), _ => AuthService.Instance.IsAdmin);
            ShowHistoryCommand = new RelayCommand(p => ShowHistory(p as TagViewModel));
            ShowFilterCommand = new RelayCommand(_ => OpenFilterWindow());
            LogoutCommand = new RelayCommand(_ => Logout());
        }

        public void RefreshTags()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Tags.Clear();
                foreach (var tag in _dc.GetAllTags())
                    Tags.Add(new TagViewModel(tag));
            });
        }

        private void RefreshAlarms()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RecentAlarms.Clear();
                foreach (var a in _dc.GetRecentAlarms())
                    RecentAlarms.Add(a);
            });
        }

        private void OnTagValueChanged(object sender, TagValueChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = Tags.FirstOrDefault(t => t.TagName == e.Tag.Name);
                if(vm != null)
                {
                    vm.CurrentValue = e.NewValue;
                    // Osvezi alarm stanje
                    if(e.Tag.Alarms != null && e.Tag.Alarms.All(a => a.State == AlarmState.Inactive))
                        vm.AlarmState = AlarmState.Inactive;
                }
            });
        }

        private void OnAlarmRaised(object sender, AlarmRaisedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = Tags.FirstOrDefault(t => t.TagName == e.TagName);
                if (vm != null) vm.AlarmState = AlarmState.Active;
                RefreshAlarms();
                if (AlarmSoundEnabled) PlayAlarmSound();
                SystemLogger.Log(TraceFlags.AlarmRaised, "Alarm raised: " + e.TagName + " - " + e.Message);
            });
        }

        private void PlayAlarmSound()
        {
            try { SystemSounds.Exclamation.Play(); }
            catch { }
        }

        private void OpenAddWindow()
        {
            AuthService.Instance.RecordActivity();
            var win = new Views.AddWindow();
            win.Owner = Application.Current.MainWindow;
            if (win.ShowDialog() == true) RefreshTags();
        }

        private void RemoveTag(TagViewModel vm)
        {
            if (vm == null) return;
            AuthService.Instance.RecordActivity();
            if (MessageBox.Show("Ukloniti tag '" + vm.TagName + "'?", "Potvrda",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _dc.RemoveTag(vm.TagName);
                Tags.Remove(vm);
                SystemLogger.Log(TraceFlags.TagUpdate, "Tag removed: " + vm.TagName);
            }
        }

        private void ShowDetails(TagViewModel vm)
        {
            if (vm == null) return;
            var win = new Views.TagDetailsWindow(vm.Tag);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        private void ToggleScan(TagViewModel vm)
        {
            if (vm == null) return;
            AuthService.Instance.RecordActivity();
            bool newState = !(vm.Tag.ScanOn ?? false);
            _dc.SetScanState(vm.TagName, newState);
            SystemLogger.Log(TraceFlags.ScanChange, "Scan " + (newState ? "ON" : "OFF") + ": " + vm.TagName);
            RefreshTags();
        }

        private void ShowWriteDialog(TagViewModel vm)
        {
            if (vm == null) return;
            AuthService.Instance.RecordActivity();
            var win = new Views.WriteValueWindow(vm.Tag);
            win.Owner = Application.Current.MainWindow;
            if (win.ShowDialog() == true)
            {
                double val;
                if (double.TryParse(win.EnteredValue, out val))
                {
                    _dc.SetOutputValue(vm.TagName, val);
                    vm.CurrentValue = val;
                    SystemLogger.Log(TraceFlags.WriteToTag, "Write " + val + " -> " + vm.TagName);
                }
            }
        }

        private void AcknowledgeAlarm(ActivatedAlarm alarm)
        {
            if (alarm == null) return;
            AuthService.Instance.RecordActivity();
            _dc.AcknowledgeAlarm(alarm.AlarmId);
            var vm = Tags.FirstOrDefault(t => t.TagName == alarm.TagName);
            if (vm != null) vm.AlarmState = AlarmState.Acknowledged;
            SystemLogger.Log(TraceFlags.AlarmAck, "Alarm acknowledged: ID=" + alarm.AlarmId + ", Tag=" + alarm.TagName);
            RefreshAlarms();
        }

        private void GenerateReport()
        {
            var dlg = new SaveFileDialog { Filter = "Text Files|*.txt", FileName = "SCADA_Report.txt" };
            if (dlg.ShowDialog() == true)
            {
                var history = _dc.GetAverageRangeHistory();
                using (var sw = new StreamWriter(dlg.FileName))
                {
                    sw.WriteLine("SCADA Report - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("============================================");
                    sw.WriteLine("Analog input values near (HighLimit + LowLimit) / 2 +/- 5");
                    sw.WriteLine();
                    foreach (var h in history)
                        sw.WriteLine(string.Format("Tag={0}  Value={1:F2}  Time={2:yyyy-MM-dd HH:mm:ss}",
                            h.TagName, h.Value, h.Timestamp));
                }
                SystemLogger.Log(TraceFlags.ImportExport, "Report generated: " + dlg.FileName);
                MessageBox.Show("Izveštaj generisan!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportConfig()
        {
            var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = "scada_config.json" };
            if (dlg.ShowDialog() == true)
            {
                ConfigService.ExportConfig(dlg.FileName);
                MessageBox.Show("Konfiguracija eksportovana!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportConfig()
        {
            var dlg = new OpenFileDialog { Filter = "JSON|*.json" };
            if (dlg.ShowDialog() == true)
            {
                int count = ConfigService.ImportConfig(dlg.FileName);
                RefreshTags();
                MessageBox.Show("Importovano " + count + " tagova.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowHistory(TagViewModel vm)
        {
            if (vm == null || vm.TagType != TagType.AI) return;
            var win = new Views.HistoryWindow(vm.Tag);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        private void OpenFilterWindow()
        {
            var win = new Views.FilterWindow();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        private void Logout()
        {
            AuthService.Instance.Logout();
            Application.Current.Dispatcher.Invoke(() =>
            {
                var login = new Views.LoginWindow();
                login.Show();
                foreach (Window w in Application.Current.Windows.OfType<Window>().ToList())
                    if (!(w is Views.LoginWindow)) w.Close();
            });
        }
    }
}
