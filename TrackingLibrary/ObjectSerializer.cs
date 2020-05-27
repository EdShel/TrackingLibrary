using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;

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
        /// Serializes to xml.
        /// </summary>
        public static string SerializeXML(object obj)
        {
            if (obj is IEnumerable col)
            {
                return XMLSerializer.ToXML(col.Cast<object>().ToArray(), "eventsArray").ToString();
            }
            return XMLSerializer.ToXML(obj, "event").ToString();
        }

        /// <summary>
        /// Serializes to Json.
        /// </summary>
        public static string SerializeJson(object obj)
        {
            if (obj is IEnumerable)
            {
                return JArray.FromObject(obj).ToString();
            }
            return JArray.FromObject(new object[] { obj }).ToString();
        }

        public static IEnumerable<object> DeserializeJson(string json)
        {
            //return (JArray.Parse(json) as IEnumerable)
            //    .Cast<object>();
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
    }
}
