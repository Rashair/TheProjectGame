using GameMaster.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace GameMaster.Tests
{
    static class ReflectionHelpers
    {
        public static MethodInfo GetMethod(string methodName, Type type)
        {
            Assert.False(string.IsNullOrWhiteSpace(methodName), $"{nameof(methodName)} cannot be null or whitespace");

            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(method == null, $"Method {methodName} not found");

            return method;
        }

        public static MethodInfo GetMethod(string methodName)
        {
            return GetMethod(methodName, typeof(GM));
        }

        public static FieldInfo GetField(string fieldName, Type type)
        {
            Assert.False(string.IsNullOrWhiteSpace(fieldName), $"{nameof(fieldName)} cannot be null or whitespace");

            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(field == null, $"Field {fieldName} not found");

            return field;
        }

        public static FieldInfo GetField(string fieldName)
        {
            return GetField(fieldName, typeof(GM));
        }
    }
}
