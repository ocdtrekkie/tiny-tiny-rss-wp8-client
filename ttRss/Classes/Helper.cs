using CaledosLab.Portable.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TinyTinyRSS.Interface;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace TinyTinyRSS.Classes
{
    public class Helper
    {
        /// <summary>
        /// Appends a '+' symbol toa given string if the given bool is true.
        /// </summary>
        /// <param name="append">Should + be appended?</param>
        /// <param name="toAppendTo">Text to append + to.</param>
        /// <returns>Given string with or without +-sign.</returns>
        public static string AppendPlus(bool append, string toAppendTo)
        {
            if (append)
            {
                toAppendTo = toAppendTo + "+";
            }
            return toAppendTo;
        }

        // The method traverses the visual tree lazily, layer by layer
        // and returns the objects of the desired type
        public static IEnumerable<T> GetChildrenOfType<T>(DependencyObject start) where T : class
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();

                var realItem = item as T;
                if (realItem != null)
                {
                    yield return realItem;
                }

                int count = VisualTreeHelper.GetChildrenCount(item);
                for (int i = 0; i < count; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(item, i));
                }
            }
        }

        /// <summary>
        /// Find an element with the given name below the given element.
        /// </summary>
        /// <param name="element">Element that should contain the other one.</param>
        /// <param name="name">Name to look for.</param>
        /// <returns>The found element or null.</returns>
        public static FrameworkElement FindDescendantByName(FrameworkElement element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name)) { return null; }

            if (name.Equals(element.Name, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var result = FindDescendantByName((VisualTreeHelper.GetChild(element, i) as FrameworkElement), name);
                if (result != null) { return result; }
            }
            return null;
        }
    }
}
