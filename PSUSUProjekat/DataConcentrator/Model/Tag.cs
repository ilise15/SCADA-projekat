using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    public enum TagType { DI, DO, AI, AO }
    public enum AlarmState { Inactive, Active, Acknowledged }
    public enum AlarmDirection { AboveLimit, BelowLimit }

    public class Tag :INotifyPropertyChanged
    {
        private string name;
        private string description;
        private string ioAddress;
        private double currentValue;
        private TagType tagType;

        [Key]
        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); OnPropertyChanged("TagName"); }
        }

        [NotMapped]
        public string TagName
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); OnPropertyChanged("TagName"); }
        }

        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged("Description"); }
        }

        public string IOAddress
        {
            get { return ioAddress; }
            set { ioAddress = value; OnPropertyChanged("IOAddress"); }
        }

        public TagType TagType
        {
            get { return tagType; }
            set { tagType = value; OnPropertyChanged("TagType"); }
        }

        [NotMapped]
        public double CurrentValue
        {
            get { return currentValue; }
            set { currentValue = value; OnPropertyChanged("CurrentValue"); }
        }

        // Input only
        public int? ScanTime { get; set; }
        public bool? ScanOn { get; set; }

        // Analog only
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string Units { get; set; }

        // Output only
        public double? InitialValue { get; set; }

        // AI only
        public double? Deadband { get; set; }
        public double? Hysteresis { get; set; }

        public virtual ICollection<Alarm> Alarms { get; set; }
        public virtual ICollection<TagHistory> History { get; set; }

        [NotMapped]
        public bool IsInput => TagType == TagType.AI || TagType == TagType.DI;

        [NotMapped]
        public bool IsAnalog => TagType == TagType.AI || TagType == TagType.AO;

        public Tag()
        {
            Alarms = new List<Alarm>();
            History = new List<TagHistory>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
