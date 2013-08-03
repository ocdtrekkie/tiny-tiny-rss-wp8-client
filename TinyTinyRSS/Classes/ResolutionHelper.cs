using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TinyTinyRSS.Classes
{
    public enum Resolutions { WVGA, WXGA, HD720p };

    public class ResolutionHelper
    {
        private static bool IsWvga
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 100;
            }
        }

        private static bool IsWxga
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 160;
            }
        }

        private static bool Is720p
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 150;
            }
        }

        public static Resolutions CurrentResolution
        {
            get
            {
                if (IsWvga) return Resolutions.WVGA;
                else if (IsWxga) return Resolutions.WXGA;
                else if (Is720p) return Resolutions.HD720p;
                else throw new InvalidOperationException("Unknown resolution");
            }
        }

        public int MaxWidth {
            get
            {
                switch (CurrentResolution)
                {
                    case Resolutions.HD720p: return 720 - 24;
                    case Resolutions.WVGA: return 480 - 24;
                    case Resolutions.WXGA: return 768 - 24;
                    default: return 480;
                }
            }
        }

        public int ReducedMaxWidth
        {
            get
            {
                switch (CurrentResolution)
                {
                    case Resolutions.HD720p:
                        return 720 - 48;
                    case Resolutions.WVGA:
                        return 480 - 48;
                    case Resolutions.WXGA:
                       return 768 - 48;
                    default:
                        return 480 - 48;
                }
            }
        }

        public int ButtonSize
        {
            get
            {
                switch (CurrentResolution)
                {
                    case Resolutions.HD720p: return 330;
                    case Resolutions.WVGA: return 200;
                    case Resolutions.WXGA: return 350;
                    default: return 200;
                }
            }
        }

        internal static double GetWidthForOrientation(PageOrientation orientation)
        {
            switch (CurrentResolution)
            {
                case Resolutions.HD720p:
                    if (orientation.Equals(PageOrientation.LandscapeLeft) || orientation.Equals(PageOrientation.LandscapeRight))
                    {
                        return 1280 - 125;
                    }
                    else
                    {
                        return 720 - 48;
                    }
                case Resolutions.WVGA:
                    if (orientation.Equals(PageOrientation.LandscapeLeft) || orientation.Equals(PageOrientation.LandscapeRight))
                    {
                        return 800 - 125;
                    }
                    else
                    {
                        return 480 - 48;
                    }
                case Resolutions.WXGA:
                    if (orientation.Equals(PageOrientation.LandscapeLeft) || orientation.Equals(PageOrientation.LandscapeRight))
                    {
                        return 1280 - 125;
                    }
                    else
                    {
                        return 768 - 48;
                    }
                default:
                    if (orientation.Equals(PageOrientation.LandscapeLeft) || orientation.Equals(PageOrientation.LandscapeRight))
                    {
                        return 800 - 125;
                    }
                    else
                    {
                        return 480 - 48;
                    }
            }
        }
    }
}
