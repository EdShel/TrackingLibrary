using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TrackingLibrary
{
    /// <summary>
    /// Type of serialization.
    /// </summary>
    public enum Serialization
    {
        /// <summary>
        /// Comma separated values
        /// </summary>
        CSV,

        /// <summary>
        /// JavaScript Object Notation
        /// </summary>
        Json,

        /// <summary>
        /// Extensible Markup language
        /// </summary>
        XML
    }

    /// <summary>
    /// Provides service for serialization of dynamic objects.
    /// </summary>
    public static class ObjectSerializer
    {
        private static CultureInfo culture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Serializes an object to the specified format.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="serialization">How to do it.</param>
        public static string Serialize(object obj, Serialization serialization)
        {
            switch (serialization)
            {
                case Serialization.CSV:
                    return SerializeCSV(obj);
                case Serialization.Json:
                    return SerializeJson(obj);
                case Serialization.XML:
                    return SerializeXML(obj);
                default: throw new System.Exception("Unknown serialization!");
            }
        }

        /// <summary>
        /// Deserializes events as the COLLECTION of the dynamic objects.
        /// </summary>
        /// <param name="str">String with the serialized events.
        /// It has to be a collection!!!</param>
        /// <param name="serialization">How to do it.</param>
        /// <returns>Collection of dynamic objects.</returns>
        public static IEnumerable<object> Deserialize(string str, Serialization serialization)
        {
            switch (serialization)
            {
                case Serialization.CSV:
                    return DeserializeCSV(str);
                case Serialization.Json:
                    return DeserializeJson(str);
                case Serialization.XML:
                    return DeserializeXML(str);
                default: throw new System.Exception("Unknown serialization!");
            }
        }

        /// <summary>
        /// Serializes to xml.
        /// </summary>
        /// <returns>XML array of events.</returns>
        public static string SerializeXML(object obj)
        {
            if (obj is IEnumerable col)
            {
                return XMLSerializer.ToXML(col, "eventsArray").ToString();
            }
            return XMLSerializer.ToXML(new object[] { obj }, "eventsArray").ToString();
        }

        /// <summary>
        /// Deserialized from xml collection.
        /// </summary>
        /// <param name="xml">XML array of events.</param>
        public static IEnumerable<object> DeserializeXML(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            string jsonText = JsonConvert.SerializeXNode(doc, Formatting.None, true);
            var prop = ExtractProperty(JsonConvert.DeserializeObject<ExpandoObject>(jsonText));
            if (prop is IDictionary<string, object> d)
            {
                prop = new object[] { d };
            }

            return prop as IEnumerable<object>;
        }

        /// <summary>
        /// Serializes to Json array.
        /// </summary>
        /// <returns>Json array of events.</returns>
        public static string SerializeJson(object obj)
        {
            if (obj is IEnumerable)
            {
                return JArray.FromObject(obj).ToString();
            }
            return JArray.FromObject(new object[] { obj }).ToString();
        }

        /// <summary>
        /// Deserializes from Json array.
        /// </summary>
        /// <param name="json">Json array string.</param>
        public static IEnumerable<object> DeserializeJson(string json)
        {
            var o = JsonConvert.DeserializeObject<ExpandoObject[]>(json);
            return o;
        }

        /// <summary>
        /// Serializes to CSV.
        /// </summary>
        public static string SerializeCSV(object obj)
        {
            using (var writer = new StringWriter())
            {
                using (var csv = new CsvWriter(writer, culture))
                {
                    IEnumerable records = obj is IEnumerable col
                        ? col
                        : new[] { obj };

                    csv.WriteRecords(records);

                    return writer.ToString();
                }
            }
        }

        /// <summary>
        /// Deserializes from CSV.
        /// </summary>
        public static IEnumerable<object> DeserializeCSV(string csv)
        {
            using (var reader = new StringReader(csv))
            {
                using (var r = new CsvReader(reader, culture))
                {
                    while (r.Read())
                    {
                        yield return r.GetRecord<dynamic>();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the single property of the object
        /// no matter how deep it is.
        /// E.g. if the object has only 1 property,
        /// the property will be taken from this property.
        /// This methods simply extracts objects from a chain
        /// of objects with the single property.
        /// </summary>
        /// <param name="obj">Object whose properties to extract.</param>
        private static object ExtractProperty(IDictionary<string, object> obj)
        {
            while(obj.Keys.Count == 1)
            {
                var propVal = obj.Values.First();
                if (propVal is IDictionary<string, object> dict)
                {
                    obj = dict;
                }
                else
                {
                    return propVal;
                }
            }
            return obj;
        }
    }
}
