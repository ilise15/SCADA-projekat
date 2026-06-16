using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DataConcentrator
{
    public class AuthService
    {
        private static AuthService _instance;
        public static AuthService Instance
        {
            get
            {
                if (_instance == null) _instance = new AuthService();
                return _instance;
            }
        }

        public User CurrentUser { get; private set; }
        public DateTime LastActivity { get; private set; }
        private Timer _inactivityTimer;

        public event EventHandler AdminTimedOut;

        private AuthService()
        {
            SeedUsers();
        }

        private void SeedUsers()
        {
            try
            {
                var ctx = ContextClass.Instance;
                if (!ctx.Users.Any())
                {
                    ctx.Users.Add(new User { Username = "admin", PasswordHash = HashPassword("Admin@123456789"), Role = "admin" });
                    ctx.Users.Add(new User { Username = "operater1", PasswordHash = HashPassword("Operater@123456789"), Role = "operater" });
                    ctx.Users.Add(new User { Username = "student1", PasswordHash = HashPassword("Student@123456789"), Role = "student" });
                    ctx.Users.Add(new User { Username = "teacher1", PasswordHash = HashPassword("Teacher@123456789"), Role = "teacher" });
                    ctx.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                SystemLogger.LogError(ex, "SeedUsers");
            }
        }

        public bool Login(string username, string password, string role)
        {
            try
            {
                string hash = HashPassword(password);
                var ctx = ContextClass.Instance;
                var user = ctx.Users.FirstOrDefault(u =>
                    u.Username == username && u.PasswordHash == hash && u.Role == role);

                if (user != null)
                {
                    CurrentUser = user;
                    LastActivity = DateTime.Now;
                    SystemLogger.Log(TraceFlags.Login, string.Format("User '{0}' logged in as {1}", username, role));
                    if (role == "admin") StartInactivityTimer();
                    return true;
                }
            }
            catch (Exception ex)
            {
                SystemLogger.LogError(ex, "Login");
            }
            return false;
        }

        public void RecordActivity()
        {
            LastActivity = DateTime.Now;
        }

        public void Logout()
        {
            if (CurrentUser != null)
                SystemLogger.Log(TraceFlags.Login, string.Format("User '{0}' logged out", CurrentUser.Username));
            CurrentUser = null;
            if (_inactivityTimer != null)
            {
                _inactivityTimer.Dispose();
                _inactivityTimer = null;
            }
        }

        public bool IsAdmin { get { return CurrentUser != null && CurrentUser.Role == "admin"; } }
        public bool IsLoggedIn { get { return CurrentUser != null; } }

        private void StartInactivityTimer()
        {
            if (_inactivityTimer != null) _inactivityTimer.Dispose();
            _inactivityTimer = new Timer(_ =>
            {
                if ((DateTime.Now - LastActivity).TotalMinutes >= 5)
                {
                    SystemLogger.Log(TraceFlags.Login, string.Format("Admin '{0}' auto-logged out due to inactivity",
                        CurrentUser != null ? CurrentUser.Username : ""));
                    CurrentUser = null;
                    if (AdminTimedOut != null) AdminTimedOut(this, EventArgs.Empty);
                    if (_inactivityTimer != null) _inactivityTimer.Dispose();
                }
            }, null, 30000, 30000);
        }

        public static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "SCADA_SALT_2024"));
                return Convert.ToBase64String(bytes);
            }
        }

        public bool RegisterUser(string username, string password, string role)
        {
            string hash = HashPassword(password);
            var ctx = ContextClass.Instance;
            if (ctx.Users.Any(u => u.Username == username)) return false;
            ctx.Users.Add(new User { Username = username, PasswordHash = hash, Role = role });
            ctx.SaveChanges();
            return true;
        }
    }
}
