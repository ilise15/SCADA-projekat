using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DataConcentrator;

namespace ScadaGUI.Views
{
    public partial class HistoryWindow : Window
    {
        private Tag _tag;

        public HistoryWindow(Tag tag)
        {
            InitializeComponent();
            _tag = tag;
            TitleText.Text = "Istorija: " + tag.Name;
            Loaded += (s, e) => DrawChart();
        }

        private void DrawChart()
        {
            var history = DataConcentratorService.Instance.GetTagHistory(_tag.Name);
            if (!history.Any())
            {
                MinText.Text = "Nema podataka";
                return;
            }

            double min = history.Min(item => item.Value);
            double max = history.Max(item => item.Value);
            double avg = history.Average(item => item.Value);
            MinText.Text = "Min: " + min.ToString("F2");
            MaxText.Text = "Maks: " + max.ToString("F2");
            AvgText.Text = "Prosek: " + avg.ToString("F2");

            ChartCanvas.Children.Clear();
            double w = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 760;
            double chartHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 300;
            double padding = 40;
            double chartW = w - 2 * padding;
            double chartH = chartHeight - 2 * padding;
            double valueRange = max - min;
            if (valueRange < 0.001) valueRange = 1;

            // Axes
            ChartCanvas.Children.Add(new Line
            {
                X1 = padding,
                Y1 = padding,
                X2 = padding,
                Y2 = chartHeight - padding,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            });
            ChartCanvas.Children.Add(new Line
            {
                X1 = padding,
                Y1 = chartHeight - padding,
                X2 = w - padding,
                Y2 = chartHeight - padding,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            });

            // Alarm lines
            if (_tag.Alarms != null)
            {
                foreach (var alarm in _tag.Alarms)
                {
                    double alarmY = chartHeight - padding - ((alarm.LimitValue - min) / valueRange) * chartH;
                    ChartCanvas.Children.Add(new Line
                    {
                        X1 = padding,
                        Y1 = alarmY,
                        X2 = w - padding,
                        Y2 = alarmY,
                        Stroke = Brushes.Red,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 4, 4 }
                    });
                    var lbl = new TextBlock
                    {
                        Text = "Alarm: " + alarm.LimitValue.ToString("F1"),
                        Foreground = Brushes.Red,
                        FontSize = 10
                    };
                    Canvas.SetLeft(lbl, w - padding - 80); Canvas.SetTop(lbl, alarmY - 14);
                    ChartCanvas.Children.Add(lbl);
                }
            }

            // Data line
            var points = new PointCollection();
            for (int i = 0; i < history.Count; i++)
            {
                double x = padding + (i / (double)Math.Max(history.Count - 1, 1)) * chartW;
                double y = chartHeight - padding - ((history[i].Value - min) / valueRange) * chartH;
                points.Add(new Point(x, y));
            }
            if (points.Count > 1)
            {
                ChartCanvas.Children.Add(new Polyline
                {
                    Points = points,
                    Stroke = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                });
            }

            // Y-axis labels
            for (int i = 0; i <= 4; i++)
            {
                double val = min + (valueRange * i / 4.0);
                double y = chartHeight - padding - (i / 4.0) * chartH;
                var lbl = new TextBlock { Text = val.ToString("F1"), FontSize = 9, Foreground = Brushes.Gray };
                Canvas.SetLeft(lbl, 2); Canvas.SetTop(lbl, y - 7);
                ChartCanvas.Children.Add(lbl);
                ChartCanvas.Children.Add(new Line
                {
                    X1 = padding,
                    Y1 = y,
                    X2 = w - padding,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                });
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
