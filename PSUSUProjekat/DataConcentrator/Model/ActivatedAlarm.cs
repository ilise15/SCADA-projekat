using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("ActivatedAlarms")]
    public class ActivatedAlarm
    {
        [Key]
        public int Id { get; set; }
        public int AlarmId { get; set; }
        public string TagName { get; set; }
        public string Message { get; set; }
        public DateTime ActivatedAt { get; set; }
        public double ValueAtActivation { get; set; }
    }
}
