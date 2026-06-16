using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("TagHistory")]
    public class TagHistory
    {
        [Key]
        public int Id { get; set; }
        public string TagName { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
