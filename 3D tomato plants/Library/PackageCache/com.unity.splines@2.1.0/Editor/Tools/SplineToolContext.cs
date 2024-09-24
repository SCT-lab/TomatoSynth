using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;
using UObject = UnityEngine.Object;

#if UNITY_2022_1_OR_NEWER
using UnityEditor.Overlays;
#endif

namespace UnityEditor.Splines
{
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(SplineToolContext))]
    class SplineToolContextSettings : UnityEditor.Editor, ICreateToolbar
    {
        public IEnumerable<string> toolbarElements
        {
            get
            {
                yield return "Spline Tool Settings/Handle Visuals";
            }
        }
    }
#endif

    /// <summary>
    /// Defines a tool context for editing splines. When authoring tools for splines, pass the SplineToolContext type
    /// to the EditorToolAttribute.editorToolContext parameter to register as a spline tool.
    /// </summary>
#if UNITY_2021_2_OR_NEWER
    [EditorToolContext("Spline", typeof(ISplineContainer)), Icon(k_IconPath)]
#else
    [EditorToolContext("Spline", typeof(ISplineContainer))]
#endif
    public sealed class SplineToolContext : EditorToolContext
    {
        const string k_IconPath = "Packages/com.unity.splines/Editor/Resources/Icons/SplineContext.png";

        static bool s_UseCustomSplineHandles = false;

        readonly SplineElementRectSelector m_RectSelector = new SplineElementRectSelector();
        readonly List<SplineInfo> m_Splines = new List<SplineInfo>();

        readonly List<SelectableTangent> m_TangentBuffer = new List<SelectableTangent>();

        bool m_WasActiveAfterDeserialize;

        internal static void UseCustomSplineHandles(bool useCustomSplineHandle)
        {
            s_UseCustomSplineHandles = useCustomSplineHandle;
        }

        /// <summary>
        /// Returns the matching EditorTool type for the specified Tool given the context.
        /// </summary>
        /// <param name="tool">The Tool to resolve to an EditorTool type.</param>
        /// <returns> An EditorTool type for the requested Tool.</returns>
        protected override Type GetEditorToolType(Tool tool)
        {
            if (tool == Tool.Move)
                return typeof(SplineMoveTool);
            if (tool == Tool.Rotate)
                return typeof(SplineRotateTool);
            if (tool == Tool.Scale)
                return typeof(SplineScaleTool);
            return null;
        }

        /// <summary>
        /// Invoked for each window where this context is active. The spline context uses this method to implement
        /// common functionality for working with splines, ex gizmo drawing and selection.
        /// </summary>
        /// <param name="window"></param>
        public override void OnToolGUI(EditorWindow window) 
        {
            UpdateSelectionIfSplineRemoved(m_Splines);

            EditorSplineUtility.GetSplinesFromTargets(targets, m_Splines);

            //TODO set active spline
            if (Event.current.type == EventType.Layout)
                SplineInspectorOverlay.SetSelectedSplines(m_Splines);

            m_RectSelector.OnGUI(m_Splines);

            if(!s_UseCustomSplineHandles)
                SplineHandles.DrawSplineHandles(m_Splines);

            HandleCommands();
        }

        void OnEnable()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterDomainReload;
            ToolManager.activeContextChanged += ContextChanged;
            UpdateSelection();
        }

        /// <summary>
        /// Invoked after this EditorToolContext becomes the active tool context.
        /// </summary>
        public override void OnActivated()
        {
            // Sync handleOrientation to Tools.pivotRotation only if we're switching from a different context.
            // This ensures that Parent/Element handleOrientation is retained after domain reload.
            if (!m_WasActiveAfterDeserialize)
                SplineTool.handleOrientation = (HandleOrientation)Tools.pivotRotation;
            else
                m_WasActiveAfterDeserialize = false;

            Spline.afterSplineWasModified += OnSplineWasModified;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        /// <summary>
        /// Invoked before this EditorToolContext stops being the active tool context.
        /// </summary>
        public override void OnWillBeDeactivated()
        {
            Spline.afterSplineWasModified -= OnSplineWasModified;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UpdateSelectionIfSplineRemoved(List<SplineInfo> previousSelection)
        {
            foreach (var splineInfo in previousSelection)
            {
                if (!EditorSplineUtility.Exists(splineInfo))
                {
                    UpdateSelection();
                    return;
                }
            }
        }

        void ContextChanged()
        {
            if (!ToolManager.IsActiveContext(this))
                SplineSelection.ClearNoUndo(false);
        }

        void OnSplineWasModified(Spline spline)
        {
            //Only updating selection is spline is in the selected m_Splines
            if(m_Splines.Count(s => s.Spline == spline) > 0)
                UpdateSelection();
        }

        void UndoRedoPerformed() => UpdateSelection();

        void UpdateSelection()
        {
            SplineSelection.UpdateObjectSelection(targets);
            SceneView.RepaintAll();
        }

        void DeleteSelected()
        {
            var selectedElements = SplineSelection.Count;

            if (selectedElements > 0)
            {
                EditorSplineUtility.RecordSelection($"Delete selected elements ({selectedElements})");

                List<SplineInfo> splinesToRemove = new List<SplineInfo>();
                // First delete the knots in selection
                var knotBuffer = new List<SelectableKnot>();
                SplineSelection.GetElements(m_Splines, knotBuffer);

                if (knotBuffer.Count > 0)
                {
                    //Sort knots index so removing them doesn't cause the rest of the indices to be invalid
                    knotBuffer.Sort((a, b) => a.KnotIndex.CompareTo(b.KnotIndex));
                    for (int i = knotBuffer.Count - 1; i >= 0; --i)
                    {
                        EditorSplineUtility.RemoveKnot(knotBuffer[i]);

                        var spline = knotBuffer[i].SplineInfo;
                        if (EditorSplineUtility.ShouldRemoveSpline(spline) && !splinesToRemove.Contains(spline))
                            splinesToRemove.Add(spline);
                    }
                }

                // "Delete" remaining tangents by zeroing them out
                SplineSelection.GetElements(m_Splines, m_TangentBuffer);
                for (int i = m_TangentBuffer.Count - 1; i >= 0; --i)
                    EditorSplineUtility.ClearTangent(m_TangentBuffer[i]);

                // Sort spline index so removing them doesn't cause the rest of the indices to be invalid
                splinesToRemove.Sort((a, b) => a.Index.CompareTo(b.Index));
                for (int i = splinesToRemove.Count - 1; i >= 0; --i)
                {
                    var spline = splinesToRemove[i];
                    spline.Container.RemoveSplineAt(spline.Index);
                }
            }
        }

        void FrameSelected()
        {
            Bounds selectionBounds;
            if (TransformOperation.canManipulate)
            {
                selectionBounds = TransformOperation.GetSelectionBounds(false);
                selectionBounds.Encapsulate(TransformOperation.pivotPosition);
            }
            else
                selectionBounds = EditorSplineUtility.GetBounds(m_Splines);

            var size = selectionBounds.size;
            if (selectionBounds.size.x < 1f)
                size.x = 1f;
            if (selectionBounds.size.y < 1f)
                size.y = 1f;
            if (selectionBounds.size.z < 1f)
                size.z = 1f;
            selectionBounds.size = size;

            SceneView.lastActiveSceneView.Frame(selectionBounds, false);
        }

        void HandleCommands()
        {
            Event evt = Event.current;
            var cmd = evt.commandName;

            if (evt.type == EventType.ValidateCommand)
            {
                switch (cmd)
                {
                    case "SelectAll":
                    case "Delete":
                    case "SoftDelete":
                    case "FrameSelected":
                        evt.Use();
                        break;

                    case "Duplicate":
                    case "Copy":
                        if (SplineSelection.HasAny<SelectableKnot>(m_Splines))
                            evt.Use();
                        break;

                    case "Paste":
                        if (CopyPaste.IsSplineCopyBuffer(GUIUtility.systemCopyBuffer))
                            evt.Use();
                        break;
                }
            }

            else if (evt.type == EventType.ExecuteCommand)
            {
                switch (cmd)
                {
                    case "SelectAll":
                    {
                        SelectAll();
                        evt.Use();
                        break;
                    }

                    case "Copy":
                    {
                        var knotBuffer = new List<SelectableKnot>();
                        SplineSelection.GetElements(m_Splines, knotBuffer);
                        GUIUtility.systemCopyBuffer = CopyPaste.Copy(knotBuffer);
                        evt.Use();
                        break;
                    }

                    case "Paste":
                    {
                        CopyPaste.Paste(GUIUtility.systemCopyBuffer);
                        evt.Use();
                        break;
                    }

                    case "Duplicate":
                    {
                        var knotBuffer = new List<SelectableKnot>();
                        var splineBuffer = new List<SplineInfo>();
                        foreach (var t in targets)
                        {
                            if (t is ISplineContainer container)
                            {
                                EditorSplineUtility.GetSplinesFromTarget(t, splineBuffer);
                                SplineSelection.GetElements(m_Splines, knotBuffer);
                                string copyPasteBuffer = CopyPaste.Copy(knotBuffer);
                                CopyPaste.Paste(copyPasteBuffer, container);
                            }
                        }

                        evt.Use();
                        break;
                    }

                    case "Delete":
                    case "SoftDelete":
                        {
                        DeleteSelected();
                        evt.Use();
                        break;
                    }

                    case "FrameSelected":
                    {
                        FrameSelected();
                        evt.Use();
                        break;
                    }
                }
            }
        }

        void SelectAll()
        {
            var knots = new List<SelectableKnot>();
            var tangents = new List<SelectableTangent>(knots.Count() * 2);

            foreach (var info in m_Splines)
            {
                if (!SplineSelection.HasActiveSplineSelection() || SplineSelection.Contains(info))
                {
                    for (int knotIdx = 0; knotIdx < info.Spline.Count; ++knotIdx)
                    {
                        knots.Add(new SelectableKnot(info, knotIdx));

                        void TryAddSelectableTangent(BezierTangent tan)
                        {
                            var t = new SelectableTangent(info, knotIdx, tan);
                            if (SplineSelectionUtility.IsSelectable(t))
                                tangents.Add(t);
                        }

                        TryAddSelectableTangent(BezierTangent.In);
                        TryAddSelectableTangent(BezierTangent.Out);
                    }
                }
            }

            SplineSelection.AddRange(knots);
            SplineSelection.AddRange(tangents);

        }

        void OnAfterDomainReload()
        {
            m_WasActiveAfterDeserialize = ToolManager.activeContextType == typeof(SplineToolContext);
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterDomainReload;
        }

        internal static Spline GetSpline(UObject target, int targetIndex)
        {
            if (target is ISplineContainer provider)
                return provider.Splines.ElementAt(targetIndex);
            return null;
        }
    }
}
