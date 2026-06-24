using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DataConcentrator;

namespace ScadaGUI.Views
{
    public partial class AddWindow : Window
    {
        public AddWindow()
        {
            InitializeComponent();
            UpdatePanels("AI");
            LoadAITags();
        }

        private void LoadAITags()
        {
            var aiTags = DataConcentratorService.Instance.GetAllTags()
                .Where(t => t.TagType == TagType.AI).ToList();
            AlarmTagBox.ItemsSource = aiTags;
            AlarmTagBox.DisplayMemberPath = "Name";
            if (aiTags.Any()) AlarmTagBox.SelectedIndex = 0;
        }

        private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string type = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            UpdatePanels(type);
        }

        private void UpdatePanels(string type)
        {
            if (CommonPanel == null) return;
            bool isAlarm = type == "Alarm";
            CommonPanel.Visibility = isAlarm ? Visibility.Collapsed : Visibility.Visible;
            AlarmPanel.Visibility = isAlarm ? Visibility.Visible : Visibility.Collapsed;
            InputPanel.Visibility = (type == "AI" || type == "DI") ? Visibility.Visible : Visibility.Collapsed;
            AnalogPanel.Visibility = (type == "AI" || type == "AO") ? Visibility.Visible : Visibility.Collapsed;
            AIPanel.Visibility = type == "AI" ? Visibility.Visible : Visibility.Collapsed;
            OutputPanel.Visibility = (type == "AO" || type == "DO") ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            string type = (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (type == "Alarm") AddAlarm();
            else AddTag(type);
        }

        private void AddTag(string type)
        {
            if (string.IsNullOrWhiteSpace(TagNameBox.Text))
            { ShowError("Ime taga je obavezno."); return; }
            if (string.IsNullOrWhiteSpace(IOAddrBox.Text))
            { ShowError("I/O adresa je obavezna."); return; }

            TagType tagType = type == "AI" ? TagType.AI :
                              type == "AO" ? TagType.AO :
                              type == "DI" ? TagType.DI : TagType.DO;

            var tag = new Tag
            {
                Name = TagNameBox.Text.Trim(),
                Description = DescBox.Text.Trim(),
                IOAddress = IOAddrBox.Text.Trim(),
                TagType = tagType
            };

            if (type == "AI" || type == "DI")
            {
                int st;
                if (!int.TryParse(ScanTimeBox.Text, out st) || st <= 0)
                { ShowError("Vreme skeniranja mora biti pozitivan broj."); return; }
                tag.ScanTime = st;
                tag.ScanOn = ScanOnBox.IsChecked == true;
            }

            if (type == "AI" || type == "AO")
            {
                double ll, hl;
                if (!double.TryParse(LowLimitBox.Text, out ll))
                { ShowError("Donja granica mora biti broj."); return; }
                if (!double.TryParse(HighLimitBox.Text, out hl))
                { ShowError("Gornja granica mora biti broj."); return; }
                if (hl <= ll) { ShowError("Gornja granica mora biti veća od donje."); return; }
                tag.LowLimit = ll;
                tag.HighLimit = hl;
                tag.Units = UnitsBox.Text.Trim();
            }

            if (type == "AI")
            {
                double db;
                if (!string.IsNullOrWhiteSpace(DeadbandBox.Text) && double.TryParse(DeadbandBox.Text, out db))
                    tag.Deadband = db;
                double hys;
                if (!string.IsNullOrWhiteSpace(HysteresisBox.Text) && double.TryParse(HysteresisBox.Text, out hys))
                    tag.Hysteresis = hys;
            }

            if (type == "AO" || type == "DO")
            {
                double iv;
                if (!string.IsNullOrWhiteSpace(InitValBox.Text) && double.TryParse(InitValBox.Text, out iv))
                    tag.InitialValue = iv;
            }

            if (!DataConcentratorService.Instance.AddTag(tag))
            { ShowError("Tag sa tim imenom već postoji."); return; }

            SystemLogger.Log(TraceFlags.TagAdd, "Tag added: " + tag.Name + " (" + type + ")");
            DialogResult = true;
        }

        private void AddAlarm()
        {
            var selectedTag = AlarmTagBox.SelectedItem as Tag;
            if (selectedTag == null) { ShowError("Izaberite AI tag."); return; }
            double limit;
            if (!double.TryParse(AlarmLimitBox.Text, out limit))
            { ShowError("Vrednost granice mora biti broj."); return; }
            if (string.IsNullOrWhiteSpace(AlarmMsgBox.Text))
            { ShowError("Poruka alarma je obavezna."); return; }

            var alarm = new Alarm
            {
                TagName = selectedTag.Name,
                LimitValue = limit,
                Direction = AlarmDirBox.SelectedIndex == 0
                    ? AlarmDirection.AboveLimit : AlarmDirection.BelowLimit,
                Message = AlarmMsgBox.Text.Trim(),
                State = AlarmState.Inactive
            };

            DataConcentratorService.Instance.AddAlarm(alarm);
            SystemLogger.Log(TraceFlags.TagAdd, "Alarm added for tag: " + selectedTag.Name);
            DialogResult = true;
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
