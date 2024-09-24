using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineContainer))]
    class SplineContainerEditor : UnityEditor.Editor
    {
        SerializedProperty m_SplineProperty;
        SerializedProperty splinesProperty => m_SplineProperty ??= serializedObject.FindProperty("m_Splines");

        static GUIStyle s_HelpLabelStyle;
        static GUIContent s_HelpLabelContent;
        static GUIContent s_HelpLabelContentIcon;

        const string k_ComponentMessage = "Use the <b>Spline Edit Mode</b> in the <b>Scene Tools Overlay</b> to edit this Spline.";
        const string k_HelpBoxIconPath = "Packages/com.unity.splines/Editor/Resources/Icons/SplineContext.png";
        
        public void OnEnable()
        {
            if(s_HelpLabelContent == null)
                s_HelpLabelContent = EditorGUIUtility.TrTextContent(k_ComponentMessage);

            if(s_HelpLabelContentIcon == null)
                s_HelpLabelContentIcon = EditorGUIUtility.TrIconContent(k_HelpBoxIconPath);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (s_HelpLabelStyle == null)
            {
                s_HelpLabelStyle = new GUIStyle(EditorStyles.helpBox);
                s_HelpLabelStyle.padding = new RectOffset(10, 10, 10, 10);
            }

            EditorGUILayout.BeginHorizontal(s_HelpLabelStyle);
            EditorGUILayout.LabelField(s_HelpLabelContentIcon, GUILayout.Width(20), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(s_HelpLabelContent, new GUIStyle(EditorStyles.label){richText = true, wordWrap = true});
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            var reorderableList = SplineReorderableListUtility.GetSplinesReorderableList(splinesProperty);
            reorderableList.DoList(EditorGUILayout.GetControlRect(true, reorderableList.GetHeight()));

            serializedObject.ApplyModifiedProperties();
        }

        bool HasFrameBounds()
        {
            foreach (var o in targets)
            {
                var target = (SplineContainer) o;
                foreach (var spline in target.Splines)
                    if (spline.Count > 0)
                        return true;
            }

            return false;
        }

        Bounds OnGetFrameBounds()
        {
            List<SplineInfo> splines = new List<SplineInfo>();
            EditorSplineUtility.GetSplinesFromTargets(targets, splines);
            return EditorSplineUtility.GetBounds(splines);
        }
    }
}