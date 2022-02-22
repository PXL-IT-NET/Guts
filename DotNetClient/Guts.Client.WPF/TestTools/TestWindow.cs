using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Guts.Client.WPF.TestTools
{
    public class TestWindow<TWindow> : IDisposable where TWindow : Window, new()
    {
        public TWindow Window { get; }

        public TestWindow()
        {
            Window = new TWindow();
            Window.Show();
        }

        public T GetPrivateField<T>(Func<FieldInfo, bool> filterFunc) where T : class
        {
            return GetPrivateFields<T>(filterFunc).FirstOrDefault();
        }

        public object GetPrivateField(Type fieldType, Func<FieldInfo, bool> filterFunc)
        {
            return GetPrivateFields(fieldType, filterFunc).FirstOrDefault();
        }

        public T GetPrivateField<T>() where T : class
        {
            return GetPrivateField<T>(field => true);
        }

        public object GetPrivateField(Type fieldType)
        {
            return GetPrivateField(fieldType, field => true);
        }

        public IList<T> GetPrivateFields<T>(Func<FieldInfo, bool> filterFunc) where T : class
        {
            return GetPrivateFields(typeof(T), filterFunc).OfType<T>().ToList();
        }

        public IList<object> GetPrivateFields(Type fieldType, Func<FieldInfo, bool> filterFunc)
        {
            var windowType = typeof(TWindow);
            var fields = windowType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.FieldType == fieldType)
                .Where(filterFunc);

            var values = new List<object>();
            foreach (var field in fields)
            {
                values.Add(field.GetValue(Window));
            }

            return values;
        }

        public IList<T> GetPrivateFields<T>() where T : class
        {
            return GetPrivateFields<T>(field => true);
        }

        public IList<T> GetUIElements<T>() where T : UIElement
        {
            return Window.FindVisualChildren<T>().ToList();
        }

        public T GetContentControlByPartialContentText<T>(string contentTextPart) where T : ContentControl
        {
            var contentControls = Window.FindVisualChildren<T>().ToList();
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
                Window?.Close();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
