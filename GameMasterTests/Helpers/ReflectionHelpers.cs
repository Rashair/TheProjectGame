using System;
using System.Reflection;

using GameMaster.Models;
using Xunit;

namespace GameMaster.Tests.Helpers
{
    internal static class ReflectionHelpers
    {
        public static void Invoke(this object obj, string methodName, Type type, params object[] parameters)
        {
            var method = GetMethod(methodName, type);
            method.Invoke(obj, parameters);
        }

        public static void Invoke(this object obj, string methodName, params object[] parameters)
        {
            obj.Invoke(methodName, typeof(GM), parameters);
        }

        public static T Invoke<T>(this object obj, string methodName, Type type, params object[] parameters)
        {
            var method = GetMethod(methodName);
            return (T)method.Invoke(obj, parameters);
        }

        public static T Invoke<T>(this object obj, string methodName, params object[] parameters)
        {
            return obj.Invoke<T>(methodName, typeof(GM), parameters);
        }

        private static MethodInfo GetMethod(string methodName, Type type)
        {
            Assert.False(string.IsNullOrWhiteSpace(methodName), $"{nameof(methodName)} cannot be null or whitespace");

            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(method == null, $"Method {methodName} not found");

            return method;
        }

        private static MethodInfo GetMethod(string methodName)
        {
            return GetMethod(methodName, typeof(GM));
        }

        private static FieldInfo GetField(string fieldName, Type type)
        {
            Assert.False(string.IsNullOrWhiteSpace(fieldName), $"{nameof(fieldName)} cannot be null or whitespace");

            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(field == null, $"Field {fieldName} not found");

            return field;
        }

        private static FieldInfo GetField(string fieldName)
        {
            return GetField(fieldName, typeof(GM));
        }

        public static T GetValue<T>(this object obj, string fieldName, Type type)
        {
            return (T)GetField(fieldName, type).GetValue(obj);
        }

        public static T GetValue<T>(this object obj, string fieldName)
        {
            return obj.GetValue<T>(fieldName, typeof(GM));
        }
    }
}
