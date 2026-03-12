using System.Reflection;

namespace Guts.Client.Core.TestTools;

public static class ObjectExtensions
{
    extension(object obj)
    {
        public bool HasPrivateField<T>()
        {
            return obj.HasPrivateField<T>(field => true);
        }

        public bool HasPrivateField<T>(Func<FieldInfo, bool> filterFunc)
        {
            var objectType = obj.GetType();
            var fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            return fields.Any(filterFunc);
        }

        public bool HasPrivateFieldValue<T>(Func<T?, bool> filterFunc)
        {
            var objectType = obj.GetType();
            IEnumerable<FieldInfo> fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            try
            {
                IEnumerable<T?> values = fields.Select(field => (T?)field.GetValue(obj));
                return values.Any(filterFunc);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public T? GetPrivateFieldValue<T>()
        {
            return obj.GetPrivateFieldValue<T>((field) => true);
        }

        public T? GetPrivateFieldValueByName<T>(string fieldName)
        {
            return obj.GetPrivateFieldValue<T>(field => field.Name.ToLower() == fieldName.ToLower());
        }

        public T? GetPrivateFieldValue<T>(Func<FieldInfo, bool> filterFunc)
        {
            var objectType = obj.GetType();
            var fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            var theField = fields.FirstOrDefault(filterFunc);

            if (theField == null) throw new FieldAccessException("Could not find a matching field");

            return (T?)theField.GetValue(obj);
        }

        public IEnumerable<T?> GetAllPrivateFieldValues<T>()
        {
            var objectType = obj.GetType();
            var fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            return fields.Select(field => (T?)field.GetValue(obj));
        }

        public bool HasPrivateMethod(Func<MethodInfo, bool> filterFunc)
        {
            var objectType = obj.GetType();
            try
            {
                var methodInfos = objectType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(filterFunc);
                return methodInfos.Any();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}