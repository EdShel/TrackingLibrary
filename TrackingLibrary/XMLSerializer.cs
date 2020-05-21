using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace TrackingLibrary
{
    /*
            The following code is stolen from
            gist.github.com/martinnormark/2574972
    */

    /// <summary>
    /// Extension methods for the dynamic object.
    /// </summary>
    internal static class DynamicHelper
    {
        /// <summary>
        /// Defines the simple types that is directly writeable to XML.
        /// </summary>
        private static readonly Type[] _writeTypes = new[] { typeof(string), typeof(DateTime), typeof(Enum), typeof(decimal), typeof(Guid) };

        /// <summary>
        /// Determines whether [is simple type] [the specified type].
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// 	<c>true</c> if [is simple type] [the specified type]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSimpleType(this Type type)
        {
            return type.IsPrimitive || _writeTypes.Contains(type);
        }

        public static XElement ToXML(this object obj, string name)
        {
            bool isDynamic = obj is ExpandoObject;
            if (isDynamic)
            {
                return DynamicToXml(obj, name);
            }

            return AnonymousToXML(obj, name);
        }

        /// <summary>
        /// Converts the specified dynamic object to XML.
        /// </summary>
        /// <param name="dynamicObject">The dynamic object.</param>
        /// /// <param name="objectName">The element name.</param>
        /// <returns>Returns an Xml representation of the dynamic object.</returns>
        private static XElement DynamicToXml(dynamic dynamicObject, string objectName)
        {
            if (String.IsNullOrWhiteSpace(objectName))
            {
                objectName = "object";
            }

            objectName = XmlConvert.EncodeName(objectName);
            XElement ret = new XElement(objectName);


            IDictionary<string, object> members = (IDictionary<string, object>)dynamicObject;

            List<XElement> elements = new List<XElement>();
            foreach (var prop in members)
            {
                var name = XmlConvert.EncodeName(prop.Key);
                var val = prop.Value.GetType().IsArray ? "array" : prop.Value;
                var value = prop.Value.GetType().IsArray ? GetArrayElement(prop.Key, (Array)prop.Value) : (prop.Value.GetType().IsSimpleType() ? new XElement(name, val) : val.ToXML(name));
                if (value != null)
                {
                    elements.Add(value);
                }
            }

            ret.Add(elements);

            return ret;
        }

        /// <summary>
        /// Converts an anonymous type to an XElement.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="objName">The element name.</param>
        /// <returns>Returns the object as it's XML representation in an XElement.</returns>
        private static XElement AnonymousToXML(this object input, string objName)
        {
            if (input == null)
            {
                return null;
            }

            if (String.IsNullOrWhiteSpace(objName))
            {
                objName = "object";
            }

            objName = XmlConvert.EncodeName(objName);
            var ret = new XElement(objName);

            if (input != null)
            {
                var type = input.GetType();
                var props = type.GetProperties();

                var elements = new List<XElement>();
                foreach (var prop in props)
                {
                    var name = XmlConvert.EncodeName(prop.Name);
                    var val = prop.PropertyType.IsArray ? "array" : prop.GetValue(input, null);
                    var value = prop.PropertyType.IsArray
                     ? GetArrayElement(prop, (Array)prop.GetValue(input, null))
                     : (prop.PropertyType.IsSimpleType() ? new XElement(name, val) : val.ToXML(name));

                    if (value != null)
                    {
                        elements.Add(value);
                    }
                }

                ret.Add(elements);
            }

            return ret;
        }
        
        /// <summary>
        /// Gets the array element.
        /// </summary>
        /// <param name="info">The property info.</param>
        /// <param name="input">The input object.</param>
        /// <returns>Returns an XElement with the array collection as child elements.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static XElement GetArrayElement(PropertyInfo info, Array input)
        {
            return GetArrayElement(info.Name, input);
        }

        /// <summary>
        /// Gets the array element.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="input">The input object.</param>
        /// <returns>Returns an XElement with the array collection as child elements.</returns>
        private static XElement GetArrayElement(string propertyName, Array input)
        {
            var name = XmlConvert.EncodeName(propertyName);

            XElement rootElement = new XElement(name);

            var arrayCount = input.GetLength(0);

            for (int i = 0; i < arrayCount; i++)
            {
                var val = input.GetValue(i);
                XElement childElement = val.GetType().IsSimpleType() ? new XElement(name + "Child", val) : val.ToXML(null);

                rootElement.Add(childElement);
            }

            return rootElement;
        }
    }
}
