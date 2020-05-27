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
    public static class SqlDynamic
    {
        const string ID_PROP_NAME = "EventRecordId";

        #region INSERT

        public static SqlCommand GetInsert(string command, IDictionary<string, object> obj, SqlConnection db)
        {
            var c = db.CreateCommand();

            foreach (var prop in obj)
            {
                c.Parameters.Add(new SqlParameter($"@{prop}", prop.Value));
            }

            return c;
        }

        public static string GetInsertCommand(string tableName, IDictionary<string, object> props)
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

        public static IDictionary<string, Type> GetObjectScheme(object obj)
        {
            return Flatten(obj)
                .ToDictionary(k => k.Key, v => v.Value.GetType());
        }

        public static IDictionary<string, object> ToDictionary(this object obj)
        {
            return Flatten(obj).ToDictionary(
                k => k.Key, v => v.Value);
        }

        public static IEnumerable<KeyValuePair<string, object>> Flatten(object obj)
        {
            if (obj is KeyValuePair<string, object> pair)
            {
                yield return pair;
                yield break;
            }

            if (obj is IEnumerable en && !(obj is ExpandoObject))
            {
                int i = 0;
                foreach (var el in en)
                {
                    if (el == null || IsSupportedType(el.GetType()))
                    {
                        yield return new KeyValuePair<string, object>((i++).ToString(), el);
                    }
                    else
                    {
                        foreach (var elProp in Flatten(el))
                        {
                            yield return new KeyValuePair<string, object>(
                                $"{i++}[{elProp.Key}]", elProp.Value);
                        }
                    }
                }

                yield break;
            }

            if (!(obj is ExpandoObject))
            {
                obj = ToExpandoObject(obj);
            }

            foreach (var prop in obj as IDictionary<string, object>)
            {
                var el = prop.Value;
                if (el == null || IsSupportedType(el.GetType()))
                {
                    yield return prop;
                }
                else
                {
                    foreach (var elProp in Flatten(el))
                    {
                        yield return new KeyValuePair<string, object>(
                            $"{prop.Key}[{elProp.Key}]", elProp.Value);
                    }
                }
            }
        }

        private static ExpandoObject ToExpandoObject(this object obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj.GetType()))
            {
                expando.Add(property.Name, property.GetValue(obj));
            }

            return (ExpandoObject)expando;
        }

        public static string CreateTableCommand(string name, IDictionary<string, Type> scheme)
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE \"{name}\" (\n");
            sb.Append($"{ID_PROP_NAME} int PRIMARY KEY");

            foreach (string prop in scheme.Keys)
            {
                sb.Append($",\n\"{prop}\" {TypeToSQL(scheme[prop])}");
            }

            sb.Append(");");

            return sb.ToString();
        }

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
