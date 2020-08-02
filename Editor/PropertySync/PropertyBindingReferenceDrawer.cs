using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LegendaryTools.Inspector
{
    /// <summary>
    /// Generic property binding drawer.
    /// </summary>
#if !UNITY_3_5
    [CustomPropertyDrawer(typeof(PropertyBindingReference))]
    public class PropertyBindingReferenceDrawer : PropertyDrawer
#else
public class PropertyReferenceDrawer
#endif
    {
        public class ComponentReference
        {
            public string name;
            public Component target;
        }

        /// <summary>
        /// If you want the property drawer to limit its selection list to values of specified type, set this to something other than 'void'.
        /// </summary>
        public static Type filter = typeof(void);

        /// <summary>
        /// Whether it's possible to convert between basic types, such as int to string.
        /// </summary>
        public static bool canConvert = true;

        /// <summary>
        /// Whether the property should be readable. Used to filter the property selection list.
        /// </summary>
        public static bool mustRead = false;

        /// <summary>
        /// Whether the property should be writable. Used to filter the property selection list.
        /// </summary>
        public static bool mustWrite = false;

        /// <summary>
        /// Collect a list of usable properties and fields.
        /// </summary>
        public static List<ComponentReference> GetProperties(GameObject target, bool read, bool write)
        {
            Component[] comps = target.GetComponents<Component>();

            List<ComponentReference> list = new List<ComponentReference>();

            for (int i = 0, imax = comps.Length; i < imax; ++i)
            {
                Component comp = comps[i];
                if (comp == null)
                {
                    continue;
                }

                Type type = comp.GetType();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
                FieldInfo[] fields = type.GetFields(flags);
                PropertyInfo[] props = type.GetProperties(flags);

                // The component itself without any method
                if (PropertyBindingReference.Convert(comp, filter))
                {
                    ComponentReference ent = new ComponentReference();
                    ent.target = comp;
                    list.Add(ent);
                }

                for (int b = 0; b < fields.Length; ++b)
                {
                    FieldInfo field = fields[b];

                    if (filter != typeof(void))
                    {
                        if (canConvert)
                        {
                            if (!PropertyBindingReference.Convert(field.FieldType, filter))
                            {
                                continue;
                            }
                        }
                        else if (!filter.IsAssignableFrom(field.FieldType))
                        {
                            continue;
                        }
                    }

                    ComponentReference ent = new ComponentReference();
                    ent.target = comp;
                    ent.name = field.Name;
                    list.Add(ent);
                }

                for (int b = 0; b < props.Length; ++b)
                {
                    PropertyInfo prop = props[b];
                    if (read && !prop.CanRead)
                    {
                        continue;
                    }

                    if (write && !prop.CanWrite)
                    {
                        continue;
                    }

                    if (filter != typeof(void))
                    {
                        if (canConvert)
                        {
                            if (!PropertyBindingReference.Convert(prop.PropertyType, filter))
                            {
                                continue;
                            }
                        }
                        else if (!filter.IsAssignableFrom(prop.PropertyType))
                        {
                            continue;
                        }
                    }

                    ComponentReference ent = new ComponentReference();
                    ent.target = comp;
                    ent.name = prop.Name;
                    list.Add(ent);
                }
            }

            return list;
        }

        /// <summary>
        /// Convert the specified list of delegate entries into a string array.
        /// </summary>
        public static string[] GetNames(List<ComponentReference> list, string choice, out int index)
        {
            index = 0;
            string[] names = new string[list.Count + 1];
            names[0] = string.IsNullOrEmpty(choice) ? "<Choose>" : choice;

            for (int i = 0; i < list.Count;)
            {
                ComponentReference ent = list[i];
                string del = ActionDelegateEditor.GetFuncName(ent.target, ent.name);
                names[++i] = del;
                if (index == 0 && string.Equals(del, choice))
                {
                    index = i;
                }
            }

            return names;
        }

        /// <summary>
        /// The property is either going to be 16 or 34 pixels tall, depending on whether the target has been set or not.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            SerializedProperty target = prop.FindPropertyRelative("mTarget");
            Component comp = target.objectReferenceValue as Component;
            return comp != null ? 36f : 16f;
        }

        /// <summary>
        /// Draw the actual property.
        /// </summary>
        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            SerializedProperty target = prop.FindPropertyRelative("mTarget");
            SerializedProperty field = prop.FindPropertyRelative("mName");

            rect.height = 16f;
            EditorGUI.PropertyField(rect, target, label);

            Component comp = target.objectReferenceValue as Component;

            if (comp != null)
            {
                rect.y += 18f;
                GUI.changed = false;
                EditorGUI.BeginDisabledGroup(target.hasMultipleDifferentValues);
                int index = 0;

                // Get all the properties on the target game object
                List<ComponentReference> list = GetProperties(comp.gameObject, mustRead, mustWrite);

                // We want the field to look like "Component.property" rather than just "property"
                string current =
                    PropertyBindingReference.ToString(target.objectReferenceValue as Component, field.stringValue);

                // Convert all the properties to names
                string[] names = GetNames(list, current, out index);

                // Draw a selection list
                GUI.changed = false;
                rect.xMin += EditorGUIUtility.labelWidth;
                rect.width -= 18f;
                int choice = EditorGUI.Popup(rect, "", index, names);

                // Update the target object and property name
                if (GUI.changed && choice > 0)
                {
                    ComponentReference ent = list[choice - 1];
                    target.objectReferenceValue = ent.target;
                    field.stringValue = ent.name;
                }

                EditorGUI.EndDisabledGroup();
            }
        }
    }
}