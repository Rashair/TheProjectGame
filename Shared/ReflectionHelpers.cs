using System;
using System.Reflection;

namespace Shared
{
    public static class ReflectionHelpers
    {
        /// <summary>
        /// Invoke instance, non-public, void method from TObj class
        /// </summary>
        /// <typeparam name="TObj"></typeparam>
        public static void Invoke<TObj>(this TObj obj, string methodName, params object[] parameters)
        {
            var method = GetMethod(methodName, typeof(TObj));
            method.Invoke(obj, parameters);
        }

        /// <summary>
        /// Invoke non-public, instance method from TObj class with TReturn value
        /// </summary>
        /// <typeparam name="TReturn">Type of return value</typeparam>
        /// <typeparam name="TObj">Type of object to which method belongs to.</typeparam>
        public static TReturn Invoke<TObj, TReturn>(this TObj obj, string methodName, params object[] parameters)
        {
            var method = GetMethod(methodName, typeof(TObj));
            return (TReturn)method.Invoke(obj, parameters);
        }

        private static MethodInfo GetMethod(string methodName, Type type)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException($"Method name cannot be null or whitespace");
            }

            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                throw new ArgumentException($"Method {methodName} not found");
            }

            return method;
        }

        public static TReturn GetValue<TObj, TReturn>(this TObj obj, string fieldName)
        {
            return (TReturn)GetField(fieldName, typeof(TObj)).GetValue(obj);
        }

        private static FieldInfo GetField(string fieldName, Type type)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException($"Field name cannot be null or whitespace");
            }

            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
            {
                throw new ArgumentException($"Field {fieldName} not found");
            }

            return field;
        }
    }
}
