using System;
using System.Reflection;

using Xunit;

namespace Player.Tests.Helpers
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
            obj.Invoke(methodName, typeof(Player.Models.Player), parameters);
        }

        public static T Invoke<T>(this object obj, string methodName, Type type, params object[] parameters)
        {
            var method = GetMethod(methodName);
            return (T)method.Invoke(obj, parameters);
        }

        public static T Invoke<T>(this object obj, string methodName, params object[] parameters)
        {
            return obj.Invoke<T>(methodName, typeof(Player.Models.Player), parameters);
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
            return GetMethod(methodName, typeof(Player.Models.Player));
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
            return GetField(fieldName, typeof(Player.Models.Player));
        }

        public static T GetValue<T, X>(this X obj, string fieldName)
            where X : class
        {
            return (T)GetField(fieldName, typeof(X)).GetValue(obj);
        }

        public static T GetValue<T>(this Player.Models.Player player, string fieldName)
        {
            return player.GetValue<T, Player.Models.Player>(fieldName);
        }
    }
}
