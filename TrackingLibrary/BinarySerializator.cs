using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TrackingLibrary
{
    /// <summary>
    /// Writes and reads specified object using binary reader.
    /// Combines primitive types (enums and IEnumerable too)
    /// and creates ExpandoObject.
    /// </summary>
    public static class BinarySerializator
    {
        /// <summary>
        /// Writes specified object using binary writer.
        /// </summary>
        public static void Serialize(this BinaryWriter wr, object obj)
        {
            var type = obj.GetType();
            wr.Write(type.FullName);

            // WRITING PRIMITIVE TYPES
            if (type.IsPrimitive)
            {
                WritePrimitiveType(wr, obj);
            }
            // STRING
            else if (type == typeof(string))
            {
                wr.Write((string)obj);
            }
            // WRITE ENUMS
            else if (type.IsEnum)
            {
                wr.Write((int)obj);
            }
            // IEnumerable types
            else if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
            {
                wr.WriteCollection(obj);
            }
            // Complex type
            else
            {
                WriteAnonymousObject(wr, obj);
            }
        }

        /// <summary>
        /// Reads object from the reader.
        /// </summary>
        public static dynamic Deserialize(this BinaryReader rd)
        {
            string typeName = rd.ReadString();
            Type type = Type.GetType(typeName);

            if (type != null)
            {
                // READING PRIMITIVE TYPES
                if (type.IsPrimitive)
                {
                    return ReadPrimitiveType(rd, type);
                }
                else if (type == typeof(string))
                {
                    return rd.ReadString();
                }
                // WRITE ENUMS
                else if (type.IsEnum)
                {
                    return Enum.ToObject(type, rd.ReadInt32());
                }
                // IEnumerable types and arrays
                else if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
                {
                    return rd.ReadCollection();
                }
            }

            // Complex type
            return ReadAnonymousObject(rd);
        }

        #region PRIMITIVE_TYPES_SERIALIZATION

        /// <summary>
        /// Writes given primitive type.
        /// If it isn't supported, throws NotSupportedException.
        /// </summary>
        private static void WritePrimitiveType(this BinaryWriter wr, object obj)
        {
            Type type = obj.GetType();

            // 1 - INT & 2 - enums
            if (type == typeof(int))
                wr.Write((int)obj);
            // 3 - FLOAT
            else if (type == typeof(float))
                wr.Write((float)obj);
            // 4 - DOUBLE
            else if (type == typeof(double))
                wr.Write((double)obj);
            // 5 - BOOL
            else if (type == typeof(bool))
                wr.Write((bool)obj);
            // 6 - BYTE
            else if (type == typeof(byte))
                wr.Write((byte)obj);
            // 7 - LONG
            else if (type == typeof(long))
                wr.Write((long)obj);
            // 8 - CHAR
            else if (type == typeof(char))
                wr.Write((char)obj);
            // 9 - SHORT
            else if (type == typeof(short))
                wr.Write((short)obj);
            // 10 - UINT
            else if (type == typeof(uint))
                wr.Write((uint)obj);
            // 11 - SBYTE
            else if (type == typeof(sbyte))
                wr.Write((sbyte)obj);
            // 12 - ULONG
            else if (type == typeof(ulong))
                wr.Write((ulong)obj);
            // 13 - USHORT
            else if (type == typeof(ushort))
                wr.Write((ushort)obj);
            // 14 - DECIMAL
            else if (type == typeof(decimal))
                wr.Write((decimal)obj);
            else
                throw new NotSupportedException(
                    "Given primitive type isn't supported!");
        }

        /// <summary>
        /// Reads primitve type. 
        /// If it isn't supported, throws NotSupportedException.
        /// </summary>
        /// <returns></returns>
        private static dynamic ReadPrimitiveType(this BinaryReader rd, Type type)
        {
            // 1 - INT
            if (type == typeof(int))
                return rd.ReadInt32();
            // 3 - FLOAT
            else if (type == typeof(float))
                return rd.ReadSingle();
            // 4 - DOUBLE
            else if (type == typeof(double))
                return rd.ReadDouble();
            // 5- BOOL
            else if (type == typeof(bool))
                return rd.ReadBoolean();
            // 6 - BYTE
            else if (type == typeof(byte))
                return rd.ReadByte();
            // 7 - LONG
            else if (type == typeof(long))
                return rd.ReadInt64();
            // 8 - CHAR
            else if (type == typeof(char))
                return rd.ReadChar();
            // 9 - SHORT
            else if (type == typeof(short))
                return rd.ReadInt16();
            // 10 - UINT
            else if (type == typeof(uint))
                return rd.ReadUInt32();
            // 11 - SBYTE
            else if (type == typeof(sbyte))
                return rd.ReadSByte();
            // 12 - ULONG
            else if (type == typeof(ulong))
                return rd.ReadUInt64();
            // 13 - USHORT
            else if (type == typeof(ushort))
                return rd.ReadUInt16();
            // 14 - DECIMAL
            else if (type == typeof(decimal))
                return rd.ReadDecimal();

            throw new NotSupportedException($"Unknown primitive type {type.Name}");
        }

        #endregion PRIMITIVE_TYPES_SERIALIZATION

        #region ANONYMOUS_OBJECT_SERIALIZATION

        /// <summary>
        /// Writes complex objects by serializing all its fields and properties
        /// </summary>
        private static void WriteAnonymousObject(this BinaryWriter wr, object obj)
        {
            Type type = obj.GetType();

            // Write all the members
            MemberInfo[] members = type.GetFields()
                .Cast<MemberInfo>()
                .Union(
                    type.GetProperties()
                    .Cast<MemberInfo>())
                .ToArray();

            // Write their count
            wr.Write(members.Length);

            foreach (MemberInfo member in members)
            {
                // Write name of the member
                wr.Write(member.Name);

                // Try to write field's value
                if (member is FieldInfo field)
                {
                    Serialize(wr, field.GetValue(obj));
                }
                // Try to write property's value
                else if (member is PropertyInfo property)
                {
                    Serialize(wr, property.GetValue(obj));
                }
            }
        }

        /// <summary>
        /// Reads complex object by deserializing its fields and properties.
        /// </summary>
        private static dynamic ReadAnonymousObject(this BinaryReader rd)
        {
            // Suppose that this object is anonymous
            var obj = new ExpandoObject() as IDictionary<string, object>;

            // Go through all field members
            int fields = rd.ReadInt32();
            for (int i = 0; i < fields; ++i)
            {
                string fieldName = rd.ReadString();

                obj.Add(fieldName, Deserialize(rd));
            }

            return obj;
        }

        #endregion ANONYMOUS_OBJECT_SERIALIZATION

        #region COLLECTIONS_SERIALIZATION

        /// <summary>
        /// Writes given collection by serializing its enumerated elemenets.
        /// </summary>
        private static void WriteCollection(this BinaryWriter wr, object obj)
        {
            Type type = obj.GetType();
            var collection = (obj as IEnumerable).Cast<object>();

            // Write type of the member
            wr.Write(typeof(object).FullName);

            // Write count of objects
            int countOfObjs = collection.Count();
            wr.Write(countOfObjs);

            // Write those objects
            foreach (var el in collection)
            {
                Serialize(wr, el);
            }
        }

        /// <summary>
        /// Reads given collection by deserializing its enumerated elemenets.
        /// </summary>
        private static dynamic ReadCollection(this BinaryReader rd)
        {
            // Type of the member collection
            var member = Type.GetType(rd.ReadString());

            // How many objects is in the collection
            int countOfObjs = rd.ReadInt32();

            //var member = type.GetElementType();

            var array = Array.CreateInstance(member, countOfObjs);

            for (int i = 0; i < countOfObjs; ++i)
            {
                array.SetValue(Deserialize(rd), i);
            }
            return array;
        }

        #endregion COLLECTIONS_SERIALIZATION

    }
}
