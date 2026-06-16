using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DataConcentrator
{
    public static class ConfigService
    {
        public static void ExportConfig(string filePath)
        {
            try
            {
                var tags = ContextClass.Instance.Tags.Include("Alarms").ToList();
                var json = JsonConvert.SerializeObject(tags, Formatting.Indented,
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                File.WriteAllText(filePath, json);
                SystemLogger.Log(TraceFlags.ImportExport, "Config exported to: " + filePath);
            }
            catch (System.Exception ex)
            {
                SystemLogger.LogError(ex, "ExportConfig");
            }
        }

        public static int ImportConfig(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var tags = JsonConvert.DeserializeObject<List<Tag>>(json,
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                if (tags == null || !tags.Any()) return 0;

                int count = 0;
                var ctx = ContextClass.Instance;

                foreach (var tag in tags)
                {
                    if (ctx.Tags.Any(t => t.Name == tag.Name)) continue;

                    var newTag = new Tag
                    {
                        Name = tag.Name,
                        Description = tag.Description,
                        IOAddress = tag.IOAddress,
                        TagType = tag.TagType,
                        ScanTime = tag.ScanTime,
                        ScanOn = tag.ScanOn,
                        LowLimit = tag.LowLimit,
                        HighLimit = tag.HighLimit,
                        Units = tag.Units,
                        InitialValue = tag.InitialValue,
                        Deadband = tag.Deadband,
                        Hysteresis = tag.Hysteresis
                    };
                    ctx.Tags.Add(newTag);
                    ctx.SaveChanges();
                    count++;

                    if (tag.Alarms != null)
                    {
                        foreach (var alarm in tag.Alarms)
                        {
                            ctx.Alarms.Add(new Alarm
                            {
                                TagName = newTag.Name,
                                LimitValue = alarm.LimitValue,
                                Direction = alarm.Direction,
                                Message = alarm.Message,
                                State = AlarmState.Inactive
                            });
                        }
                        ctx.SaveChanges();
                    }
                }

                SystemLogger.Log(TraceFlags.ImportExport,
                    "Config imported from: " + filePath + ", " + count + " tags added.");
                DataConcentratorService.Instance.LoadTagsFromDb();
                return count;
            }
            catch (System.Exception ex)
            {
                SystemLogger.LogError(ex, "ImportConfig");
                return 0;
            }
        }
    }
}
