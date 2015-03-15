using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;

namespace TinyTinyRSS.Classes
{
    public enum Resolutions { WVGA, WXGA, HD720p };

    public class ResolutionHelper
    {
        internal static double GetWidthForOrientation(ApplicationViewOrientation orientation)
        {
            if (orientation.Equals(ApplicationViewOrientation.Landscape))
            {
                return 570;
            }
            else
            {
                return 355;
            }
        } 
    }
}
