using System.Data.Entity;

namespace DataConcentrator
{
    public class ContextClass :DbContext
    {
        private static ContextClass instance;

        public static ContextClass Instance
        {
            get
            {
                if(instance == null)
                    instance = new ContextClass();
                return instance;
            }
        }

        public DbSet<Tag> Tags { get; set; }
        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<ActivatedAlarm> ActivatedAlarms { get; set; }
        public DbSet<TagHistory> TagHistory { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
