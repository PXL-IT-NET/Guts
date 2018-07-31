using System.Windows;
using System.Windows.Controls.Primitives;

namespace Guts.Client.Classic.TestTools.WPF
{
    public static class ButtonBaseExtensions
    {
        public static void FireClickEvent(this ButtonBase button)
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
    }
}
