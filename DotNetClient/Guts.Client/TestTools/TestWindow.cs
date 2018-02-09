using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Guts.Client.TestTools
{
    public class TestWindow<TWindow> : IDisposable where TWindow : Window, new()
    {
        private readonly TWindow _windowToTest;

        public TestWindow()
        {
            _windowToTest = new TWindow();
            _windowToTest.Show();
        }

        public T GetPrivateField<T>(Func<FieldInfo, bool> filterFunc) where T : class
        {
            var windowType = typeof(TWindow);
            var fields = windowType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            var theField = fields.FirstOrDefault(filterFunc);

            if (theField == null) return null;

            return (T)theField.GetValue(_windowToTest);
        }

        public T GetPrivateField<T>() where T : class
        {
            return GetPrivateField<T>(field => true);
        }

        public IList<T> GetUIElements<T>() where T : UIElement
        {
            return FindVisualChildren<T>(_windowToTest).ToList();
        }

        public T GetContentControlByPartialContentText<T>(string contentTextPart) where T : ContentControl
        {
            var contentControls = FindVisualChildren<T>(_windowToTest).ToList();
            return contentControls.FirstOrDefault(c =>
            {
                var contentText = c.Content as string;
                return !string.IsNullOrEmpty(contentText) && contentText.ToLower().Contains(contentTextPart.ToLower());
            });

        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
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

        #region IDisposable Support
        private bool _disposed; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _windowToTest?.Close();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
