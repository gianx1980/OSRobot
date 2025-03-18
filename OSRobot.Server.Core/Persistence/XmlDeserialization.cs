/*=/*======================================================================================
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

using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace OSRobot.Server.Core.Persistence;

public class XmlDeserialization
{
    private readonly XmlDocument _xmlDoc;
    private readonly Dictionary<int, XmlElement> _xmlObjects;
    private readonly XmlElement _xmlRootObject;
    private readonly Dictionary<int, object> _objectsDeserialized;
    private readonly Dictionary<int, string> _references;

    public bool CheckSerializeAttribute { get; set; }

    public XmlDeserialization(XmlDocument xmlDoc)
    {
        _xmlDoc = xmlDoc;

        XmlElement? tempRootOBject = (XmlElement?)_xmlDoc.SelectSingleNode($"//{XmlCommon.ObjectTagName}[@{XmlCommon.RootObjectAttributeName}='True']") ?? throw new ApplicationException("XmlDeserialization ctor: Cannot find root node.");
        _xmlRootObject = tempRootOBject;
        _xmlObjects = [];
        _objectsDeserialized = [];
        _references = [];
    }

    private bool CheckIfObjectExists(int objectID, out object? objectRef)
    {
        if (!_objectsDeserialized.TryGetValue(objectID, out object? value))
        {
            objectRef = null;
            return false;
        }
        else
        {
            objectRef = value;
            return true;
        }
    }

    private bool HasDefaultConstructor(Type objectType)
    {
        return objectType.IsValueType || objectType.GetConstructor(Type.EmptyTypes) != null;
    }

    private object? CreateObjectInstance(Type objectType)
    {
        if (objectType == typeof(string))
            return new string(new char[] { });
        else if (HasDefaultConstructor(objectType))
            return Activator.CreateInstance(objectType);
        else
            throw new ApplicationException($"Cannot create object instance of {objectType.FullName}");
    }

    private FieldInfo? FindField(FieldInfo[] fields, string fieldName)
    {
        return fields.Where(f => f.Name == fieldName || f.Name == XmlCommon.Field_GetAutomaticPropFieldName(fieldName)).FirstOrDefault();
    }

    private PropertyInfo? FindProperty(PropertyInfo[] properties, string propertyName)
    {
        return properties.Where(f => f.Name == propertyName).FirstOrDefault();
    }

    private Type? GetType(string refID)
    {
        string typeAssemblyName = _references[int.Parse(refID)];

        return Type.GetType(typeAssemblyName);
    }

    private object? DeserializeArray(XmlElement xmlObject)
    {
        if (xmlObject.HasAttribute(XmlCommon.IsNullAttributeName))
            return null;

        string instanceTypeName = xmlObject.GetAttribute(XmlCommon.RefTypeIDAttributeName);
        Type? objType = GetType(instanceTypeName) ?? throw new ApplicationException($"Cannot find type {instanceTypeName}");
        XmlNodeList xmlItems = xmlObject.GetElementsByTagName(XmlCommon.ItemTagName);

        IList? objArray = (IList?)Activator.CreateInstance(objType, new object[] { xmlItems.Count }) ?? throw new ApplicationException($"Cannot create instance of type {instanceTypeName}");
        for (int i = 0; i < xmlItems.Count; i++)
        {
            if (xmlItems[i] == null) continue;

            object? objItem = DeserializeObject((XmlElement)xmlItems[i]!);
            objArray[i] = objItem;
        }

        return objArray;
    }

    private object? DeserializeStruct(XmlElement xmlObject)
    {
        string instanceTypeName = xmlObject.GetAttribute(XmlCommon.RefTypeIDAttributeName);
        Type? objType = GetType(instanceTypeName) ?? throw new ApplicationException($"Cannot find type {instanceTypeName}");

        // For properties check if we need to consider non public ones
        BindingFlags propsBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (CheckSerializeAttribute)
            propsBindingFlags |= BindingFlags.NonPublic;

        FieldInfo[] fields = objType.GetFields(propsBindingFlags);
        PropertyInfo[] properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        object? objClass = CreateObjectInstance(objType) ?? throw new ApplicationException($"Cannot create instance of type {instanceTypeName}");
        foreach (XmlElement xmlSubElement in xmlObject.ChildNodes)
        {
            XmlElement xmlObjectToDeserialize;
            FieldInfo? fieldInfo = FindField(fields, xmlSubElement.Name);

            if (fieldInfo != null)
            {
                if (xmlSubElement.HasAttribute(XmlCommon.RefIDAttributeName))
                {
                    int refObjectID = int.Parse(xmlSubElement.GetAttribute(XmlCommon.RefIDAttributeName));
                    xmlObjectToDeserialize = _xmlObjects[refObjectID];
                }
                else
                    xmlObjectToDeserialize = xmlSubElement;

                fieldInfo.SetValue(objClass, DeserializeObject(xmlObjectToDeserialize));
                continue;
            }

            PropertyInfo? propInfo = FindProperty(properties, xmlSubElement.Name);
            if (propInfo != null)
            {
                if (xmlSubElement.HasAttribute(XmlCommon.RefIDAttributeName))
                {
                    int refObjectID = int.Parse(xmlSubElement.GetAttribute(XmlCommon.RefIDAttributeName));
                    xmlObjectToDeserialize = _xmlObjects[refObjectID];
                }
                else
                    xmlObjectToDeserialize = xmlSubElement;

                propInfo.SetValue(objClass, DeserializeObject(xmlObjectToDeserialize));
                continue;
            }
        }

        return objClass;
    }

    private object? DeserializeDataTable(XmlElement xmlObject)
    {
        string instanceTypeName = xmlObject.GetAttribute(XmlCommon.RefTypeIDAttributeName);
        Type? objType = GetType(instanceTypeName) ?? throw new ApplicationException($"Cannot find type {instanceTypeName}");
        object? objClass = CreateObjectInstance(objType) ?? throw new ApplicationException($"Cannot create instance of type {instanceTypeName}");
        XmlNodeList? xmlColumnsDefinition = xmlObject.SelectNodes($"{XmlCommon.ColumnsDefinitionTagName}/{XmlCommon.ColumnTagName}") ?? throw new ApplicationException($"Cannot find columns definition tags for datatable");
        XmlNodeList? xmlRows = xmlObject.SelectNodes($"{XmlCommon.RowsTagName}/{XmlCommon.RowTagName}") ?? throw new ApplicationException($"Cannot find rows tags for datatable");
        DataTable dataTable = (DataTable)objClass;
        
        foreach (XmlElement xmlSubEl in xmlColumnsDefinition)
        {
            string? columnName = xmlSubEl.SelectSingleNode(XmlCommon.ColumnTagName)?.InnerText;
            string? columnTypeName = xmlSubEl.SelectSingleNode(XmlCommon.TypeTagName)?.InnerText;

            if (columnName == null || columnTypeName == null)
                throw new ApplicationException($"Column definition missing name or type");

            Type? columnType = Type.GetType(columnTypeName);

            dataTable.Columns.Add(columnName, columnType!);
        }

        foreach (XmlElement xmlRow in xmlRows)
        {
            DataRow newRow = dataTable.NewRow();
            foreach (DataColumn dataCol in dataTable.Columns)
            {
                XmlElement? xmlColEl = (XmlElement?)xmlRow.SelectSingleNode(dataCol.ColumnName);

                if (xmlColEl != null)
                    newRow[dataCol.ColumnName] = DeserializeObject(xmlColEl);
            }
            dataTable.Rows.Add(newRow);
        }

        return objClass;
    }

    private object? DeserializeList(XmlElement xmlObject)
    {
        if (xmlObject.HasAttribute(XmlCommon.IsNullAttributeName))
            return null;

        string instanceTypeName = xmlObject.GetAttribute(XmlCommon.RefTypeIDAttributeName);
        Type? objType = GetType(instanceTypeName) ?? throw new ApplicationException($"Cannot find type {instanceTypeName}");
        IList? objList = (IList?)Activator.CreateInstance(objType) ?? throw new ApplicationException($"Cannot create instance of type {instanceTypeName}");
        int thisObjectID = int.Parse(xmlObject.GetAttribute(XmlCommon.RefIDAttributeName));
        bool objectExists = CheckIfObjectExists(thisObjectID, out object? thisObject);
        if (objectExists)
            return thisObject;
        else
            _objectsDeserialized.Add(thisObjectID, objList);

        XmlNodeList xmlItems = xmlObject.GetElementsByTagName(XmlCommon.ItemTagName);
        for (int i = 0; i < xmlItems.Count; i++)
        {
            if (xmlItems[i] == null) continue;

            XmlElement xmlSubElement = (XmlElement)xmlItems[i]!;
            XmlElement xmlObjectToDeserialize;

            if (xmlSubElement.HasAttribute(XmlCommon.RefIDAttributeName))
            {
                int refObjectID = int.Parse(xmlSubElement.GetAttribute(XmlCommon.RefIDAttributeName));
                xmlObjectToDeserialize = _xmlObjects[refObjectID];
            }
            else
                xmlObjectToDeserialize = xmlSubElement;

            object? objItem = DeserializeObject(xmlObjectToDeserialize);
            objList.Add(objItem);
        }

        return objList;
    }

    private object? DeserializeDictionary(XmlElement xmlObject)
    {
        if (xmlObject.HasAttribute(XmlCommon.IsNullAttributeName))
            return null;

        string instanceTypeName = xmlObject.GetAttribute(XmlCommon.RefTypeIDAttributeName);
        Type? objType = GetType(instanceTypeName) ?? throw new ApplicationException($"Cannot find type {instanceTypeName}");
        IDictionary? objDictionary = (IDictionary?)Activator.CreateInstance(objType) ?? throw new ApplicationException($"Cannot create instance of type {instanceTypeName}");
        int thisObjectID = int.Parse(xmlObject.GetAttribute(XmlCommon.RefIDAttributeName));
        bool objectExists = CheckIfObjectExists(thisObjectID, out object? thisObject);
        if (objectExists)
            return thisObject;
        else
            _objectsDeserialized.Add(thisObjectID, objDictionary);

        XmlNodeList xmlItems = xmlObject.GetElementsByTagName(XmlCommon.ItemTagName);
        for (int i = 0; i < xmlItems.Count; i++)
        {
            if (xmlItems[i] == null) continue;

            XmlElement xmlSubElement = (XmlElement)xmlItems[i]!;
            
            XmlElement? xmlKeyObject = null;
            XmlElement? xmlValueObject = null;

            if (xmlSubElement.HasChildNodes)
            {
                foreach (XmlElement xmlItemSubElement in xmlSubElement.ChildNodes)
                {
                    if (xmlItemSubElement.Name == XmlCommon.KeyTagName)
                    {
                        if (xmlItemSubElement.HasAttribute(XmlCommon.RefIDAttributeName))
                        {
                            int refObjectID = int.Parse(xmlItemSubElement.GetAttribute(XmlCommon.RefIDAttributeName));
                            xmlKeyObject = _xmlObjects[refObjectID];
                        }
                        else
                            xmlKeyObject = xmlItemSubElement;
                    }
                    else if (xmlItemSubElement.Name == XmlCommon.ValueTagName)
                    {
                        if (xmlItemSubElement.HasAttribute(XmlCommon.RefIDAttributeName))
                        {
                            int refObjectID = int.Parse(xmlItemSubElement.GetAttribute(XmlCommon.RefIDAttributeName));
                            xmlValueObject = _xmlObjects[refObjectID];
                        }
                        else
                            xmlValueObject = xmlItemSubElement;
                    }

                    if (xmlKeyObject != null && xmlValueObject != null)
                        break;
                }
            }

            if (xmlKeyObject == null || xmlValueObject == null)
                throw new ApplicationException("Dictionary deserialization: cannot find key tag or value tag");

            object? objKey = DeserializeObject(xmlKeyObject) ?? throw new ApplicationException("Dictionary deserialization: cannot find key tag");
            object? objValue = DeserializeObject(xmlValueObject);
            
            objDictionary.Add(objKey, objValue);
        }

        return objDictionary;
    }

    private object? DeserializeClass(XmlElement xmlObject)
    {
        System.Diagnostics.Debug.WriteLine("DeserializeClass start...");
        System.Diagnostics.Debug.WriteLine(xmlObject.OuterXml);

        if (xmlObject.HasAttribute(XmlCommon.IsNullAttributeName))
            return null;

        string instanceTypeName = xmlObject.GetAttribute(XmlCommon.RefTypeIDAttributeName);
        Type? objType = GetType(instanceTypeName) ?? throw new ApplicationException($"Cannot find type {instanceTypeName}");
        int refObjectID = -1;
        int thisObjectID = int.Parse(xmlObject.GetAttribute(XmlCommon.RefIDAttributeName));

        // For properties check if we need to consider non public ones
        BindingFlags propsBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (CheckSerializeAttribute)
            propsBindingFlags |= BindingFlags.NonPublic;

        List<FieldInfo> fieldList = [];
        List<PropertyInfo> propertyList = [];

        // Take care of parent classes!
        Type? currentType = objType;
        while (currentType != null)
        {
            FieldInfo[] typeFields = currentType.GetFields(propsBindingFlags);
            fieldList.AddRange(typeFields);

            PropertyInfo[] typeProperties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            propertyList.AddRange(typeProperties);

            currentType = currentType.BaseType;
        }
        FieldInfo[] fields = [.. fieldList];
        PropertyInfo[] properties = [.. propertyList]; 

        object? objClass = CreateObjectInstance(objType) ?? throw new ApplicationException($"Cannot create instance of type {instanceTypeName}");
        bool objectExists = CheckIfObjectExists(thisObjectID, out object? thisObject);
        if (objectExists)
            return thisObject;
        else
            _objectsDeserialized.Add(thisObjectID, objClass);

        foreach (XmlElement xmlSubElement in xmlObject.ChildNodes)
        {
            XmlElement xmlObjectToDeserialize;
            FieldInfo? fieldInfo = FindField(fields, xmlSubElement.Name);

            if (fieldInfo != null)
            {
                if (xmlSubElement.HasAttribute(XmlCommon.RefIDAttributeName))
                {
                    refObjectID = int.Parse(xmlSubElement.GetAttribute(XmlCommon.RefIDAttributeName));
                    xmlObjectToDeserialize = _xmlObjects[refObjectID];
                }
                else
                    xmlObjectToDeserialize = xmlSubElement;

                if (thisObjectID != refObjectID)
                    fieldInfo.SetValue(objClass, DeserializeObject(xmlObjectToDeserialize));
                else
                    fieldInfo.SetValue(objClass, objClass);

                continue;
            }

            PropertyInfo? propInfo = FindProperty(properties, xmlSubElement.Name);
            if (propInfo != null)
            {
                if (xmlSubElement.HasAttribute(XmlCommon.RefIDAttributeName))
                {
                    refObjectID = int.Parse(xmlSubElement.GetAttribute(XmlCommon.RefIDAttributeName));
                    xmlObjectToDeserialize = _xmlObjects[refObjectID];
                }
                else
                    xmlObjectToDeserialize = xmlSubElement;

                if (thisObjectID != refObjectID)
                    propInfo.SetValue(objClass, DeserializeObject(xmlObjectToDeserialize));
                else
                    propInfo.SetValue(objClass, objClass);

                continue;
            }
        }

        System.Diagnostics.Debug.WriteLine("DeserializeClass end...");

        return objClass;
    }

    private object? DeserializeObject(XmlElement xmlObject)
    {
        if (xmlObject.HasAttribute(XmlCommon.IsNullAttributeName))
            return null;

        string instanceTypeName = xmlObject.GetAttribute(XmlCommon.RefTypeIDAttributeName);
        Type? objType = GetType(instanceTypeName) ?? throw new ApplicationException($"Cannot find type {instanceTypeName}");
        if (objType.IsAssignableFrom(typeof(DataTable)))
        {
            return DeserializeDataTable(xmlObject);
        }
        else if (objType.IsPrimitive
            || objType.IsAssignableFrom(typeof(DateTime)))
        {
            TypeConverter converter = TypeDescriptor.GetConverter(objType);
            return converter.ConvertFromString(null, CultureInfo.InvariantCulture, xmlObject.InnerText);
        }
        else if (objType.IsValueType && !objType.IsEnum)
        {
            return DeserializeStruct(xmlObject);
        }
        else if (objType.IsValueType && objType.IsEnum)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(objType);
            return converter.ConvertFromString(null, CultureInfo.InvariantCulture, xmlObject.InnerText);
        }
        else if (objType.IsArray && objType == typeof(byte[]))
        {
            return Convert.FromBase64String(xmlObject.InnerText);
        }
        else if (objType.IsArray)
        {
            return DeserializeArray(xmlObject);
        }
        else if (objType.IsClass && objType == typeof(string))
        {
            // TODO: Implement encryption
            //if (xmlObject.HasAttribute(XmlCommon.EncryptedAttributeName))
            //{
            //    byte[] EncryptedBytes = Convert.FromBase64String(xmlObject.InnerText);
            //    byte[] DecryptedBytes = AsymmetricCryptography.DecryptData(EncryptedBytes, XmlCommon.KeyContainerName, true, true);
            //    return Encoding.UTF8.GetString(DecryptedBytes, 0, DecryptedBytes.Length);
            //}
            //else
                return xmlObject.InnerText;
        }
        else if (XmlCommon.Type_IsList(objType))
        {
            return DeserializeList(xmlObject);
        }
        else if (XmlCommon.Type_IsDictionary(objType))
        {
            return DeserializeDictionary(xmlObject);
        }
        else if (objType.IsClass || objType.IsInterface)
        {
            return DeserializeClass(xmlObject);
        }

        return null;
    }

    private object? DeserializeXmlDoc()
    {
        foreach (XmlElement xmlObject in _xmlDoc.GetElementsByTagName(XmlCommon.ObjectTagName))
        {
            _xmlObjects.Add(int.Parse(xmlObject.GetAttribute(XmlCommon.RefIDAttributeName)), xmlObject);
        }

        foreach (XmlElement xmlRef in _xmlDoc.GetElementsByTagName(XmlCommon.ReferenceTagName))
        {
            _references.Add(int.Parse(xmlRef.GetAttribute(XmlCommon.RefTypeIDAttributeName)), xmlRef.InnerText);
        }

        object? firstObject = DeserializeObject(_xmlRootObject);

        return firstObject;
    }

    public object? Deserialize()
    {
        return DeserializeXmlDoc();
    }
}
