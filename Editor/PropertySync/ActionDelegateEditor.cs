using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LegendaryTools.Inspector
{
    public static class ActionDelegateEditor
    {
        private static bool mEndHorizontal;

        /// <summary>
        /// Collect a list of usable delegates from the specified target game object.
        /// </summary>

        public static bool minimalisticLook
        {
            get => GetBool("Minimalistic", false);
            set => SetBool("Minimalistic", value);
        }

        /// <summary>
        /// Get the previously saved boolean value.
        /// </summary>
        public static bool GetBool(string name, bool defaultValue)
        {
            return EditorPrefs.GetBool(name, defaultValue);
        }

        /// <summary>
        /// Save the specified boolean value in settings.
        /// </summary>
        public static void SetBool(string name, bool val)
        {
            EditorPrefs.SetBool(name, val);
        }

        /// <summary>
        /// Convenience function that converts Class + Function combo into Class.Function representation.
        /// </summary>
        public static string GetFuncName(object obj, string method)
        {
            if (obj == null)
            {
                return "<null>";
            }

            string type = obj.GetType().ToString();
            int period = type.LastIndexOf('/');
            if (period > 0)
            {
                type = type.Substring(period + 1);
            }

            return string.IsNullOrEmpty(method) ? type : type + "/" + method;
        }

        public static List<PropertyBindingReferenceDrawer.ComponentReference> GetMethods(GameObject target)
        {
            MonoBehaviour[] comps = target.GetComponents<MonoBehaviour>();

            List<PropertyBindingReferenceDrawer.ComponentReference> list =
                new List<PropertyBindingReferenceDrawer.ComponentReference>();

            for (int i = 0, imax = comps.Length; i < imax; ++i)
            {
                MonoBehaviour mb = comps[i];
                if (mb == null)
                {
                    continue;
                }

                MethodInfo[] methods = mb.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

                for (int b = 0; b < methods.Length; ++b)
                {
                    MethodInfo mi = methods[b];

                    if (mi.ReturnType == typeof(void))
                    {
                        string name = mi.Name;
                        if (name == "Invoke")
                        {
                            continue;
                        }

                        if (name == "InvokeRepeating")
                        {
                            continue;
                        }

                        if (name == "CancelInvoke")
                        {
                            continue;
                        }

                        if (name == "StopCoroutine")
                        {
                            continue;
                        }

                        if (name == "StopAllCoroutines")
                        {
                            continue;
                        }

                        if (name == "BroadcastMessage")
                        {
                            continue;
                        }

                        if (name.StartsWith("SendMessage"))
                        {
                            continue;
                        }

                        if (name.StartsWith("set_"))
                        {
                            continue;
                        }

                        PropertyBindingReferenceDrawer.ComponentReference ent =
                            new PropertyBindingReferenceDrawer.ComponentReference();
                        ent.target = mb;
                        ent.name = mi.Name;
                        list.Add(ent);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Draw an editor field for the Unity Delegate.
        /// </summary>
        public static bool Field(Object undoObject, ActionDelegate del)
        {
            return Field(undoObject, del, true, minimalisticLook);
        }

        /// <summary>
        /// Draw an editor field for the Unity Delegate.
        /// </summary>
        public static bool Field(Object undoObject, ActionDelegate del, bool removeButton, bool minimalistic)
        {
            if (del == null)
            {
                return false;
            }

            bool prev = GUI.changed;
            GUI.changed = false;
            bool retVal = false;
            MonoBehaviour target = del.target;
            bool remove = false;

            if (removeButton && (del.target != null || del.isValid))
            {
                if (!minimalistic)
                {
                    SetLabelWidth(82f);
                }

                if (del.target == null && del.isValid)
                {
                    EditorGUILayout.LabelField("Notify", del.ToString());
                }
                else
                {
                    target =
                        EditorGUILayout.ObjectField("Notify", del.target, typeof(MonoBehaviour), true) as MonoBehaviour;
                }

                GUILayout.Space(-18f);
                GUILayout.BeginHorizontal();
                GUILayout.Space(70f);

                if (GUILayout.Button("", "ToggleMixed", GUILayout.Width(20f), GUILayout.Height(16f)))
                {
                    target = null;
                    remove = true;
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                target = EditorGUILayout.ObjectField("Notify", del.target, typeof(MonoBehaviour),
                    true) as MonoBehaviour;
            }

            if (remove)
            {
                RegisterUndo("Delegate Selection", undoObject);
                del.Clear();
                EditorUtility.SetDirty(undoObject);
            }
            else if (del.target != target)
            {
                RegisterUndo("Delegate Selection", undoObject);
                del.target = target;
                EditorUtility.SetDirty(undoObject);
            }

            if (del.target != null && del.target.gameObject != null)
            {
                GameObject go = del.target.gameObject;
                List<PropertyBindingReferenceDrawer.ComponentReference> list = GetMethods(go);

                int index = 0;
                string[] names = PropertyBindingReferenceDrawer.GetNames(list, del.ToString(), out index);
                int choice = 0;

                GUILayout.BeginHorizontal();
                choice = EditorGUILayout.Popup("Method", index, names);
                DrawPadding();
                GUILayout.EndHorizontal();

                if (choice > 0 && choice != index)
                {
                    PropertyBindingReferenceDrawer.ComponentReference entry = list[choice - 1];
                    RegisterUndo("Delegate Selection", undoObject);
                    del.target = entry.target as MonoBehaviour;
                    del.methodName = entry.name;
                    EditorUtility.SetDirty(undoObject);
                    retVal = true;
                }

                GUI.changed = false;
                ActionDelegate.Parameter[] ps = del.parameters;

                if (ps != null)
                {
                    for (int i = 0; i < ps.Length; ++i)
                    {
                        ActionDelegate.Parameter param = ps[i];
                        Object obj = EditorGUILayout.ObjectField("   Arg " + i, param.obj, typeof(Object), true);

                        if (GUI.changed)
                        {
                            GUI.changed = false;
                            param.obj = obj;
                            EditorUtility.SetDirty(undoObject);
                        }

                        if (obj == null)
                        {
                            continue;
                        }

                        GameObject selGO = null;
                        Type type = obj.GetType();
                        if (type == typeof(GameObject))
                        {
                            selGO = obj as GameObject;
                        }
                        else if (type.IsSubclassOf(typeof(Component)))
                        {
                            selGO = (obj as Component).gameObject;
                        }

                        if (selGO != null)
                        {
                            // Parameters must be exact -- they can't be converted like property bindings
                            PropertyBindingReferenceDrawer.filter = param.expectedType;
                            PropertyBindingReferenceDrawer.canConvert = false;
                            List<PropertyBindingReferenceDrawer.ComponentReference> ents =
                                PropertyBindingReferenceDrawer.GetProperties(selGO, true, false);

                            int selection;
                            string[] props = GetNames(ents, GetFuncName(param.obj, param.field), out selection);

                            GUILayout.BeginHorizontal();
                            int newSel = EditorGUILayout.Popup(" ", selection, props);
                            DrawPadding();
                            GUILayout.EndHorizontal();

                            if (GUI.changed)
                            {
                                GUI.changed = false;

                                if (newSel == 0)
                                {
                                    param.obj = selGO;
                                    param.field = null;
                                }
                                else
                                {
                                    param.obj = ents[newSel - 1].target;
                                    param.field = ents[newSel - 1].name;
                                }

                                EditorUtility.SetDirty(undoObject);
                            }
                        }
                        else if (!string.IsNullOrEmpty(param.field))
                        {
                            param.field = null;
                            EditorUtility.SetDirty(undoObject);
                        }

                        PropertyBindingReferenceDrawer.filter = typeof(void);
                        PropertyBindingReferenceDrawer.canConvert = true;
                    }
                }
            }
            else
            {
                retVal = GUI.changed;
            }

            GUI.changed = prev;
            return retVal;
        }

        /// <summary>
        /// Convert the specified list of delegate entries into a string array.
        /// </summary>
        public static string[] GetNames(List<PropertyBindingReferenceDrawer.ComponentReference> list, string choice,
            out int index)
        {
            index = 0;
            string[] names = new string[list.Count + 1];
            names[0] = "<GameObject>";

            for (int i = 0; i < list.Count;)
            {
                PropertyBindingReferenceDrawer.ComponentReference ent = list[i];
                string del = GetFuncName(ent.target, ent.name);
                names[++i] = del;
                if (index == 0 && string.Equals(del, choice))
                {
                    index = i;
                }
            }

            return names;
        }

        /// <summary>
        /// Draw a list of fields for the specified list of delegates.
        /// </summary>
        public static void Field(Object undoObject, List<ActionDelegate> list)
        {
            Field(undoObject, list, null, null, minimalisticLook);
        }

        /// <summary>
        /// Draw a list of fields for the specified list of delegates.
        /// </summary>
        public static void Field(Object undoObject, List<ActionDelegate> list, bool minimalistic)
        {
            Field(undoObject, list, null, null, minimalistic);
        }

        /// <summary>
        /// Draw a list of fields for the specified list of delegates.
        /// </summary>
        public static void Field(Object undoObject, List<ActionDelegate> list, string noTarget, string notValid,
            bool minimalistic)
        {
            bool targetPresent = false;
            bool isValid = false;

            // Draw existing delegates
            for (int i = 0; i < list.Count;)
            {
                ActionDelegate del = list[i];

                if (del == null || del.target == null && !del.isValid)
                {
                    list.RemoveAt(i);
                    continue;
                }

                Field(undoObject, del, true, minimalistic);
                EditorGUILayout.Space();

                if (del.target == null && !del.isValid)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (del.target != null)
                {
                    targetPresent = true;
                }

                isValid = true;
                ++i;
            }

            // Draw a new delegate
            ActionDelegate newDel = new ActionDelegate();
            Field(undoObject, newDel, true, minimalistic);

            if (newDel.target != null)
            {
                targetPresent = true;
                list.Add(newDel);
            }

            if (!targetPresent)
            {
                if (!string.IsNullOrEmpty(noTarget))
                {
                    GUILayout.Space(6f);
                    EditorGUILayout.HelpBox(noTarget, MessageType.Info, true);
                    GUILayout.Space(6f);
                }
            }
            else if (!isValid)
            {
                if (!string.IsNullOrEmpty(notValid))
                {
                    GUILayout.Space(6f);
                    EditorGUILayout.HelpBox(notValid, MessageType.Warning, true);
                    GUILayout.Space(6f);
                }
            }
        }

        //////////////////////////////////////////////////////

        /// <summary>
        /// Convenience function that marks the specified object as dirty in the Unity Editor.
        /// </summary>
        public static void SetDirty(Object obj)
        {
#if UNITY_EDITOR
            if (obj)
            {
                //if (obj is Component) Debug.Log(NGUITools.GetHierarchy((obj as Component).gameObject), obj);
                //else if (obj is GameObject) Debug.Log(NGUITools.GetHierarchy(obj as GameObject), obj);
                //else Debug.Log("Hmm... " + obj.GetType(), obj);
                EditorUtility.SetDirty(obj);
            }
#endif
        }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        public static bool DrawMinimalisticHeader(string text)
        {
            return DrawHeader(text, text, false, true);
        }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        public static bool DrawHeader(string text)
        {
            return DrawHeader(text, text, false, minimalisticLook);
        }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        public static bool DrawHeader(string text, string key)
        {
            return DrawHeader(text, key, false, minimalisticLook);
        }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        public static bool DrawHeader(string text, bool detailed)
        {
            return DrawHeader(text, text, detailed, !detailed);
        }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        public static bool DrawHeader(string text, string key, bool forceOn, bool minimalistic)
        {
            bool state = EditorPrefs.GetBool(key, true);

            if (!minimalistic)
            {
                GUILayout.Space(3f);
            }

            if (!forceOn && !state)
            {
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            }

            GUILayout.BeginHorizontal();
            GUI.changed = false;

            if (minimalistic)
            {
                if (state)
                {
                    text = "\u25BC" + (char) 0x200a + text;
                }
                else
                {
                    text = "\u25BA" + (char) 0x200a + text;
                }

                GUILayout.BeginHorizontal();
                GUI.contentColor = EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.7f)
                    : new Color(0f, 0f, 0f, 0.7f);
                if (!GUILayout.Toggle(true, text, "PreToolbar2", GUILayout.MinWidth(20f)))
                {
                    state = !state;
                }

                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();
            }
            else
            {
                text = "<b><size=11>" + text + "</size></b>";
                if (state)
                {
                    text = "\u25BC " + text;
                }
                else
                {
                    text = "\u25BA " + text;
                }

                if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f)))
                {
                    state = !state;
                }
            }

            if (GUI.changed)
            {
                EditorPrefs.SetBool(key, state);
            }

            if (!minimalistic)
            {
                GUILayout.Space(2f);
            }

            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            if (!forceOn && !state)
            {
                GUILayout.Space(3f);
            }

            return state;
        }

        /// <summary>
        /// Begin drawing the content area.
        /// </summary>
        public static void BeginContents()
        {
            BeginContents(minimalisticLook);
        }

        /// <summary>
        /// Begin drawing the content area.
        /// </summary>
        public static void BeginContents(bool minimalistic)
        {
            if (!minimalistic)
            {
                mEndHorizontal = true;
                GUILayout.BeginHorizontal();
#if UNITY_4_7 || UNITY_5_5 || UNITY_5_6
                    EditorGUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(10f));
#else
                EditorGUILayout.BeginHorizontal("TextArea", GUILayout.MinHeight(10f));
#endif
            }
            else
            {
                mEndHorizontal = false;
                EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(10f));
                GUILayout.Space(10f);
            }

            GUILayout.BeginVertical();
            GUILayout.Space(2f);
        }

        /// <summary>
        /// End drawing the content area.
        /// </summary>
        public static void EndContents()
        {
            GUILayout.Space(3f);
            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (mEndHorizontal)
            {
                GUILayout.Space(3f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(3f);
        }

        /// <summary>
        /// Draw a list of fields for the specified list of delegates.
        /// </summary>
        public static void DrawEvents(string text, Object undoObject, List<ActionDelegate> list)
        {
            DrawEvents(text, undoObject, list, null, null, false);
        }

        /// <summary>
        /// Draw a list of fields for the specified list of delegates.
        /// </summary>
        public static void DrawEvents(string text, Object undoObject, List<ActionDelegate> list, bool minimalistic)
        {
            DrawEvents(text, undoObject, list, null, null, minimalistic);
        }

        /// <summary>
        /// Draw a list of fields for the specified list of delegates.
        /// </summary>
        public static void DrawEvents(string text, Object undoObject, List<ActionDelegate> list, string noTarget,
            string notValid, bool minimalistic)
        {
            if (!DrawHeader(text, text, false, minimalistic))
            {
                return;
            }

            if (!minimalistic)
            {
                BeginContents(minimalistic);
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                Field(undoObject, list, notValid, notValid, minimalistic);

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EndContents();
            }
            else
            {
                Field(undoObject, list, notValid, notValid, minimalistic);
            }
        }


        /// <summary>
        /// Helper function that draws a serialized property.
        /// </summary>
        public static SerializedProperty DrawProperty(SerializedObject serializedObject, string property,
            params GUILayoutOption[] options)
        {
            return DrawProperty(null, serializedObject, property, false, options);
        }

        /// <summary>
        /// Helper function that draws a serialized property.
        /// </summary>
        public static SerializedProperty DrawProperty(string label, SerializedObject serializedObject, string property,
            params GUILayoutOption[] options)
        {
            return DrawProperty(label, serializedObject, property, false, options);
        }

        /// <summary>
        /// Helper function that draws a serialized property.
        /// </summary>
        public static SerializedProperty DrawPaddedProperty(SerializedObject serializedObject, string property,
            params GUILayoutOption[] options)
        {
            return DrawProperty(null, serializedObject, property, true, options);
        }

        /// <summary>
        /// Helper function that draws a serialized property.
        /// </summary>
        public static SerializedProperty DrawPaddedProperty(string label, SerializedObject serializedObject,
            string property, params GUILayoutOption[] options)
        {
            return DrawProperty(label, serializedObject, property, true, options);
        }

        /// <summary>
        /// Helper function that draws a serialized property.
        /// </summary>
        public static SerializedProperty DrawProperty(string label, SerializedObject serializedObject, string property,
            bool padding, params GUILayoutOption[] options)
        {
            SerializedProperty sp = serializedObject.FindProperty(property);

            if (sp != null)
            {
                if (minimalisticLook)
                {
                    padding = false;
                }

                if (padding)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                if (sp.isArray && sp.type != "string")
                {
                    DrawArray(serializedObject, property, label ?? property);
                }
                else if (label != null)
                {
                    EditorGUILayout.PropertyField(sp, new GUIContent(label), options);
                }
                else
                {
                    EditorGUILayout.PropertyField(sp, options);
                }

                if (padding)
                {
                    DrawPadding();
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                Debug.LogWarning("Unable to find property " + property);
            }

            return sp;
        }

        /// <summary>
        /// Helper function that draws an array property.
        /// </summary>
        public static void DrawArray(this SerializedObject obj, string property, string title)
        {
            SerializedProperty sp = obj.FindProperty(property + ".Array.size");

            if (sp != null && DrawHeader(title))
            {
                BeginContents();
                int size = sp.intValue;
                int newSize = EditorGUILayout.IntField("Size", size);
                if (newSize != size)
                {
                    obj.FindProperty(property + ".Array.size").intValue = newSize;
                }

                EditorGUI.indentLevel = 1;

                for (int i = 0; i < newSize; i++)
                {
                    SerializedProperty p = obj.FindProperty(string.Format("{0}.Array.data[{1}]", property, i));
                    if (p != null)
                    {
                        EditorGUILayout.PropertyField(p);
                    }
                }

                EditorGUI.indentLevel = 0;
                EndContents();
            }
        }

        /// <summary>
        /// Helper function that draws a serialized property.
        /// </summary>
        public static void DrawProperty(string label, SerializedProperty sp, params GUILayoutOption[] options)
        {
            DrawProperty(label, sp, true, options);
        }

        /// <summary>
        /// Helper function that draws a serialized property.
        /// </summary>
        public static void DrawProperty(string label, SerializedProperty sp, bool padding,
            params GUILayoutOption[] options)
        {
            if (sp != null)
            {
                if (padding)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                if (label != null)
                {
                    EditorGUILayout.PropertyField(sp, new GUIContent(label), options);
                }
                else
                {
                    EditorGUILayout.PropertyField(sp, options);
                }

                if (padding)
                {
                    DrawPadding();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        /// <summary>
        /// Unity 4.3 changed the way LookLikeControls works.
        /// </summary>
        public static void SetLabelWidth(float width)
        {
            EditorGUIUtility.labelWidth = width;
        }

        /// <summary>
        /// Create an undo point for the specified objects.
        /// </summary>
        public static void RegisterUndo(string name, params Object[] objects)
        {
            if (objects != null && objects.Length > 0)
            {
                Undo.RecordObjects(objects, name);

                foreach (Object obj in objects)
                {
                    if (obj == null)
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(obj);
                }
            }
        }

        /// <summary>
        /// Draw 18 pixel padding on the right-hand side. Used to align fields.
        /// </summary>
        public static void DrawPadding()
        {
            if (!minimalisticLook)
            {
                GUILayout.Space(18f);
            }
        }
    }
}