using System.Windows;
using System.Windows.Media;

namespace Guts.Client.WPF.TestTools
{
    public static class DependencyObjectExtensions
    {
        /// <summary>
        /// (Recursively) searches for child WPF elements within a WPF element.
        /// </summary>
        /// <typeparam name="T">The type of child element to find.</typeparam>
        /// <param name="dependencyObject">The parent element that should contain the child (recursively).</param>
        /// <returns>All the matching children (empty if no matching children are found)</returns>
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject == null) yield break;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                if (child is T tChild)
                {
                    yield return tChild;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}