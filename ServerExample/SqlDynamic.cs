using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TrackingLibrary;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Dynamic;
using System.Linq;
using System.Data;
using System.Collections;
using System.ComponentModel;

namespace ServerExample
{
    /// <summary>
    /// Provides service for SQL query generation.
    /// </summary>
    public static class SqlDynamic
    {

        #region INSERT

        /// <summary>
        /// Creates SQL command for inserting specific <see cref="ExpandoObject"/>.
        /// The object must be flattened!
        /// </summary>
        /// <param name="tableName">Table name to insert in.</param>
        /// <param name="obj">Flattened object to insert.</param>
        /// <param name="db">Db connection to work with. Must be opened before calling this!</param>
        /// <returns>Command for Inserting this object. Must be executed manually!</returns>
        public static SqlCommand GetInsert(string tableName, IDictionary<string, object> obj, SqlConnection db)
        {
            // Create Insert command
            string command = GetInsertCommand(tableName, obj);

            // Populate its parameters
            var c = db.CreateCommand();
            foreach (var prop in obj)
            {
                c.Parameters.Add(new SqlParameter($"@{prop}", prop.Value));
            }

            return c;
        }

        /// <summary>
        /// Generates SQL INSERT command for this object.
        /// All the values are replaced with @params
        /// </summary>
        /// <param name="tableName">Table to which it must be inserted.</param>
        /// <param name="props">Flattened object.</param>
        /// <returns>String with the command.</returns>
        private static string GetInsertCommand(string tableName, IDictionary<string, object> props)
        {
            var sb = new StringBuilder();

            sb.Append($"INSERT INTO ${tableName}(");

            bool isFirst = true;
            foreach (var prop in props.Keys)
            {
                if (!isFirst)
                {
                    sb.Append(',');
                }
                else
                {
                    isFirst = false;
                }
                sb.Append(prop);
            }

            isFirst = true;
            sb.Append(") VALUES(");

            foreach (var prop in props)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(',');
                }
                sb.Append('@');
                sb.Append(prop);
            }

            sb.Append(")");

            return sb.ToString();
        }

        #endregion INSERT

        /// <summary>
        /// Converts the object to the flatten object with values-types of the
        /// initial object's properties.
        /// </summary>
        /// <param name="obj">Object to be flattened.</param>
        public static IDictionary<string, Type> GetObjectScheme(object obj)
        {
            return Flatten(obj)
                .ToDictionary(k => k.Key, v => v.Value.GetType());
        }

        /// <summary>
        /// Flattens the object, in other words, makes all its properties
        /// and their subproperties (recursively) to be on the same level.
        /// </summary>
        /// <param name="obj">Object to be flattened.</param>
        /// <returns>Simply it's the call of <see cref="Flatten(object)"/>
        /// but converted to the dictionary instead of the collection
        /// of key-value properties.</returns>
        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            return Flatten(obj).ToDictionary(
                k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Flattens the object, in other words, makes all its properties
        /// and their subproperties (recursively) to be on the same level.
        /// </summary>
        /// <param name="obj">Object to be flattened.</param>
        public static IEnumerable<KeyValuePair<string, object>> Flatten(object obj)
        {
            // If this is a collection
            if (obj is IEnumerable en && !(obj is ExpandoObject))
            {
                // Enumerate it
                int i = 0;
                foreach (var el in en)
                {
                    // If it's the simple element
                    if (el == null || IsSupportedType(el.GetType()))
                    {
                        // Just return it
                        yield return new KeyValuePair<string, object>((i++).ToString(), el);
                    }
                    // Complex object
                    else
                    {
                        // Flatten it
                        foreach (var elProp in Flatten(el))
                        {
                            // And return its properties
                            yield return new KeyValuePair<string, object>(
                                $"{i++}[{elProp.Key}]", elProp.Value);
                        }
                    }
                }

                yield break;
            }

            // If this is not a dictionary 
            if (!(obj is ExpandoObject))
            {
                // make it a dictionary.
                // If you want, you can optimize this
                // part of code, but I'll leave it this way
                // in order to KISS
                obj = ToExpandoObject(obj);
            }

            // Enumerate all the properties
            foreach (var prop in obj as IDictionary<string, object>)
            {
                // If the property is a simple type
                var el = prop.Value;
                if (el == null || IsSupportedType(el.GetType()))
                {
                    // Leave it
                    yield return prop;
                }
                // The property is a complex type
                else
                {
                    // Flatten it recursively
                    foreach (var elProp in Flatten(el))
                    {
                        // Return its properties as the properties of this object
                        yield return new KeyValuePair<string, object>(
                            $"{prop.Key}[{elProp.Key}]", elProp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Converts anonymous or any other type
        /// to the dictionary with properties.
        /// </summary>
        private static ExpandoObject ToExpandoObject(this object obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj.GetType()))
            {
                expando.Add(property.Name, property.GetValue(obj));
            }

            return (ExpandoObject)expando;
        }

        /// <summary>
        /// Generates create table SQL command.
        /// </summary>
        /// <param name="name">Name of the table to create.</param>
        /// <param name="idCol">PK column name.</param>
        /// <param name="scheme">Scheme of the table to be created.</param>
        /// <returns></returns>
        public static string CreateTableCommand(string name, string idCol, IDictionary<string, Type> scheme)
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE \"{name}\" (\n");
            sb.Append($"{idCol} int PRIMARY KEY");

            foreach (string prop in scheme.Keys)
            {
                sb.Append($",\n\"{prop}\" {TypeToSQL(scheme[prop])}");
            }

            sb.Append(");");

            return sb.ToString();
        }

        /// <summary>
        /// Checks whether this type can be used in SQL.
        /// </summary>
        private static bool IsSupportedType(Type t)
        {
            return (t == typeof(string)
                || (t == typeof(int))
                || (t == typeof(float))
                || (t == typeof(double))
                || (t == typeof(long))
                || (t == typeof(bool))
                || (t == typeof(short))
                || (t == typeof(DateTime))
                || (t == typeof(TimeSpan))
                || (t == typeof(decimal))
                || (t == typeof(byte[]))
                || (t == typeof(char[])));
        }

        /// <summary>
        /// Converts .Net type to the name of the
        /// associated SQL type.
        /// </summary>
        private static string TypeToSQL(Type t)
        {
            if (t == typeof(string))
            {
                return "nvarchar(MAX)";
            }
            if (t == typeof(int))
            {
                return "int";
            }
            if (t == typeof(float))
            {
                return "real";
            }
            if (t == typeof(double))
            {
                return "float";
            }
            if (t == typeof(long))
            {
                return "bigInt";
            }
            if (t == typeof(bool))
            {
                return "bit";
            }
            if (t == typeof(short))
            {
                return "smallInt";
            }
            if (t == typeof(DateTime))
            {
                return "dateTime";
            }
            if (t == typeof(TimeSpan))
            {
                return "time";
            }
            if (t == typeof(decimal)){
                return "decimal";
            }
            if (t == typeof(byte[]))
            {
                return "binary";
            }
            if (t == typeof(char[]))
            {
                return "nvarchar(MAX)";
            }

            return "nvarchar(MAX)";
        }

        /// <summary>
        /// Checks whether the given table exists in the db.
        /// </summary>
        /// <param name="tableName">Name of the table to check.</param>
        /// <param name="db">Database connetion which must be
        /// opened before calling this method!</param>
        /// <returns>Whether the table exists in the db.</returns>
        public static bool IsTableCreated(string tableName, SqlConnection db)
        {
            var command = db.CreateCommand();
            command.CommandText =
                   "SELECT 1 "
                 + "FROM INFORMATION_SCHEMA.TABLES "
                 + "WHERE TABLE_NAME = '@name';";

            command.Parameters.Add("@name", System.Data.SqlDbType.NVarChar);

            command.Parameters["@name"].Value = tableName;

            return command.ExecuteScalar() != null;
        }
    }
}
