﻿using System;
using System.Linq;
using System.Reflection;

/// <remarks> https://stackoverflow.com/a/8724150/6841224 </remarks>
/// <summary>
/// A static class for reflection type functions
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// Extension for 'Object' that copies the properties to a destination object.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="destination">The destination.</param>
    public static void CopyProperties(this object source, object destination)
    {
        // If any this null throw an exception
        if (source == null || destination == null)
        {
            throw new Exception("Source or/and Destination Objects are null");
        }

        // Getting the Types of the objects
        Type typeDest = destination.GetType();
        Type typeSource = source.GetType();

        // Collect all the valid properties to map
        var results = from sourceProperty in typeSource.GetProperties()
                      let destProperty = typeDest.GetProperty(sourceProperty.Name)
                      where sourceProperty.CanRead
                      && destProperty != null
                      && destProperty.GetSetMethod(true) != null && !destProperty.GetSetMethod(true).IsPrivate
                      && (destProperty.GetSetMethod().Attributes & MethodAttributes.Static) == 0
                      && destProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType)
                      select new { sourceProperty, destProperty };

        // map the properties.
        foreach (var props in results)
        {
            props.destProperty.SetValue(destination, props.sourceProperty.GetValue(source, null), null);
        }
    }
}