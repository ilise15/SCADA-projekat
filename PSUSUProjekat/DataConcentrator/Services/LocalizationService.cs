using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace DataConcentrator
{
    public enum AppLanguage { Serbian, English }
    public enum DateFormatType { DMY, MDY, YMD }

    public class LocalizationService
    {
        private static LocalizationService _instance;
        public static LocalizationService Instance
        {
            get
            {
                if (_instance == null) _instance = new LocalizationService();
                return _instance;
            }
        }

        public AppLanguage Language { get; private set; }
        public DateFormatType DateFormat { get; private set; }
        public string TimeZoneId { get; private set; }

        public event EventHandler LanguageChanged;

        private Dictionary<string, Dictionary<string, string>> _strings;

        private LocalizationService()
        {
            Language = AppLanguage.Serbian;
            DateFormat = DateFormatType.DMY;
            TimeZoneId = "Central European Standard Time";
            InitStrings();
        }

        private void InitStrings()
        {
            _strings = new Dictionary<string, Dictionary<string, string>>();
            _strings["sr"] = new Dictionary<string, string>
            {
                {"Tags", "Tagovi"}, {"Alarms", "Alarmi"}, {"Add", "Dodaj"},
                {"Remove", "Ukloni"}, {"Details", "Detalji"}, {"Report", "Izveštaj"},
                {"Settings", "Podešavanja"}, {"Login", "Prijavi se"}, {"Logout", "Odjavi se"},
                {"Username", "Korisničko ime"}, {"Password", "Lozinka"}, {"Role", "Uloga"},
                {"Filter", "Filter"}, {"Export", "Eksportuj"}, {"Import", "Importuj"},
                {"History", "Istorija"}, {"Min", "Min"}, {"Max", "Maks"}, {"Average", "Prosek"},
                {"Active", "Aktivan"}, {"Acknowledged", "Potvrđen"}, {"Inactive", "Neaktivan"},
                {"DarkMode", "Tamna tema"}, {"Language", "Jezik"}, {"Timezone", "Vremenska zona"},
                {"Save", "Sačuvaj"}, {"Cancel", "Otkaži"}
            };
            _strings["en"] = new Dictionary<string, string>
            {
                {"Tags", "Tags"}, {"Alarms", "Alarms"}, {"Add", "Add"},
                {"Remove", "Remove"}, {"Details", "Details"}, {"Report", "Report"},
                {"Settings", "Settings"}, {"Login", "Login"}, {"Logout", "Logout"},
                {"Username", "Username"}, {"Password", "Password"}, {"Role", "Role"},
                {"Filter", "Filter"}, {"Export", "Export"}, {"Import", "Import"},
                {"History", "History"}, {"Min", "Min"}, {"Max", "Max"}, {"Average", "Average"},
                {"Active", "Active"}, {"Acknowledged", "Acknowledged"}, {"Inactive", "Inactive"},
                {"DarkMode", "Dark Mode"}, {"Language", "Language"}, {"Timezone", "Timezone"},
                {"Save", "Save"}, {"Cancel", "Cancel"}
            };
        }

        public string Get(string key)
        {
            string lang = Language == AppLanguage.Serbian ? "sr" : "en";
            string val;
            if (_strings[lang].TryGetValue(key, out val)) return val;
            return key;
        }

        public void SetLanguage(AppLanguage lang)
        {
            Language = lang;
            string culture = lang == AppLanguage.Serbian ? "sr-Latn-RS" : "en-US";
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            if (LanguageChanged != null) LanguageChanged(this, EventArgs.Empty);
        }

        public string FormatDate(DateTime dt)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
            var local = TimeZoneInfo.ConvertTimeFromUtc(dt.ToUniversalTime(), tz);
            if (DateFormat == DateFormatType.DMY) return local.ToString("dd.MM.yyyy HH:mm:ss");
            if (DateFormat == DateFormatType.MDY) return local.ToString("MM/dd/yyyy HH:mm:ss");
            return local.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void SetTimezone(string tzId) { TimeZoneId = tzId; }
        public void SetDateFormat(DateFormatType fmt) { DateFormat = fmt; }
    }
}
