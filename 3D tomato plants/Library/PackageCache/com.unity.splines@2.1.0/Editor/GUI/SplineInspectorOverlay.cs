using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    [Icon("UnityEditor.InspectorWindow")]
    [Overlay(typeof(SceneView), "unity-spline-inspector", "Element Inspector", "SplineInspector")]
    sealed class SplineInspectorOverlay : Overlay, ITransientOverlay
    {
        internal static readonly string SplineChangeUndoMessage = L10n.Tr("Apply Changes to Spline");
        public static void ForceUpdate()
        {
            s_ForceUpdateRequested?.Invoke();
        }

        static event Action s_ForceUpdateRequested;
        static bool s_FirstUpdateSinceDomainReload = true;
        static IReadOnlyList<SplineInfo> m_SelectedSplines;
        internal static void SetSelectedSplines(IReadOnlyList<SplineInfo> splines)
        {
            m_SelectedSplines = splines;
            if (s_FirstUpdateSinceDomainReload)
            {
                s_FirstUpdateSinceDomainReload = false;
                ForceUpdate();
            }
        }

        public bool visible => ToolManager.activeContextType == typeof(SplineToolContext) && ToolManager.activeToolType != typeof(KnotPlacementTool);

        ElementInspector m_ElementInspector;

        public override VisualElement CreatePanelContent()
        {
            VisualElement root = new VisualElement();
            root.Add(m_ElementInspector = new ElementInspector());

            UpdateInspector();

            return root;
        }

        public override void OnCreated()
        {
            displayedChanged += OnDisplayedChange;
            SplineSelection.changed += UpdateInspector;
            s_ForceUpdateRequested += UpdateInspector;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public override void OnWillBeDestroyed()
        {
            displayedChanged -= OnDisplayedChange;
            SplineSelection.changed -= UpdateInspector;
            s_ForceUpdateRequested -= UpdateInspector;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnDisplayedChange(bool displayed)
        {
            UpdateInspector();
        }

        void UpdateInspector()
        {
            if (m_SelectedSplines == null)
                return;
            
            m_ElementInspector?.UpdateSelection(m_SelectedSplines);
        }

        void OnUndoRedoPerformed()
        {
            ForceUpdate();
        }
    }
}
