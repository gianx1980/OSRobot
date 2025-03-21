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

using System.Collections;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace OSRobot.Server.Core.Persistence;

public class XmlSerialization
{
    class ObjectSerialized(int id, object obj)
    {
        public int Id { get; set; } = id;
        public object Obj { get; set; } = obj;
    }

    public bool CheckSerializeAttribute { get; set; }

    private int _lastObjectID;
    private readonly List<ObjectSerialized> _objectsSerialized;
    private int _lastTypeID;
    private readonly Dictionary<string, int> _referencedTypes;
    private XmlDocument _xmlDoc;

    
    private XmlElement? _xmlObjectsElement;
    private XmlElement? _xmlReferencesElement;

    public XmlSerialization()
    {
        _xmlDoc = new XmlDocument();
        _objectsSerialized = [];
        _referencedTypes = [];
    }

    private bool TrackObject(object objectRef, out int objectID)
    {
        ObjectSerialized? objectTracked = _objectsSerialized.Where(o => object.ReferenceEquals(o.Obj, objectRef)).FirstOrDefault();

        if (objectTracked == null)
        {
            _lastObjectID++;
            objectTracked = new ObjectSerialized(_lastObjectID, objectRef);
            _objectsSerialized.Add(objectTracked);
            objectID = _lastObjectID;

            return false;
        }
        else
        {
            objectID = objectTracked.Id;
            return true;
        }
    }

    private XmlAttribute CreateTypeAttribute(object objectToSerialize)
    {
        int refTypeID;
        string? objectType = objectToSerialize.GetType().AssemblyQualifiedName ?? throw new ApplicationException("Assembly name is null.");
        if (!_referencedTypes.TryGetValue(objectType, out int value))
        {
            _lastTypeID++;
            _referencedTypes.Add(objectType, _lastTypeID);
            XmlElement xmlRefElement = _xmlDoc.CreateElement(XmlCommon.ReferenceTagName);
            xmlRefElement.SetAttribute(XmlCommon.RefTypeIDAttributeName, _lastTypeID.ToString());
            xmlRefElement.InnerText = objectType;
            _xmlReferencesElement!.AppendChild(xmlRefElement);

            refTypeID = _lastTypeID;
        }
        else
        {
            refTypeID = value;
        }

        XmlAttribute xmlTypeAttr = _xmlDoc.CreateAttribute(XmlCommon.RefTypeIDAttributeName);
        xmlTypeAttr.Value = refTypeID.ToString();

        return xmlTypeAttr;
    }

    private XmlElement SerializePrimitive(object objectToSerialize, string tagName)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);
        xmlEl.InnerText = Convert.ToString(objectToSerialize, CultureInfo.InvariantCulture) ?? string.Empty;
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        return xmlEl;
    }

    private XmlElement SerializeEnum(object objectToSerialize, string tagName)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);
        xmlEl.InnerText = Convert.ToString(objectToSerialize, CultureInfo.InvariantCulture) ?? string.Empty;
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        return xmlEl;
    }

    private XmlElement SerializeString(string objectToSerialize, string tagName, bool encrypt)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);
        
        // TODO: Implement encryption
        //if (encrypt)
        //{
        //    byte[] StringBytes = Encoding.UTF8.GetBytes(objectToSerialize.ToString());
        //    XmlEl.InnerText = Convert.ToBase64String(AsymmetricCryptography.EncryptData(StringBytes, XmlCommon.KeyContainerName, true, true));
        //    XmlEl.SetAttribute(XmlCommon.EncryptedAttributeName, true.ToString());
        //}
        //else
            xmlEl.InnerText = objectToSerialize.ToString();

        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        return xmlEl;
    }

    private XmlElement SerializeObjectRef(int objectID, string tagName)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);
        xmlEl.SetAttribute(XmlCommon.RefIDAttributeName, objectID.ToString());
        return xmlEl;
    }

    private XmlElement SerializeInnerObject(object objectToSerialize, string tagName, bool encrypt)
    {
        XmlElement xmlEl;
        Type objType = objectToSerialize.GetType();

        if (objectToSerialize is DataTable dataTable)
        {
            bool objectExists = TrackObject(objectToSerialize, out int objectID);

            if (!objectExists)
            {
                _xmlObjectsElement!.AppendChild(SerializeDataTable(dataTable, XmlCommon.ObjectTagName, objectID));
            }

            xmlEl = SerializeObjectRef(objectID, tagName);
        }
        else if (objType.IsPrimitive || objectToSerialize is DateTime)
        {
            xmlEl = SerializePrimitive(objectToSerialize, tagName);
        }
        else if (objType.IsValueType && !objType.IsEnum)
        {
            xmlEl = SerializeStruct(objectToSerialize, tagName);
        }
        else if (objType.IsValueType && objType.IsEnum)
        {
            xmlEl = SerializeEnum(objectToSerialize, tagName);
        }
        else if (objType.IsArray && objType == typeof(byte[]))
        {
            bool objectExists = TrackObject(objectToSerialize, out int objectID);

            if (!objectExists)
            {
                _xmlObjectsElement!.AppendChild(SerializeByteArray((byte[])objectToSerialize, XmlCommon.ObjectTagName, objectID));
            }

            xmlEl = SerializeObjectRef(objectID, tagName);
        }
        else if (objType.IsArray)
        {
            bool objectExists = TrackObject(objectToSerialize, out int objectID);

            if (!objectExists)
            {
                _xmlObjectsElement!.AppendChild(SerializeArray(objectToSerialize, XmlCommon.ObjectTagName, objectID));
            }

            xmlEl = SerializeObjectRef(objectID, tagName);
        }
        else if (objType.IsClass && objType == typeof(string))
        {
            xmlEl = SerializeString((string)objectToSerialize, tagName, encrypt);
        }
        else if (XmlCommon.Type_IsList(objType))
        {
            bool objectExists = TrackObject(objectToSerialize, out int objectID);

            if (!objectExists)
            {
                _xmlObjectsElement!.AppendChild(SerializeList(objectToSerialize, XmlCommon.ObjectTagName, objectID));
            }

            xmlEl = SerializeObjectRef(objectID, tagName);
        }
        else if (XmlCommon.Type_IsDictionary(objType))
        {
            bool objectExists = TrackObject(objectToSerialize, out int objectID);

            if (!objectExists)
            {
                _xmlObjectsElement!.AppendChild(SerializeDictionary(objectToSerialize, XmlCommon.ObjectTagName, objectID));
            }

            xmlEl = SerializeObjectRef(objectID, tagName);
        }
        else if (objType.IsClass || objType.IsInterface)
        {
            bool objectExists = TrackObject(objectToSerialize, out int objectID);

            if (!objectExists)
            {
                _xmlObjectsElement!.AppendChild(SerializeObject(objectToSerialize, XmlCommon.ObjectTagName, objectID));
            }

            xmlEl = SerializeObjectRef(objectID, tagName);
        }
        else
        {
            xmlEl = _xmlDoc.CreateElement(tagName);
        }

        return xmlEl;
    }

    private XmlElement SerializeList(object objectToSerialize, string tagName, int objectID)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);

        XmlAttribute xmlRefAttr = _xmlDoc.CreateAttribute(XmlCommon.RefIDAttributeName);
        xmlRefAttr.Value = objectID.ToString();
        xmlEl.Attributes.Append(xmlRefAttr);
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        XmlElement xmlElItems = _xmlDoc.CreateElement(XmlCommon.ItemsTagName);
        xmlEl.AppendChild(xmlElItems);

        IEnumerable list = (IEnumerable)objectToSerialize;
        foreach (object listItem in list)
        {
            XmlElement xmlListItem = SerializeInnerObject(listItem, XmlCommon.ItemTagName, false);
            xmlElItems.AppendChild(xmlListItem);
        }

        return xmlEl;
    }

    private XmlElement SerializeDictionary(object objectToSerialize, string tagName, int objectID)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);

        XmlAttribute xmlRefAttr = _xmlDoc.CreateAttribute(XmlCommon.RefIDAttributeName);
        xmlRefAttr.Value = objectID.ToString();
        xmlEl.Attributes.Append(xmlRefAttr);
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        XmlElement xmlElItems = _xmlDoc.CreateElement(XmlCommon.ItemsTagName);
        xmlEl.AppendChild(xmlElItems);

        IDictionary dictionary = (IDictionary)objectToSerialize;
        foreach (DictionaryEntry di in dictionary)
        {
            XmlElement xmlListItem = _xmlDoc.CreateElement(XmlCommon.ItemTagName); 
            xmlElItems.AppendChild(xmlListItem);

            XmlElement xmlListItemKey = SerializeInnerObject(di.Key, XmlCommon.KeyTagName, false);
            xmlListItem.AppendChild(xmlListItemKey);

            XmlElement xmlListItemValue = di.Value != null ? SerializeInnerObject(di.Value, XmlCommon.ValueTagName, false) : SerializeNullValue(XmlCommon.ValueTagName);
            xmlListItem.AppendChild(xmlListItemValue);
        }

        return xmlEl;
    }

    private XmlElement SerializeArray(object objectToSerialize, string tagName, int objectID)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);

        XmlAttribute xmlRefAttr = _xmlDoc.CreateAttribute(XmlCommon.RefIDAttributeName);
        xmlRefAttr.Value = objectID.ToString();
        xmlEl.Attributes.Append(xmlRefAttr);
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        XmlElement xmlElItems = _xmlDoc.CreateElement(XmlCommon.ItemsTagName);
        xmlEl.AppendChild(xmlElItems);

        IEnumerable list = (IEnumerable)objectToSerialize;
        foreach (object listItem in list)
        {
            if (listItem != null)
            {
                XmlElement XmlListItem = SerializeInnerObject(listItem, XmlCommon.ItemTagName, false);
                xmlElItems.AppendChild(XmlListItem);
            }
        }

        return xmlEl;
    }

    private XmlElement SerializeByteArray(byte[] objectToSerialize, string tagName, int objectID)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);

        XmlAttribute xmlRefAttr = _xmlDoc.CreateAttribute(XmlCommon.RefIDAttributeName);
        xmlRefAttr.Value = objectID.ToString();
        xmlEl.Attributes.Append(xmlRefAttr);
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        xmlEl.InnerText = Convert.ToBase64String(objectToSerialize);

        return xmlEl;
    }

    private XmlElement SerializeNullValue(string tagName)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);
        XmlAttribute xmlIsNullAttr = _xmlDoc.CreateAttribute(XmlCommon.IsNullAttributeName);
        xmlIsNullAttr.Value = "true";
        xmlEl.Attributes.Append(xmlIsNullAttr);
        
        return xmlEl;
    }

    private XmlElement SerializeObject(object objectToSerialize, string tagName, int objectID)
    {
        Type objectType = objectToSerialize.GetType();

        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);

        XmlAttribute xmlRefAttr = _xmlDoc.CreateAttribute(XmlCommon.RefIDAttributeName);
        xmlRefAttr.Value = objectID.ToString();
        xmlEl.Attributes.Append(xmlRefAttr);
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        // For properties check if we need to consider non public ones
        BindingFlags propsBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (CheckSerializeAttribute)
            propsBindingFlags |= BindingFlags.NonPublic;

        // Take care of parent classes!
        Type? currentType = objectType;
        while (currentType != null)
        {
            // Read object members
            FieldInfo[] fields = currentType.GetFields(propsBindingFlags);
            
            foreach (FieldInfo fieldInfo in fields)
            {
                if (XmlCommon.Field_IsEvent(fieldInfo)
                    || XmlCommon.Field_IsPtr(fieldInfo))
                    continue;

                if (CheckSerializeAttribute 
                    && !fieldInfo.IsPublic 
                    && !XmlCommon.Field_HasAttribute(fieldInfo, typeof(XmlSerializeFieldAttribute))
                    )
                    continue;
                

                string fieldName = XmlCommon.Field_GetFieldName(fieldInfo);

                if (XmlCommon.Field_IsNull(objectToSerialize, fieldInfo))
                    xmlEl.AppendChild(SerializeNullValue(fieldName));
                else
                {
                    object? fieldValue = fieldInfo.GetValue(objectToSerialize);
                    if (fieldValue != null)
                        xmlEl.AppendChild(SerializeInnerObject(fieldValue, fieldName, XmlCommon.Field_HasAttribute(fieldInfo, typeof(XmlEncryptFieldAttribute))));
                }
            }

            // Read object public properties
            PropertyInfo[] properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo propInfo in properties)
            {
                // Skip properties with parameters. This is ok for now.
                if ((propInfo.GetMethod?.GetParameters().Length ?? 0) > 0)
                    continue;

                if (XmlCommon.Property_IsNull(objectToSerialize, propInfo))
                    xmlEl.AppendChild(SerializeNullValue(propInfo.Name));
                else
                {
                    object? fieldValue = propInfo.GetValue(objectToSerialize);
                    if (fieldValue != null)
                        xmlEl.AppendChild(SerializeInnerObject(fieldValue, propInfo.Name, XmlCommon.Property_HasAttribute(propInfo, typeof(XmlEncryptFieldAttribute))));
                }
            }

            currentType = currentType.BaseType;
        } 

        return xmlEl;
    }

    private XmlElement SerializeStruct(object objectToSerialize, string tagName)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);
        xmlEl.Attributes.Append(CreateTypeAttribute(objectToSerialize));

        Type objectType = objectToSerialize.GetType();
        BindingFlags propsBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (CheckSerializeAttribute)
            propsBindingFlags |= BindingFlags.NonPublic;

        FieldInfo[] fields = objectType.GetFields(propsBindingFlags);

        // Read public members
        foreach (FieldInfo fieldInfo in fields)
        {
            if (XmlCommon.Field_IsNull(objectToSerialize, fieldInfo)
                || XmlCommon.Field_IsEvent(fieldInfo)
                || XmlCommon.Field_IsPtr(fieldInfo))
                continue;

            if (CheckSerializeAttribute
                    && !fieldInfo.IsPublic
                    && !XmlCommon.Field_HasAttribute(fieldInfo, typeof(XmlSerializeFieldAttribute))
                    )
                continue;

            string fieldName = XmlCommon.Field_GetFieldName(fieldInfo);

            if (XmlCommon.Field_IsNull(objectToSerialize, fieldInfo))
                xmlEl.AppendChild(SerializeNullValue(fieldName));
            else
            {
                object? fieldValue = fieldInfo.GetValue(objectToSerialize);
                if (fieldValue != null)
                    xmlEl.AppendChild(SerializeInnerObject(fieldValue, fieldName, XmlCommon.Field_HasAttribute(fieldInfo, typeof(XmlEncryptFieldAttribute))));
            }                
        }

        // Read public properties
        PropertyInfo[] properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo propInfo in properties)
        {
            // Skip properties with parameters. This is ok for now.
            if ((propInfo.GetMethod?.GetParameters().Length ?? 0) > 0)
                continue;

            if (XmlCommon.Property_IsNull(objectToSerialize, propInfo))
                xmlEl.AppendChild(SerializeNullValue(propInfo.Name));
            else
            {
                object? fieldValue = propInfo.GetValue(objectToSerialize);
                if (fieldValue != null)
                    xmlEl.AppendChild(SerializeInnerObject(fieldValue, propInfo.Name, XmlCommon.Property_HasAttribute(propInfo, typeof(XmlEncryptFieldAttribute))));
            }
        }

        return xmlEl;
    }

    private XmlElement SerializeDataTable(DataTable dataTable, string tagName, int objectID)
    {
        XmlElement xmlEl = _xmlDoc.CreateElement(tagName);
        XmlAttribute xmlRefAttr = _xmlDoc.CreateAttribute(XmlCommon.RefIDAttributeName);
        xmlRefAttr.Value = objectID.ToString();
        xmlEl.Attributes.Append(xmlRefAttr);
        xmlEl.Attributes.Append(CreateTypeAttribute(dataTable));

        XmlElement xmlElColumns = _xmlDoc.CreateElement(XmlCommon.ColumnsDefinitionTagName);
        xmlEl.AppendChild(xmlElColumns);

        foreach (DataColumn dc in dataTable.Columns)
        {
            XmlElement xmlElColumn = _xmlDoc.CreateElement(XmlCommon.ColumnTagName);
            xmlElColumns.AppendChild(xmlElColumn);

            XmlElement xmlElColumnName = _xmlDoc.CreateElement(XmlCommon.ColumnTagName);
            xmlElColumnName.InnerText = dc.ColumnName;  
            xmlElColumn.AppendChild(xmlElColumnName);

            XmlElement xmlElColumnType = _xmlDoc.CreateElement(XmlCommon.TypeTagName);
            xmlElColumnType.InnerText = dc.DataType.AssemblyQualifiedName!;
            xmlElColumn.AppendChild(xmlElColumnType);
        }

        XmlElement xmlElRows = _xmlDoc.CreateElement(XmlCommon.RowsTagName);
        xmlEl.AppendChild(xmlElRows);

        foreach (DataRow dr in dataTable.Rows)
        {
            XmlElement xmlElRow = _xmlDoc.CreateElement(XmlCommon.RowTagName);
            xmlElRows.AppendChild(xmlElRow);

            foreach (DataColumn dc in dataTable.Columns)
            {
                xmlElRow.AppendChild(SerializeInnerObject(dr[dc.ColumnName], dc.ColumnName, false));
            }
        }

        return xmlEl;
    }

    private XmlElement SerializeRootObject(object objectToSerialize)
    {
        XmlElement xmlEl;
        Type objectType = objectToSerialize.GetType();

        if (objectToSerialize is DataTable dataTable)
        {
            TrackObject(objectToSerialize, out int objectID);
            xmlEl = SerializeDataTable(dataTable, XmlCommon.ObjectTagName, objectID);
        }
        else if (objectType.IsPrimitive || objectToSerialize is DateTime)
        {
            xmlEl = SerializePrimitive(objectToSerialize, XmlCommon.ObjectTagName);
        }
        else if (objectType.IsValueType && !objectType.IsEnum)
        {
            xmlEl = SerializeStruct(objectToSerialize, XmlCommon.ObjectTagName);
        }
        else if (objectType.IsClass && objectType == typeof(string))
        {
            xmlEl = SerializeString((string)objectToSerialize, XmlCommon.ObjectTagName, false);
        }
        else if (XmlCommon.Type_IsList(objectType))
        {
            TrackObject(objectToSerialize, out int objectID);
            xmlEl = SerializeList(objectToSerialize, XmlCommon.ObjectTagName, objectID);
        }
        else if (XmlCommon.Type_IsDictionary(objectType))
        {
            TrackObject(objectToSerialize, out int objectID);
            xmlEl = SerializeDictionary(objectToSerialize, XmlCommon.ObjectTagName, objectID);
        }
        else if (objectType.IsClass || objectType.IsInterface)
        {
            TrackObject(objectToSerialize, out int objectID);
            xmlEl = SerializeObject(objectToSerialize, XmlCommon.ObjectTagName, objectID);
        }
        else
        {
            throw new ApplicationException("Unrecognized object type.");
        }

        xmlEl.SetAttribute(XmlCommon.RootObjectAttributeName, true.ToString());

        return xmlEl;
    }

    public XmlDocument Serialize(object objectGraph, string rootElementName)
    {
        _lastObjectID = 0;
        _objectsSerialized.Clear();
        _xmlDoc = new XmlDocument();

        XmlElement XmlRootEl = _xmlDoc.CreateElement(rootElementName);
        _xmlDoc.AppendChild(XmlRootEl);

        // Create Objects element
        _xmlObjectsElement = _xmlDoc.CreateElement(XmlCommon.ObjectsTagName);
        XmlRootEl.AppendChild(_xmlObjectsElement);

        // Create References element
        _xmlReferencesElement = _xmlDoc.CreateElement(XmlCommon.ReferencesTagName);
        XmlRootEl.AppendChild(_xmlReferencesElement);

        _xmlObjectsElement.AppendChild(SerializeRootObject(objectGraph));

        return _xmlDoc;
    }

    public string SerializeToXmlString(object objectGraph, string rootElementName)
    {
        return Serialize(objectGraph, rootElementName).OuterXml;
    }
}
