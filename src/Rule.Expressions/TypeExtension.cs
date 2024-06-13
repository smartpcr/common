// -----------------------------------------------------------------------
// <copyright file="TypeExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using AutoFixture;
    using AutoFixture.Kernel;

    public static class TypeExtension
    {
        public static IEnumerable<MethodInfo> GetExtensionMethods(this Type extendedType)
        {
            var query = from type in extendedType.Assembly.GetTypes()
                where type.IsSealed && !type.IsGenericType && !type.IsNested
                from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                where method.IsDefined(typeof(ExtensionAttribute), false)
                where method.GetParameters()[0].ParameterType == extendedType
                select method;
            return query;
        }

        public static List<string> GetAllowedValues(this Type type)
        {
            if (type == typeof(bool) || Nullable.GetUnderlyingType(type) == typeof(bool))
            {
                return new List<string>(){"true", "false"};
            }

            if (type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true)
            {
                var enumType = Nullable.GetUnderlyingType(type) ?? type;
                return Enum.GetNames(enumType).Select(v => v.ToString()).ToList();
            }

            return new List<string>();
        }

        public static object? ConvertValue(this Type type, string value)
        {
            return TypeDescriptor.GetConverter(type).ConvertFrom(value);
        }

        public static bool IsNumericType(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsPrimitiveType(this Type type)
        {
            return type.IsPrimitive || type.IsValueType;
        }

        /// <summary>
        /// primitive type, value type and string, including nullable types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsScalarType(this Type type)
        {
            return type.IsPrimitiveType() || type == typeof(string) || Nullable.GetUnderlyingType(type)?.IsPrimitiveType() == true;
        }

        public static bool IsDateType(this Type type)
        {
            return Type.GetTypeCode(type) == TypeCode.DateTime || Nullable.GetUnderlyingType(type) == typeof(DateTime);
        }

        public static object? GetDefaultValue(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static object CreateDummyValue(this Type type)
        {
            Fixture fixture = new Fixture();
            return fixture.Create(type, new SpecimenContext(fixture));
        }
    }
}