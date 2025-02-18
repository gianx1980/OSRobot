/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/

using System.Reflection;
using System.Text.RegularExpressions;

namespace OSRobot.Server.Core.Persistence;

static class XmlCommon
{
    public const string KeyContainerName = "OSRobotCryptoKey3";

    public const string ObjectTagName = "Object";
    public const string ObjectsTagName = "Objects";
    public const string ItemTagName = "Item";
    public const string ItemsTagName = "Items";
    public const string ReferenceTagName = "Reference";
    public const string ReferencesTagName = "References";
    public const string KeyTagName = "Key";
    public const string ValueTagName = "Value";
    public const string ColumnsDefinitionTagName = "ColumnsDefinition";
    public const string ColumnTagName = "Column";
    public const string NameTagName = "Name";
    public const string TypeTagName = "Type";
    public const string RowsTagName = "Rows";
    public const string RowTagName = "Row";

    public const string RefIDAttributeName = "RefID";
    public const string TypeAttributeName = "Type";
    public const string RefTypeIDAttributeName = "RefTypeID";
    public const string RootObjectAttributeName = "RootObject";
    public const string EncryptedAttributeName = "Encrypted";
    public const string IsNullAttributeName = "IsNull";


    public static bool Field_IsNull(object objectRef, FieldInfo fieldInfo)
    {
        return (fieldInfo.GetValue(objectRef) == null);
    }

    public static bool Field_IsEvent(FieldInfo fieldInfo)
    {
        return (fieldInfo.FieldType.BaseType == typeof(MulticastDelegate));
    }

    public static bool Field_IsList(FieldInfo fieldInfo)
    {
        return (fieldInfo.FieldType.IsClass && fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)));
    }

    public static bool Field_IsPtr(FieldInfo fieldInfo)
    {
        return (fieldInfo.FieldType == typeof(IntPtr) || fieldInfo.FieldType == typeof(UIntPtr));
    }

    public static bool Field_HasAttribute(FieldInfo fieldInfo, Type attribute)
    {
        return fieldInfo.CustomAttributes.Where(A => A.AttributeType == attribute).FirstOrDefault() != null;
    }

    public static bool Type_IsList(Type objectType)
    {
        return (objectType.IsClass && objectType.IsGenericType && objectType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)));
    }

    public static bool Type_IsDictionary(Type objectType)
    {
        return (objectType.IsClass && objectType.IsGenericType && objectType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>)));
    }

    public static string Field_GetFieldName(FieldInfo fieldInfo)
    {
        // Take care of automatic properties! (<{0}>k__BackingField)
        Match mt = Regex.Match(fieldInfo.Name, @"^\<(\w+)\>k__BackingField$");

        if (mt.Success)
            return mt.Groups[1].Value;
        else
            return fieldInfo.Name;
    }

    public static string Field_GetAutomaticPropFieldName(string propertyName)
    {
        return $"<{propertyName}>k__BackingField";
    }

    public static bool Property_IsNull(object objectRef, PropertyInfo propertyInfo)
    {
        return (propertyInfo.GetValue(objectRef) == null);
    }

    public static bool Property_HasAttribute(PropertyInfo fieldInfo, Type attribute)
    {
        return fieldInfo.CustomAttributes.Where(A => A.AttributeType == attribute).FirstOrDefault() != null;
    }
}
