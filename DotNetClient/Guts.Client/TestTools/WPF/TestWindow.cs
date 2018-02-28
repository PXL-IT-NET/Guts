using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Guts.Client.TestTools.WPF
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
            return GetPrivateFields<T>(filterFunc).FirstOrDefault();
        }

        public T GetPrivateField<T>() where T : class
        {
            return GetPrivateField<T>(field => true);
        }

        public IList<T> GetPrivateFields<T>(Func<FieldInfo, bool> filterFunc) where T : class
        {
            var windowType = typeof(TWindow);
            var fields = windowType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            var values = new List<T>();
            foreach (var field in fields)
            {
                values.Add((T)field.GetValue(_windowToTest));
            }

            return values;
        }

        public IList<T> GetPrivateFields<T>() where T : class
        {
            return GetPrivateFields<T>(field => true);
        }

        public IList<T> GetUIElements<T>() where T : UIElement
        {
            return _windowToTest.FindVisualChildren<T>().ToList();
        }

        public T GetContentControlByPartialContentText<T>(string contentTextPart) where T : ContentControl
        {
            var contentControls = _windowToTest.FindVisualChildren<T>().ToList();
            return contentControls.FirstOrDefault(c =>
            {
                var contentText = c.Content as string;
                return !string.IsNullOrEmpty(contentText) && contentText.ToLower().Contains(contentTextPart.ToLower());
            });

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
