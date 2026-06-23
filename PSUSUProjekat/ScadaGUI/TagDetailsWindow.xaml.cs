using System.Windows;
using System.Windows.Controls;
using DataConcentrator;

namespace ScadaGUI.Views
{
    public partial class TagDetailsWindow : Window
    {
        private Tag _tag;

        public TagDetailsWindow(Tag tag)
        {
            InitializeComponent();
            _tag = tag;
            TitleText.Text = "Tag: " + tag.Name + " (" + tag.TagType + ")";
            BuildPropsGrid();
            LoadAlarms();
        }

        private void LoadAlarms()
        {
            using (var ctx = new ContextClass())
            {
                var tag = ctx.Tags.Find(_tag.Name);
                if (tag != null)
                {
                    ctx.Entry(tag).Collection("Alarms").Load();
                    AlarmsGrid.ItemsSource = tag.Alarms;
                }
            }
        }

        private void BuildPropsGrid()
        {
            PropsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            PropsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddRow("Opis:", _tag.Description, 0);
            AddRow("I/O Adresa:", _tag.IOAddress, 1);
            if (_tag.ScanTime.HasValue) AddRow("Scan Time:", _tag.ScanTime + "s", 2);
            if (_tag.LowLimit.HasValue) AddRow("Low Limit:", _tag.LowLimit + " " + _tag.Units, 3);
            if (_tag.HighLimit.HasValue) AddRow("High Limit:", _tag.HighLimit + " " + _tag.Units, 4);
            if (_tag.Deadband.HasValue) AddRow("Deadband:", _tag.Deadband.ToString(), 5);
            if (_tag.Hysteresis.HasValue) AddRow("Hysteresis:", _tag.Hysteresis.ToString(), 6);
        }

        private void AddRow(string label, string value, int row)
        {
            PropsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lbl = new TextBlock { Text = label, FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 2, 0, 2) };
            var val = new TextBlock { Text = value, Margin = new Thickness(8, 2, 0, 2) };
            Grid.SetColumn(lbl, 0); Grid.SetRow(lbl, row);
            Grid.SetColumn(val, 1); Grid.SetRow(val, row);
            PropsGrid.Children.Add(lbl);
            PropsGrid.Children.Add(val);
        }

        private void RemoveAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (!AuthService.Instance.IsAdmin)
            { MessageBox.Show("Samo admin može ukloniti alarm."); return; }
            var btn = sender as Button;
            if (btn != null && btn.Tag is int)
            {
                int alarmId = (int)btn.Tag;
                DataConcentratorService.Instance.RemoveAlarm(alarmId);
                LoadAlarms();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
