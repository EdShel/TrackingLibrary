using CsvHelper;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;

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
            return DynamicHelper.ToXml(obj, "event").ToString();
        }

        /// <summary>
        /// Serializes to Json.
        /// </summary>
        public static string SerializeJson(object obj)
        {
            return JObject.FromObject(obj).ToString();
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
                    csv.WriteRecords(new[] { obj });

                    return writer.ToString();
                }
            }
        }
    }
}
