namespace OptimumCoaching.web.Core
{
    public class AppSettings
    {
        public string TokenSecretKey { get; set; } = string.Empty;
        public int TokenExpiresHours { get; set; }
        public string UserNewPassword { get; set; } = string.Empty;
        public string UserResetPassword { get; set; } = string.Empty;
        public bool IsMailServiceActive { get; set; }
        public string SubDomain { get; set; } = string.Empty;
    }
}
