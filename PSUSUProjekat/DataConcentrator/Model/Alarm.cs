using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("Alarms")]
    public class Alarm
    {
        [Key]
        public int Id { get; set; }
        public string TagName { get; set; }
        public double LimitValue { get; set; }
        public AlarmDirection Direction { get; set; }
        public string Message { get; set; }
        public AlarmState State { get; set; }
    }
}
