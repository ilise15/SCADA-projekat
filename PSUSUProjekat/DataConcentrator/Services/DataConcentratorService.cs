using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DataConcentrator
{
    public class TagValueChangedEventArgs : EventArgs
    {
        public Tag Tag { get; set; }
        public double OldValue { get; set; }
        public double NewValue { get; set; }
    }

    public class AlarmRaisedEventArgs : EventArgs
    {
        public int ActivatedAlarmId { get; set; }
        public string TagName { get; set; }
        public string Message { get; set; }
    }

    public class DataConcentratorService
    {
        private static DataConcentratorService _instance;
        public static DataConcentratorService Instance
        {
            get
            {
                if (_instance == null) _instance = new DataConcentratorService();
                return _instance;
            }
        }

        private ConcurrentDictionary<string, Tag> _tags = new ConcurrentDictionary<string, Tag>();
        private ConcurrentDictionary<string, Timer> _scanTimers = new ConcurrentDictionary<string, Timer>();
        private readonly object _dbLock = new object();

        public event EventHandler<TagValueChangedEventArgs> TagValueChanged;
        public event EventHandler<AlarmRaisedEventArgs> AlarmRaised;

        private DataConcentratorService()
        {
            LoadTagsFromDb();
        }

        public void LoadTagsFromDb()
        {
            lock (_dbLock)
            {
                try
                {
                    var ctx = ContextClass.Instance;
                    var tags = ctx.Tags.Include("Alarms").ToList();
                    _tags.Clear();
                    foreach (var tag in tags)
                    {
                        _tags[tag.Name] = tag;
                        if (tag.IsInput && tag.ScanOn == true)
                            StartScan(tag);
                    }
                }
                catch (Exception ex)
                {
                    SystemLogger.LogError(ex, "LoadTagsFromDb");
                }
            }
        }

        public List<Tag> GetAllTags()
        {
            return _tags.Values.ToList();
        }

        public Tag GetTag(string tagName)
        {
            Tag tag;
            _tags.TryGetValue(tagName, out tag);
            return tag;
        }

        public bool AddTag(Tag tag)
        {
            lock (_dbLock)
            {
                try
                {
                    var ctx = ContextClass.Instance;
                    if (ctx.Tags.Any(t => t.Name == tag.Name)) return false;
                    ctx.Tags.Add(tag);
                    ctx.SaveChanges();
                    _tags[tag.Name] = tag;
                    if (tag.IsInput && tag.ScanOn == true)
                        StartScan(tag);
                    return true;
                }
                catch (Exception ex)
                {
                    SystemLogger.LogError(ex, "AddTag");
                    return false;
                }
            }
        }

        public bool RemoveTag(string tagName)
        {
            lock (_dbLock)
            {
                try
                {
                    StopScan(tagName);
                    var ctx = ContextClass.Instance;
                    var tag = ctx.Tags.FirstOrDefault(t => t.Name == tagName);
                    if (tag == null) return false;
                    // Remove alarms first
                    var alarms = ctx.Alarms.Where(a => a.TagName == tagName).ToList();
                    ctx.Alarms.RemoveRange(alarms);
                    ctx.Tags.Remove(tag);
                    ctx.SaveChanges();
                    Tag removed;
                    _tags.TryRemove(tagName, out removed);
                    return true;
                }
                catch (Exception ex)
                {
                    SystemLogger.LogError(ex, "RemoveTag");
                    return false;
                }
            }
        }

        public bool AddAlarm(Alarm alarm)
        {
            lock (_dbLock)
            {
                try
                {
                    var ctx = ContextClass.Instance;
                    ctx.Alarms.Add(alarm);
                    ctx.SaveChanges();
                    Tag tag;
                    if (_tags.TryGetValue(alarm.TagName, out tag))
                    {
                        if (tag.Alarms == null) tag.Alarms = new List<Alarm>();
                        tag.Alarms.Add(alarm);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    SystemLogger.LogError(ex, "AddAlarm");
                    return false;
                }
            }
        }

        public bool RemoveAlarm(int alarmId)
        {
            lock (_dbLock)
            {
                try
                {
                    var ctx = ContextClass.Instance;
                    var alarm = ctx.Alarms.Find(alarmId);
                    if (alarm == null) return false;
                    ctx.Alarms.Remove(alarm);
                    ctx.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    SystemLogger.LogError(ex, "RemoveAlarm");
                    return false;
                }
            }
        }

        public void SetOutputValue(string tagName, double value)
        {
            Tag tag;
            if (_tags.TryGetValue(tagName, out tag))
            {
                if (tag.TagType == TagType.AO)
                    PLC.Instance.SetAnalogValue(tag.IOAddress, value);
                else if (tag.TagType == TagType.DO)
                    PLC.Instance.SetDigitalValue(tag.IOAddress, value);
                tag.CurrentValue = value;
                SaveHistory(tag.Name, value);
            }
        }

        public void SetScanState(string tagName, bool scanOn)
        {
            Tag tag;
            if (_tags.TryGetValue(tagName, out tag) && tag.IsInput)
            {
                tag.ScanOn = scanOn;
                lock (_dbLock)
                {
                    try
                    {
                        var ctx = ContextClass.Instance;
                        var dbTag = ctx.Tags.FirstOrDefault(t => t.Name == tagName);
                        if (dbTag != null)
                        {
                            dbTag.ScanOn = scanOn;
                            ctx.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.LogError(ex, "SetScanState");
                    }
                }
                if (scanOn) StartScan(tag);
                else StopScan(tagName);
            }
        }

        public void AcknowledgeAlarm(int alarmId)
        {
            lock (_dbLock)
            {
                try
                {
                    var ctx = ContextClass.Instance;
                    var alarm = ctx.Alarms.Find(alarmId);
                    if (alarm != null && alarm.State == AlarmState.Active)
                    {
                        alarm.State = AlarmState.Acknowledged;
                        ctx.SaveChanges();
                        Tag tag;
                        if (_tags.TryGetValue(alarm.TagName, out tag) && tag.Alarms != null)
                        {
                            var memAlarm = tag.Alarms.FirstOrDefault(a => a.Id == alarmId);
                            if (memAlarm != null) memAlarm.State = AlarmState.Acknowledged;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SystemLogger.LogError(ex, "AcknowledgeAlarm");
                }
            }
        }

        private void StartScan(Tag tag)
        {
            StopScan(tag.Name);
            int interval = (tag.ScanTime ?? 1) * 1000;
            double lastValue = tag.CurrentValue;
            string tagName = tag.Name;

            var timer = new Timer(_ =>
            {
                try
                {
                    Tag t;
                    if (!_tags.TryGetValue(tagName, out t)) return;

                    double newVal = PLC.Instance.GetValue(t.IOAddress);

                    // Deadband check for AI
                    if (t.TagType == TagType.AI && t.Deadband.HasValue
                        && Math.Abs(newVal - lastValue) < t.Deadband.Value)
                        return;

                    double oldVal = t.CurrentValue;
                    t.CurrentValue = newVal;
                    lastValue = newVal;
                    SaveHistory(tagName, newVal);

                    if (TagValueChanged != null)
                        TagValueChanged(this, new TagValueChangedEventArgs
                        { Tag = t, OldValue = oldVal, NewValue = newVal });

                    // Check alarms for AI
                    if (t.TagType == TagType.AI && t.Alarms != null)
                    {
                        foreach (var alarm in t.Alarms)
                        {
                            bool triggered = false;
                            if (alarm.Direction == AlarmDirection.AboveLimit)
                                triggered = t.Hysteresis.HasValue
                                    ? newVal > alarm.LimitValue + t.Hysteresis.Value
                                    : newVal > alarm.LimitValue;
                            else
                                triggered = t.Hysteresis.HasValue
                                    ? newVal < alarm.LimitValue - t.Hysteresis.Value
                                    : newVal < alarm.LimitValue;

                            if (triggered && alarm.State == AlarmState.Inactive)
                            {
                                alarm.State = AlarmState.Active;
                                int activatedId = SaveActivatedAlarm(alarm, newVal);
                                if (AlarmRaised != null)
                                    AlarmRaised(this, new AlarmRaisedEventArgs
                                    {
                                        ActivatedAlarmId = activatedId,
                                        TagName = tagName,
                                        Message = alarm.Message
                                    });
                            }
                            else if(!triggered && alarm.State != AlarmState.Inactive)
                            {
                                alarm.State = AlarmState.Inactive;
                                lock(_dbLock)
                                {
                                    try
                                    {
                                        var ctx = ContextClass.Instance;
                                        var dbAlarm = ctx.Alarms.Find(alarm.Id);
                                        if(dbAlarm != null)
                                        {
                                            dbAlarm.State = AlarmState.Inactive;
                                            ctx.SaveChanges();
                                        }
                                    }
                                    catch { }
                                }
                                if(TagValueChanged != null)
                                    TagValueChanged(this, new TagValueChangedEventArgs
                                    { Tag = t, OldValue = t.CurrentValue, NewValue = t.CurrentValue });
                            }
                        
                        }
                    }
                }
                catch (Exception ex)
                {
                    SystemLogger.LogError(ex, "Scan timer");
                }
            }, null, interval, interval);

            _scanTimers[tag.Name] = timer;
        }

        private void StopScan(string tagName)
        {
            Timer timer;
            if (_scanTimers.TryRemove(tagName, out timer))
                timer.Dispose();
        }

        private void SaveHistory(string tagName, double value)
        {
            lock (_dbLock)
            {
                try
                {
                    var ctx = ContextClass.Instance;
                    ctx.TagHistory.Add(new TagHistory
                    {
                        TagName = tagName,
                        Value = value,
                        Timestamp = DateTime.Now
                    });
                    ctx.SaveChanges();
                }
                catch { }
            }
        }

        private int SaveActivatedAlarm(Alarm alarm, double value)
        {
            lock (_dbLock)
            {
                try
                {
                    var ctx = ContextClass.Instance;
                    var dbAlarm = ctx.Alarms.Find(alarm.Id);
                    if (dbAlarm != null)
                    {
                        dbAlarm.State = AlarmState.Active;
                        ctx.SaveChanges();
                    }
                    var activated = new ActivatedAlarm
                    {
                        AlarmId = alarm.Id,
                        TagName = alarm.TagName,
                        Message = alarm.Message,
                        ActivatedAt = DateTime.Now,
                        ValueAtActivation = value
                    };
                    ctx.ActivatedAlarms.Add(activated);
                    ctx.SaveChanges();
                    return activated.Id;
                }
                catch { }
                return -1;
            }
        }

        public List<ActivatedAlarm> GetRecentAlarms(int count = 50)
        {
            lock (_dbLock)
            {
                try
                {
                    return ContextClass.Instance.ActivatedAlarms
                        .OrderByDescending(a => a.ActivatedAt).Take(count).ToList();
                }
                catch { return new List<ActivatedAlarm>(); }
            }
        }

        public List<TagHistory> GetTagHistory(string tagName, DateTime? from = null,
            DateTime? to = null, double? minVal = null, double? maxVal = null)
        {
            lock (_dbLock)
            {
                try
                {
                    var query = ContextClass.Instance.TagHistory
                        .Where(h => h.TagName == tagName);
                    if (from.HasValue) query = query.Where(h => h.Timestamp >= from.Value);
                    if (to.HasValue) query = query.Where(h => h.Timestamp <= to.Value);
                    if (minVal.HasValue) query = query.Where(h => h.Value >= minVal.Value);
                    if (maxVal.HasValue) query = query.Where(h => h.Value <= maxVal.Value);
                    return query.OrderBy(h => h.Timestamp).ToList();
                }
                catch { return new List<TagHistory>(); }
            }
        }

        public List<TagHistory> GetAverageRangeHistory()
        {
            var result = new List<TagHistory>();
            lock (_dbLock)
            {
                try
                {
                    var aiTags = _tags.Values.Where(t => t.TagType == TagType.AI
                        && t.HighLimit.HasValue && t.LowLimit.HasValue).ToList();
                    foreach (var tag in aiTags)
                    {
                        double mid = (tag.HighLimit.Value + tag.LowLimit.Value) / 2.0;
                        var history = ContextClass.Instance.TagHistory
                            .Where(h => h.TagName == tag.Name
                                && h.Value >= mid - 5 && h.Value <= mid + 5).ToList();
                        result.AddRange(history);
                    }
                }
                catch { }
            }
            return result;
        }
    }
}
