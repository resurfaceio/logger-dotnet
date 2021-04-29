using System;

namespace Resurfaceio
{
    public sealed class UsageLoggers {
        
        private static readonly bool BRICKED = "true".Equals(Environment.GetEnvironmentVariable("USAGE_LOGGERS_DISABLE"));

        private static bool disabled = BRICKED;
        
        public static void Disable() {
            disabled = true;
        }

        public static void Enable() {
            if (!BRICKED) disabled = false;
        }

        public static bool IsEnabled() {
            return !disabled;
        }

        public static string UrlByDefault() {
            return Environment.GetEnvironmentVariable("USAGE_LOGGERS_URL");
        }
    }
}