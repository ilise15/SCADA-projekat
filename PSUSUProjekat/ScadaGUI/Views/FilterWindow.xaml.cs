using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using DataConcentrator;
using Microsoft.Win32;

namespace ScadaGUI.Views
{
    public partial class FilterWindow : Window
    {
        private List<TagHistory> _currentResults = new List<TagHistory>();

        public FilterWindow()
        {
            InitializeComponent();
            LoadTags();
        }

        private void LoadTags()
        {
            var tags = DataConcentratorService.Instance.GetAllTags()
                .Where(t => t.TagType == TagType.AI).ToList();
            TagFilterBox.ItemsSource = tags;
            TagFilterBox.DisplayMemberPath = "Name";
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            var selectedTag = TagFilterBox.SelectedItem as Tag;
            string tagName = selectedTag?.Name;
            DateTime? from = FromDate.SelectedDate;
            DateTime? to = ToDate.SelectedDate?.AddDays(1);
            double? minVal = null, maxVal = null;
            double tmp;
            if (double.TryParse(MinValBox.Text, out tmp)) minVal = tmp;
            if (double.TryParse(MaxValBox.Text, out tmp)) maxVal = tmp;

            if (string.IsNullOrEmpty(tagName))
            {
                // All AI tags
                _currentResults = new List<TagHistory>();
                foreach (var tag in DataConcentratorService.Instance.GetAllTags()
                    .Where(t => t.TagType == TagType.AI))
                {
                    _currentResults.AddRange(DataConcentratorService.Instance
                        .GetTagHistory(tag.Name, from, to, minVal, maxVal));
                }
            }
            else
            {
                _currentResults = DataConcentratorService.Instance
                    .GetTagHistory(tagName, from, to, minVal, maxVal);
            }

            ResultsGrid.ItemsSource = _currentResults.OrderByDescending(h => h.Timestamp).Take(500).ToList();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentResults.Any()) { MessageBox.Show("Prvo filtrirajte podatke."); return; }
            var dlg = new SaveFileDialog { Filter = "Text files|*.txt", FileName = "FilteredData.txt" };
            if (dlg.ShowDialog() == true)
            {
                using (var sw = new StreamWriter(dlg.FileName))
                {
                    sw.WriteLine("SCADA - Filtrirani podaci - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sw.WriteLine("===============================");
                    sw.WriteLine(string.Format("{0,-20} {1,-12} {2}", "Tag", "Vrednost", "Vreme"));
                    sw.WriteLine(new string('-', 60));
                    foreach (var r in _currentResults)
                        sw.WriteLine(string.Format("{0,-20} {1,-12:F3} {2:yyyy-MM-dd HH:mm:ss}",
                            r.TagName, r.Value, r.Timestamp));
                }
                SystemLogger.Log(TraceFlags.ImportExport, "Filter export: " + dlg.FileName);
                MessageBox.Show("Fajl generisan!");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
