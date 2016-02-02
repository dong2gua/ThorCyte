﻿namespace Mvvm
{
    using System;
    using System.ComponentModel;
    using System.Linq.Expressions;

    /// <summary>
    /// Generic extension methods used by the framework.
    /// </summary>
    public static class PropertyChangedExtensionMethods
    {
        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        public static void Raise<T, P>(this PropertyChangedEventHandler pc, T source, Expression<Func<T, P>> pe)
        {
            if (pc != null)
            {
                pc.Invoke(source,
                    new PropertyChangedEventArgs(((MemberExpression)pe.Body).Member.Name));
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event for all properties.
        /// </summary>
        public static void RaiseAll<T>(this PropertyChangedEventHandler pc, T source)
        {
            if (pc != null)
            {
                pc.Invoke(source, new PropertyChangedEventArgs(string.Empty));
            }
        }
    }
}
