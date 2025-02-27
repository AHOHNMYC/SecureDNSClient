﻿using CustomControls;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MsmhTools
{
    public class Settings
    {
        private XDocument XDoc = new();
        private List<Setting> SettingList = new();
        class Setting
        {
            public string? ControlName { get; set; }
            public string? PropertyName { get; set; }
            public string? PropertyValue { get; set; }
            public Setting(string controlName, string propertyName, string propertyValue)
            {
                ControlName = controlName;
                PropertyName = propertyName;
                PropertyValue = propertyValue;
            }
        }

        private List<ControlsAndProperties> ControlsAndPropertiesList = new();
        public class ControlsAndProperties
        {
            public Type ControlType { get; set; }
            public string PropertyName { get; set; }
            public ControlsAndProperties(Type controlType, string propertyName)
            {
                ControlType = controlType;
                PropertyName = propertyName;
            }
        }

        private readonly char Delimiter = '|';
        public Settings(string? xmlFilePath = null)
        {
            if (xmlFilePath != null)
            {
                if (Xml.IsValidXMLFile(xmlFilePath))
                    LoadFromXMLFile(xmlFilePath);
                else
                    CustomMessageBox.Show("XML file is not valid.");
            }
        }

        public XDocument Export()
        {
            return XDoc;
        }

        public void LoadFromXML(string xmlString)
        {
            if (Xml.IsValidXML(xmlString))
                XDoc = XDocument.Parse(xmlString);
        }

        public void LoadFromXMLFile(string xmlFilePath)
        {
            if (xmlFilePath != null && Xml.IsValidXMLFile(xmlFilePath))
            {
                // Clear List
                SettingList.Clear();

                // Clear XDoc
                XElement? settingsx = XDoc.Element("Settings");
                if (settingsx != null)
                    settingsx.RemoveAll();

                // Add Settings to XDoc
                XDoc = XDocument.Load(xmlFilePath);

                // Add Settings to List
                // Begin Check
                var settings = XDoc.Elements("Settings");
                bool settingExist = settings.Any();
                if (settingExist)
                {
                    // Top Exist
                    XElement? setting0 = XDoc.Element("Settings");
                    if (setting0 != null)
                    {
                        var controls = setting0.Elements();
                        bool controlExist = controls.Any();
                        if (controlExist)
                        {
                            // Control Exist
                            for (int n1 = 0; n1 < controls.Count(); n1++)
                            {
                                XElement? control0 = controls.ToArray()[n1];
                                if (control0 != null)
                                {
                                    var controlProperties = control0.Elements();
                                    bool controlPropertyExist = controlProperties.Any();
                                    if (controlPropertyExist)
                                    {
                                        // Control Property Exist
                                        for (int n2 = 0; n2 < controlProperties.Count(); n2++)
                                        {
                                            XElement? controlProperty0 = controlProperties.ToArray()[n2];
                                            if (controlProperty0 != null)
                                            {
                                                AddSettingToList(control0.Name.LocalName, controlProperty0.Name.LocalName, controlProperty0.Value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Save(string xmlFilePath)
        {
            XmlWriterSettings xmlWriterSettings = new();
            xmlWriterSettings.Async = true;
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.OmitXmlDeclaration = true;
            xmlWriterSettings.Encoding = new UTF8Encoding(false);
            using XmlWriter xmlWriter = XmlWriter.Create(xmlFilePath, xmlWriterSettings);
            XDoc.Save(xmlWriter);
        }

        public async Task SaveAsync(string xmlFilePath)
        {
            XmlWriterSettings xmlWriterSettings = new();
            xmlWriterSettings.Async = true;
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.OmitXmlDeclaration = true;
            xmlWriterSettings.Encoding = new UTF8Encoding(false);
            using XmlWriter xmlWriter = XmlWriter.Create(xmlFilePath, xmlWriterSettings);
            await XDoc.SaveAsync(xmlWriter, CancellationToken.None);
        }

        private void SaveToFileAsTXT(string txtFilePath)
        {
            using FileStream fileStream = new(txtFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter streamWriter = new(fileStream);
            for (int n = 0; n < SettingList.Count; n++)
            {
                var setting = SettingList[n];
                if (string.IsNullOrWhiteSpace(setting.ControlName) && string.IsNullOrWhiteSpace(setting.PropertyName) && setting.PropertyValue != null)
                {
                    object line = setting.ControlName + Delimiter + setting.PropertyName + Delimiter + setting.PropertyValue;
                    streamWriter.WriteLine(line.ToString());
                }
            }
        }

        public void LoadAllSettings(Form form)
        {
            List<Control> controls = Controllers.GetAllControls(form);
            for (int n1 = 0; n1 < controls.Count; n1++)
            {
                Control control = controls[n1];
                PropertyInfo[] properties = control.GetType().GetProperties();
                for (int n2 = 0; n2 < properties.Length; n2++)
                {
                    PropertyInfo property = properties[n2];
                    List<Setting> settingList = SettingList.ToList(); // ToList: Fix: Collection was modified; enumeration operation may not execute.
                    for (int n3 = 0; n3 < settingList.Count; n3++)
                    {
                        Setting setting = settingList[n3];
                        if (control.Name == setting.ControlName && property.Name == setting.PropertyName && setting.PropertyValue != null)
                        {
                            try
                            {
                                TypeConverter typeConverter = TypeDescriptor.GetConverter(property.PropertyType);
                                if (typeConverter.CanConvertFrom(typeof(string)))
                                {
                                    property.SetValue(control, typeConverter.ConvertFrom(setting.PropertyValue), null);
                                    break;
                                }
                            }
                            catch (Exception ex1)
                            {
                                Debug.WriteLine(property.Name + ": " + ex1.Message);
                                try
                                {
                                    property.SetValue(control, Convert.ChangeType(setting.PropertyValue, property.PropertyType), null);
                                    break;
                                }
                                catch (Exception ex2)
                                {
                                    Debug.WriteLine(property.Name + ": " + ex2.Message);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get Setting e.g.
        /// <para>Settings settings = new();</para>
        /// <para>var test = settings.GetSettingFromList&lt;bool&gt;(CheckBox1, nameof(CheckBox1.Checked));</para>
        /// </summary>
        public T? GetSettingFromList<T>(Control control, string propertyName)
        {
            for (int n = 0; n < SettingList.Count; n++)
            {
                var setting = SettingList[n];
                if (control.Name == setting.ControlName && propertyName == setting.PropertyName && setting.PropertyValue != null)
                {
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
                    if (typeConverter.CanConvertFrom(typeof(object)))
                    {
                        return (T?)typeConverter.ConvertFrom(setting.PropertyValue);
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// Get Setting e.g.
        /// <para>Settings settings = new();</para>
        /// <para>var test = settings.GetSettingFromXML&lt;bool&gt;(CheckBox1, nameof(CheckBox1.Checked));</para>
        /// </summary>
        public T? GetSettingFromXML<T>(Control control, string propertyName)
        {
            // Begin Check
            var settings = XDoc.Elements("Settings");
            bool settingExist = settings.Any();
            if (settingExist)
            {
                // Top Exist
                XElement? setting0 = XDoc.Element("Settings");
                if (setting0 != null)
                {
                    var controls = setting0.Elements(control.Name);
                    bool controlExist = controls.Any();
                    if (controlExist)
                    {
                        // Control Exist
                        XElement? control0 = setting0.Element(control.Name);
                        if (control0 != null)
                        {
                            var controlProperties = control0.Elements(propertyName);
                            bool controlPropertyExist = controlProperties.Any();
                            if (controlPropertyExist)
                            {
                                // Control Property Exist
                                XElement? controlProperty0 = control0.Element(propertyName);
                                if (controlProperty0 != null)
                                {
                                    if (control0.Name.LocalName == control.Name && controlProperty0.Name.LocalName == propertyName)
                                    {
                                        TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
                                        if (typeConverter.CanConvertFrom(typeof(string)))
                                        {
                                            return (T?)typeConverter.ConvertFrom(controlProperty0.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return default;
        }

        public void AddAllSettings(Form form)
        {
            List<Control> controls = Controllers.GetAllControls(form);
            for (int n1 = 0; n1 < controls.Count; n1++)
            {
                Control control = controls[n1];
                PropertyInfo[] properties = control.GetType().GetProperties();
                for (int n2 = 0; n2 < properties.Length; n2++)
                {
                    PropertyInfo property = properties[n2];
                    string propertyName = property.Name;

                    try
                    {
                        object? propertyValue = property.GetValue(control, null);
                        //Type propertyType = TypeToType(property.PropertyType);

                        if (!string.IsNullOrWhiteSpace(control.Name) && !string.IsNullOrWhiteSpace(propertyName) && propertyValue != null)
                        {
                            AddSetting(control, propertyName, propertyValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SaveAllSettings: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Select which control type and properties should be saved with \"AddSelectedSettings\" method.
        /// <para>e.g.</para>
        /// <para>Settings settings = new();</para>
        /// <para>settings.AddSelectedControlAndProperty(typeof(CustomCheckBox), \"Checked\");</para>
        /// </summary>
        public void AddSelectedControlAndProperty(Type controlType, string propertyName)
        {
            ControlsAndProperties controlsAndProperties = new(controlType, propertyName);
            ControlsAndPropertiesList.Add(controlsAndProperties);
        }

        /// <summary>
        /// Add selected settings to be saved with \"Save\" or \"SaveAsync\" methods.
        /// </summary>
        public void AddSelectedSettings(Form form)
        {
            List<Control> controls = Controllers.GetAllControls(form);
            for (int n1 = 0; n1 < controls.Count; n1++)
            {
                Control control = controls[n1];
                PropertyInfo[] properties = control.GetType().GetProperties();
                for (int n2 = 0; n2 < properties.Length; n2++)
                {
                    PropertyInfo property = properties[n2];
                    string propertyName = property.Name;

                    try
                    {
                        object? propertyValue = property.GetValue(control, null);

                        if (!string.IsNullOrWhiteSpace(control.Name) && !string.IsNullOrWhiteSpace(propertyName) && propertyValue != null)
                        {
                            // Read filters (to speed up settings loading)
                            for (int n3 = 0; n3 < ControlsAndPropertiesList.Count; n3++)
                            {
                                ControlsAndProperties controlsAndProperties = ControlsAndPropertiesList[n3];
                                Type selectedControlType = controlsAndProperties.ControlType;
                                string selectedPropertyName = controlsAndProperties.PropertyName;

                                if (control.GetType() == selectedControlType && propertyName.Equals(selectedPropertyName))
                                    AddSetting(control, propertyName, propertyValue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SaveAllSettings: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Add Setting e.g.
        /// <para>Settings settings = new();</para>
        /// <para>settings.AddSetting(CheckBox1, nameof(CheckBox1.Checked), CheckBox1.Checked);</para>
        /// </summary>
        public void AddSetting(Control control, string propertyName, object propertyValue)
        {
            // Add Setting to List
            AddSettingToList(control.Name, propertyName, propertyValue);

            // Add Setting to XDoc
            AddSettingToXDoc(control.Name, propertyName, propertyValue);
        }

        private void AddSettingToList(string controlName, string propertyName, object propertyValue)
        {
            if (string.IsNullOrWhiteSpace(controlName)) return;
            if (string.IsNullOrWhiteSpace(propertyName)) return;
            if (propertyValue == null) return;

            string? value = propertyValue.ToString();
            if (string.IsNullOrEmpty(value)) return;

            Setting setting = new(controlName, propertyName, value);
            SettingList.Add(setting);
        }

        private void AddSettingToXDoc(string controlName, string propertyName, object propertyValue)
        {
            if (string.IsNullOrWhiteSpace(controlName)) return;
            if (string.IsNullOrWhiteSpace(propertyName)) return;
            if (propertyValue == null) return;

            string? value = propertyValue.ToString();
            if (string.IsNullOrEmpty(value)) return;

            // Create Control Property
            XElement xControlProperty = new(propertyName);
            xControlProperty.Value = value;

            // Create Control Name
            XElement xControl = new(controlName);
            xControl.Add(xControlProperty);

            // Create Settings
            XElement xSettings = new("Settings");
            xSettings.Add(xControl);

            // Begin Check
            var settings = XDoc.Elements("Settings");
            bool settingExist = settings.Any();
            if (settingExist)
            {
                // Top Exist
                XElement? setting0 = XDoc.Element("Settings");
                if (setting0 == null) return;

                var controls = setting0.Elements(controlName);
                bool controlExist = controls.Any();
                if (controlExist)
                {
                    // Control Exist
                    XElement? control0 = setting0.Element(controlName);
                    if (control0 == null) return;

                    var controlProperties = control0.Elements(propertyName);
                    bool controlPropertyExist = controlProperties.Any();
                    if (controlPropertyExist)
                    {
                        // Control Property Exist
                        XElement? controlProperty0 = control0.Element(propertyName);
                        if (controlProperty0 == null) return;

                        controlProperty0.Value = value;
                    }
                    else
                    {
                        // Control Property Not Exist
                        control0.Add(xControlProperty);
                    }
                }
                else
                {
                    // Control Not Exist
                    setting0.Add(xControl);
                }
            }
            else
            {
                // Setiings Not Exist
                XDoc.Add(xSettings);
            }
        }

        private static Type NameToType(string typeName, Type defaultType)
        {
            Type propertyType = defaultType;

            switch (typeName)
            {
                case "Boolean": propertyType = typeof(bool); break;
                case "Byte": propertyType = typeof(byte); break;
                case "Char": propertyType = typeof(char); break;
                case "DateTime": propertyType = typeof(DateTime); break;
                case "DBNull": propertyType = typeof(DBNull); break;
                case "Decimal": propertyType = typeof(decimal); break;
                case "Double": propertyType = typeof(double); break;
                case "Int16": propertyType = typeof(short); break;
                case "Int32": propertyType = typeof(int); break;
                case "Int64": propertyType = typeof(long); break;
                case "Object": propertyType = typeof(object); break;
                case "SByte": propertyType = typeof(sbyte); break;
                case "Single": propertyType = typeof(float); break;
                case "String": propertyType = typeof(string); break;
                case "UInt16": propertyType = typeof(ushort); break;
                case "UInt32": propertyType = typeof(uint); break;
                case "UInt64": propertyType = typeof(ulong); break;
            }

            return propertyType;
        }


    }
}
