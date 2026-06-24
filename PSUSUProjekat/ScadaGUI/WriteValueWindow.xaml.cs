using System.Windows;
using DataConcentrator;

namespace ScadaGUI.Views
{
    public partial class WriteValueWindow : Window
    {
        public string EnteredValue { get; private set; }
        private Tag _tag;

        public WriteValueWindow(Tag tag)
        {
            InitializeComponent();
            _tag = tag;
            TagLabel.Text = tag.Name + " (" + tag.TagType + ")";
            if (tag.InitialValue.HasValue)
                ValueBox.Text = tag.InitialValue.Value.ToString();
        }

        private void Write_Click(object sender, RoutedEventArgs e)
        {
            double val;
            if (!double.TryParse(ValueBox.Text, out val))
            {
                ErrText.Text = "Unesite ispravan broj.";
                ErrText.Visibility = Visibility.Visible;
                return;
            }
            if (_tag.TagType == TagType.DO && val != 0 && val != 1)
            {
                ErrText.Text = "Digitalni izlaz: vrednost mora biti 0 ili 1.";
                ErrText.Visibility = Visibility.Visible;
                return;
            }
            EnteredValue = ValueBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
