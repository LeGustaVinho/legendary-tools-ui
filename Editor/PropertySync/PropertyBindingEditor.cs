using LegendaryTools.Inspector;
using LegendaryTools.UI;
using UnityEditor;
using UnityEngine;

namespace LegendaryTools
{
    [CanEditMultipleObjects, CustomEditor(typeof(PropertySync))]
    public class PropertySyncEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PropertySync pb = target as PropertySync;

            ActionDelegateEditor.SetLabelWidth(80f);

            serializedObject.Update();

            if (pb.direction == PropertySync.Direction.TargetUpdatesSource && pb.target != null)
            {
                PropertyBindingReferenceDrawer.filter = pb.target.GetPropertyType();
            }

            GUILayout.Space(3f);
            PropertySync.Direction dir = (target as PropertySync).direction;

            PropertyBindingReferenceDrawer.mustRead = dir == PropertySync.Direction.SourceUpdatesTarget ||
                                                      dir == PropertySync.Direction.BiDirectional;
            PropertyBindingReferenceDrawer.mustWrite = dir == PropertySync.Direction.TargetUpdatesSource ||
                                                       dir == PropertySync.Direction.BiDirectional;

            ActionDelegateEditor.DrawProperty(serializedObject, "source");

            if (pb.direction == PropertySync.Direction.SourceUpdatesTarget && pb.source != null)
            {
                PropertyBindingReferenceDrawer.filter = pb.source.GetPropertyType();
            }

            if (pb.source.target != null)
            {
                GUILayout.Space(-18f);

                if (pb.direction == PropertySync.Direction.TargetUpdatesSource)
                {
                    GUILayout.Label("   \u25B2"); // Up
                }
                else if (pb.direction == PropertySync.Direction.SourceUpdatesTarget)
                {
                    GUILayout.Label("   \u25BC"); // Down
                }
                else
                {
                    GUILayout.Label("  \u25B2\u25BC");
                }
            }

            GUILayout.Space(1f);

            PropertyBindingReferenceDrawer.mustRead = dir == PropertySync.Direction.TargetUpdatesSource ||
                                                      dir == PropertySync.Direction.BiDirectional;
            PropertyBindingReferenceDrawer.mustWrite = dir == PropertySync.Direction.SourceUpdatesTarget ||
                                                       dir == PropertySync.Direction.BiDirectional;

            ActionDelegateEditor.DrawProperty(serializedObject, "target");

            PropertyBindingReferenceDrawer.mustRead = false;
            PropertyBindingReferenceDrawer.mustWrite = false;
            PropertyBindingReferenceDrawer.filter = typeof(void);

            GUILayout.Space(1f);
            ActionDelegateEditor.DrawPaddedProperty(serializedObject, "direction");
            ActionDelegateEditor.DrawPaddedProperty(serializedObject, "update");
            GUILayout.BeginHorizontal();
            ActionDelegateEditor.DrawProperty(" ", serializedObject, "editMode", GUILayout.Width(100f));
            GUILayout.Label("Update in Edit Mode");

            if (pb.source.GetPropertyType() == typeof(bool) && pb.target.GetPropertyType() == typeof(bool))
            {
                ActionDelegateEditor.DrawProperty(" ", serializedObject, "invertBool", GUILayout.Width(100f));
                GUILayout.Label("Invert Bool");
            }

            GUILayout.EndHorizontal();

            if (!serializedObject.isEditingMultipleObjects)
            {
                if (pb.source != null && pb.target != null &&
                    pb.source.GetPropertyType() != pb.target.GetPropertyType())
                {
                    if (pb.direction == PropertySync.Direction.BiDirectional)
                    {
                        EditorGUILayout.HelpBox(
                            "Bi-Directional updates require both Source and Target to reference values of the same type.",
                            MessageType.Error);
                    }
                    else if (pb.direction == PropertySync.Direction.SourceUpdatesTarget)
                    {
                        if (!PropertyBindingReference.Convert(pb.source.Get(), pb.target.GetPropertyType()))
                        {
                            EditorGUILayout.HelpBox(
                                "Unable to convert " + pb.source.GetPropertyType() + " to " +
                                pb.target.GetPropertyType(), MessageType.Error);
                        }
                    }
                    else if (!PropertyBindingReference.Convert(pb.target.Get(), pb.source.GetPropertyType()))
                    {
                        EditorGUILayout.HelpBox(
                            "Unable to convert " + pb.target.GetPropertyType() + " to " + pb.source.GetPropertyType(),
                            MessageType.Error);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}