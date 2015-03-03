﻿using Samotorcan.HtmlUi.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Samotorcan.HtmlUi.Core
{
    /// <summary>
    /// Controller. TODO: create attributes like controller name, camel case ...
    /// </summary>
    public abstract class Controller : IDisposable
    {
        #region Properties
        #region Public

        #region Id
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; private set; }
        #endregion

        #endregion
        #region Private

        #region Properties
        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        private List<ControllerPropertyInfo> Properties { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Gets or sets the methods.
        /// </summary>
        /// <value>
        /// The methods.
        /// </value>
        private List<ControllerMethodInfo> Methods { get; set; }
        #endregion
        #region Name
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        private string Name { get; set; }
        #endregion
        #region Type
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        private Type Type { get; set; }
        #endregion
        #region IgnoreProperties
        /// <summary>
        /// Gets or sets the ignore properties.
        /// </summary>
        /// <value>
        /// The ignore properties.
        /// </value>
        private List<string> IgnoreProperties { get; set; }
        #endregion

        #endregion
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Controller"/> class.
        /// </summary>
        protected Controller(int id)
        {
            Id = id;

            Properties = new List<ControllerPropertyInfo>();
            Methods = new List<ControllerMethodInfo>();
            IgnoreProperties = new List<string> // TODO: create attributes
            {
                "Id"
            };
            Type = GetType();
            Name = Type.Name;

            Properties = FindProperties(Type);
            Methods = FindMethods(Type);
        }

        #endregion
        #region Methods
        #region Internal

        #region GetProperties
        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="access">The access.</param>
        /// <param name="naming">The naming.</param>
        /// <returns></returns>
        private List<ControllerProperty> GetProperties(Access? access, Naming naming)
        {
            return Properties.Where(p => access == null || p.Access.HasFlag(access.Value))
                .Select(p => new ControllerProperty
                {
                    Name = naming == Naming.CamelCase ? StringUtility.CamelCase(p.Name) : p.Name,
                    Value = p.Access.HasFlag(Access.Read) ? p.PropertyInfo.GetValue(this, null) : null,
                    Access = p.Access
                })
                .ToList();
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="access">The access.</param>
        /// <param name="naming">The naming.</param>
        /// <returns></returns>
        internal List<ControllerProperty> GetProperties(Access access, Naming naming)
        {
            return GetProperties((Access?)access, naming);
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <param name="naming">The naming.</param>
        /// <returns></returns>
        internal List<ControllerProperty> GetProperties(Naming naming)
        {
            return GetProperties(null, naming);
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <returns></returns>
        internal List<ControllerProperty> GetProperties()
        {
            return GetProperties(null, Naming.Normal);
        }
        #endregion
        #region GetMethods
        /// <summary>
        /// Gets the methods.
        /// </summary>
        /// <param name="naming">The naming.</param>
        /// <returns></returns>
        internal List<ControllerMethod> GetMethods(Naming naming)
        {
            return Methods.Select(m => new ControllerMethod
                {
                    Name = naming == Naming.CamelCase
                        ? StringUtility.CamelCase(m.Name)
                        : m.Name
                })
                .GroupBy(m => m.Name)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Gets the methods.
        /// </summary>
        /// <returns></returns>
        internal List<ControllerMethod> GetMethods()
        {
            return GetMethods(Naming.CamelCase);
        }
        #endregion
        #region GetDescription
        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <param name="naming">The naming.</param>
        /// <returns></returns>
        internal ControllerDescription GetDescription(Naming naming)
        {
            return new ControllerDescription
            {
                Name = Name,
                Properties = GetProperties(naming),
                Methods = GetMethods(naming)
            };
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <returns></returns>
        internal ControllerDescription GetDescription()
        {
            return GetDescription(Naming.CamelCase);
        }
        #endregion
        #region TrySetProperty
        /// <summary>
        /// Tries to set property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        /// <param name="propertyNameType">Type of the property name.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">propertyName</exception>
        internal bool TrySetProperty(string propertyName, object value, Naming propertyNameType)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException("propertyName");

            var property = Properties.FirstOrDefault(p => p.Access.HasFlag(Access.Write) &&
                (propertyNameType == Naming.CamelCase
                    ? StringUtility.CamelCase(p.Name) == propertyName
                    : p.Name == propertyName));

            if (property != null)
            {
                var propertyInfo = property.PropertyInfo;
                var propertyType = propertyInfo.PropertyType;

                if (value == null)
                {
                    if (!propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null)
                    {
                        propertyInfo.SetValue(this, value);
                        return true;
                    }
                }
                else
                {
                    var valueType = value.GetType();

                    if (valueType == propertyType)
                    {
                        propertyInfo.SetValue(this, value);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to set property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        internal bool TrySetProperty(string propertyName, object value)
        {
            return TrySetProperty(propertyName, value, Naming.CamelCase);
        }
        #endregion

        #endregion
        #region Private

        #region FindProperties
        /// <summary>
        /// Finds the properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private List<ControllerPropertyInfo> FindProperties(Type type)
        {
            var properties = new List<ControllerPropertyInfo>();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!IgnoreProperties.Contains(property.Name))
                {
                    var readAccess = property.CanRead && property.GetGetMethod(false) != null;
                    var writeAccess = property.CanWrite && property.GetSetMethod(false) != null;
                    Access? access = null;

                    // access
                    if (readAccess && writeAccess)
                        access = Access.Read | Access.Write;
                    else if (readAccess)
                        access = Access.Read;
                    else if (writeAccess)
                        access = Access.Write;

                    // add
                    if (access != null)
                        properties.Add(new ControllerPropertyInfo { Name = property.Name, PropertyInfo = property, Access = access.Value });
                }
            }

            return properties;
        }
        #endregion
        #region FindMethods
        /// <summary>
        /// Finds the methods.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private List<ControllerMethodInfo> FindMethods(Type type)
        {
            var methods = new List<ControllerMethodInfo>();

            while (type != typeof(object))
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!method.IsSpecialName && method.Name != "Dispose")  // TODO: ignore attribute
                        methods.Add(new ControllerMethodInfo { Name = method.Name, MethodInfo = method });
                }

                type = type.BaseType;
            }

            return methods;
        }
        #endregion

        #endregion
        #endregion
        #region Data

        #region ControllerPropertyInfo
        /// <summary>
        /// Controller property info.
        /// </summary>
        private class ControllerPropertyInfo : ControllerProperty
        {
            /// <summary>
            /// Gets or sets the property info.
            /// </summary>
            /// <value>
            /// The property info.
            /// </value>
            public PropertyInfo PropertyInfo { get; set; }
        }
        #endregion
        #region ControllerMethodInfo
        /// <summary>
        /// Controller method info.
        /// </summary>
        private class ControllerMethodInfo : ControllerMethod
        {
            /// <summary>
            /// Gets or sets the method information.
            /// </summary>
            /// <value>
            /// The method information.
            /// </value>
            public MethodInfo MethodInfo { get; set; }
        }
        #endregion

        #endregion

        #region IDisposable

        /// <summary>
        /// Was dispose already called.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
