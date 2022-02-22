using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Guts.Client.Core.TestTools
{
    public static class ObjectExtensions
    {
        public static bool HasPrivateField<T>(this object obj)
        {
            return obj.HasPrivateField<T>(field => true);
        }

        public static bool HasPrivateField<T>(this object obj, Func<FieldInfo, bool> filterFunc)
        {
            var objectType = obj.GetType();
            var fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            return fields.Any(filterFunc);
        }

        public static bool HasPrivateFieldValue<T>(this object obj, Func<T, bool> filterFunc)
        {
            var objectType = obj.GetType();
            var fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            try
            {
                var values = fields.Select(field => (T)field.GetValue(obj));
                return values.Any(filterFunc);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static T GetPrivateFieldValue<T>(this object obj)
        {
            return obj.GetPrivateFieldValue<T>((field) => true);
        }

        public static T GetPrivateFieldValueByName<T>(this object oject, string fieldName)
        {
            return oject.GetPrivateFieldValue<T>(field => field.Name.ToLower() == fieldName.ToLower());
        }

        public static T GetPrivateFieldValue<T>(this object obj, Func<FieldInfo, bool> filterFunc)
        {
            var objectType = obj.GetType();
            var fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            var theField = fields.FirstOrDefault(filterFunc);

            if (theField == null) throw new FieldAccessException("Could not find a matching field");

            return (T)theField.GetValue(obj);
        }

        public static IEnumerable<T> GetAllPrivateFieldValues<T>(this object obj)
        {
            var objectType = obj.GetType();
            var fields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.FieldType == typeof(T));

            return fields.Select(field => (T)field.GetValue(obj));
        }

        public static bool HasPrivateMethod(this object obj, Func<MethodInfo, bool> filterFunc)
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
