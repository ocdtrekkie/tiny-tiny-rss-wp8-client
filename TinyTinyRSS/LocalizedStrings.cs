using TinyTinyRSS.Resources;

namespace TinyTinyRSS
{
    /// <summary>
    /// Bietet Zugriff auf Zeichenfolgenressourcen.
    /// </summary>
    public class LocalizedStrings
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources { get { return _localizedResources; } }
    }
}