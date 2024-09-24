using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor.ShortcutManagement;
using Object = UnityEngine.Object;

#if UNITY_2022_1_OR_NEWER
using UnityEditor.Overlays;

#else
using System.Reflection;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
#endif

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(KnotPlacementTool))]
#if UNITY_2022_1_OR_NEWER
    class KnotPlacementToolSettings : UnityEditor.Editor, ICreateToolbar
    {
        public IEnumerable<string> toolbarElements
        {
#else
    class KnotPlacementToolSettings : CreateToolbarBase
    {
        protected override IEnumerable<string> toolbarElements
        {
#endif
            get { yield return "Spline Tool Settings/Default Knot Type"; }
        }
    }

    [EditorTool("Draw Spline", typeof(ISplineContainer), typeof(SplineToolContext))]
    sealed class KnotPlacementTool : SplineTool
    {
        sealed class DrawingOperation : IDisposable
        {
            /// <summary>
            /// Indicates whether the knot placed is at the start or end of a curve segment
            /// </summary>
            public enum DrawingDirection
            {
                Start,
                End
            }

            public bool HasStartedDrawing { get; private set; }

            public DrawingDirection Direction
            {
                get => m_Direction;
            }

            public readonly SplineInfo CurrentSplineInfo;

            readonly DrawingDirection m_Direction;
            readonly bool m_AllowDeleteIfNoCurves;

            /// <summary>
            /// Gets the last index of the knot on the spline
            /// </summary>
            /// <returns>The index of the last knot on the spline - this will be the same as the starting knot
            /// in a closed spline</returns>
            int GetLastKnotIndex()
            {
                var isFromStartAndClosed = m_Direction == DrawingDirection.Start && CurrentSplineInfo.Spline.Closed;
                var isFromEndAndOpened = m_Direction == DrawingDirection.End && !CurrentSplineInfo.Spline.Closed;
                return isFromStartAndClosed || isFromEndAndOpened ? ( CurrentSplineInfo.Spline.Count - 1 ) : 0;
            }

            internal SelectableKnot GetLastAddedKnot()
            {
                return new SelectableKnot(CurrentSplineInfo, GetLastKnotIndex());
            }

            public DrawingOperation(SplineInfo splineInfo, DrawingDirection direction, bool allowDeleteIfNoCurves)
            {
                CurrentSplineInfo = splineInfo;
                m_Direction = direction;
                m_AllowDeleteIfNoCurves = allowDeleteIfNoCurves;
            }

            public void OnGUI(IReadOnlyList<SplineInfo> splines)
            {
                KnotPlacementHandle(splines, this, CreateKnotOnKnot, CreateKnotOnSurface, DrawCurvePreview);
            }

            void UncloseSplineIfNeeded()
            {
                // If the spline was closed, we unclose it, create a knot on the last knot and connect the first and last
                if (CurrentSplineInfo.Spline.Closed)
                {
                    CurrentSplineInfo.Spline.Closed = false;

                    switch (m_Direction)
                    {
                        case DrawingDirection.Start:
                        {
                            var lastKnot = new SelectableKnot(CurrentSplineInfo, CurrentSplineInfo.Spline.Count - 1);
                            var normal = math.mul(lastKnot.Rotation, math.up());
                            EditorSplineUtility.AddKnotToTheStart(CurrentSplineInfo, lastKnot.Position, normal,
                                -lastKnot.TangentOut.Direction);

                            //Adding the knot before the first element is shifting indexes using a callback
                            //Using a delay called here to be certain that the indexes has been shift and that this new link won't be shifted
                            EditorApplication.delayCall += () =>
                                EditorSplineUtility.LinkKnots(new SelectableKnot(CurrentSplineInfo, 1),
                                    new SelectableKnot(CurrentSplineInfo, CurrentSplineInfo.Spline.Count - 1));
                            break;
                        }

                        case DrawingDirection.End:
                        {
                            var firstKnot = new SelectableKnot(CurrentSplineInfo, 0);
                            var normal = math.mul(firstKnot.Rotation, math.up());
                            EditorSplineUtility.AddKnotToTheEnd(CurrentSplineInfo, firstKnot.Position, normal,
                                -firstKnot.TangentIn.Direction);
                            EditorSplineUtility.LinkKnots(new SelectableKnot(CurrentSplineInfo, 0),
                                new SelectableKnot(CurrentSplineInfo, CurrentSplineInfo.Spline.Count - 1));
                            break;
                        }
                    }
                }
            }

            void CreateKnotOnKnot(SelectableKnot knot, float3 tangentOut)
            {
                EditorSplineUtility.RecordObject(CurrentSplineInfo, "Draw Spline");

                var lastAddedKnot = GetLastAddedKnot();
                if (knot.Equals(lastAddedKnot))
                    return;

                // If the user clicks on the first knot (or a knot linked to the first knot) of the spline close the spline
                var closeKnotIndex = m_Direction == DrawingDirection.End ? 0 : knot.SplineInfo.Spline.Count - 1;
                if (knot.SplineInfo.Equals(CurrentSplineInfo)
                    && ( knot.KnotIndex == closeKnotIndex ||
                         EditorSplineUtility.AreKnotLinked(knot,
                             new SelectableKnot(CurrentSplineInfo, closeKnotIndex)) ))
                {
                    knot.SplineInfo.Spline.Closed = true;

                    //When using a Bezier Tangent, break the mode on the first knot to keep both tangents
                    if (knot.Mode != EditorSplineUtility.DefaultTangentMode ||
                        math.lengthsq(tangentOut) > float.Epsilon)
                        knot.Mode = TangentMode.Broken;

                    SelectableTangent tangent;
                    switch (m_Direction)
                    {
                        case DrawingDirection.Start:
                            tangent = new SelectableTangent(knot.SplineInfo, closeKnotIndex, BezierTangent.Out);
                            break;

                        case DrawingDirection.End:
                            tangent = new SelectableTangent(knot.SplineInfo, closeKnotIndex, BezierTangent.In);
                            break;

                        default:
                            tangent = default;
                            break;
                    }

                    tangent.Direction = -tangentOut;
                }
                else
                {
                    UncloseSplineIfNeeded();

                    lastAddedKnot = AddKnot(knot.Position, math.mul(knot.Rotation, math.up()), tangentOut);
                    if (m_Direction == DrawingDirection.End || knot.SplineInfo.Index != lastAddedKnot.SplineInfo.Index)
                        EditorSplineUtility.LinkKnots(knot, lastAddedKnot);
                    else
                        EditorSplineUtility.LinkKnots(new SelectableKnot(knot.SplineInfo, knot.KnotIndex + 1),
                            lastAddedKnot);
                }
            }

            void CreateKnotOnSurface(float3 position, float3 normal, float3 tangentOut)
            {
                EditorSplineUtility.RecordObject(CurrentSplineInfo, "Draw Spline");

                var lastKnot = GetLastAddedKnot();

                if (lastKnot.IsValid())
                    position = ApplyIncrementalSnap(position, lastKnot.Position);

                UncloseSplineIfNeeded();

                AddKnot(position, normal, tangentOut);
            }

            SelectableKnot AddKnot(float3 position, float3 normal, float3 tangentOut)
            {
                switch (m_Direction)
                {
                    case DrawingDirection.Start:
                        return EditorSplineUtility.AddKnotToTheStart(CurrentSplineInfo, position, normal, tangentOut);

                    case DrawingDirection.End:
                        return EditorSplineUtility.AddKnotToTheEnd(CurrentSplineInfo, position, normal, tangentOut);
                }

                return default;
            }

            void DrawCurvePreview(float3 position, float3 normal, float3 tangent, SelectableKnot target)
            {
                if (!Mathf.Approximately(math.length(tangent), 0))
                {
                    TangentHandles.Draw(position - tangent, position, normal);
                    TangentHandles.Draw(position + tangent, position, normal);
                }

                var lastKnot = GetLastAddedKnot();
                if (target.IsValid() && target.Equals(lastKnot))
                    return;

                var mode = EditorSplineUtility.GetModeFromPlacementTangent(tangent);
                position = ApplyIncrementalSnap(position, lastKnot.Position);

                if (mode == TangentMode.AutoSmooth)
                    tangent = SplineUtility.GetAutoSmoothTangent(lastKnot.Position, position);

                BezierCurve previewCurve = m_Direction == DrawingDirection.Start
                    ? EditorSplineUtility.GetPreviewCurveFromStart(CurrentSplineInfo, lastKnot.KnotIndex, position, tangent, mode)
                    : EditorSplineUtility.GetPreviewCurveFromEnd(CurrentSplineInfo, lastKnot.KnotIndex, position, tangent, mode);

                CurveHandles.Draw(-1, previewCurve);

                if (EditorSplineUtility.AreTangentsModifiable(lastKnot.Mode) && CurrentSplineInfo.Spline.Count > 0)
                    TangentHandles.DrawInformativeTangent(previewCurve.P1, previewCurve.P0);
#if UNITY_2022_2_OR_NEWER
                KnotHandles.Draw(position, SplineUtility.GetKnotRotation(tangent, normal), Handles.elementColor, false, false);
#else
                KnotHandles.Draw(position, SplineUtility.GetKnotRotation(tangent, normal), SplineHandleUtility.knotColor, false, false);
#endif
            }

            /// <summary>
            /// Remove drawing action that created no curves and were canceled after being created
            /// </summary>
            public void Dispose()
            {
                EditorSplineUtility.RecordObject(CurrentSplineInfo, "Finalize Spline Drawing");

                var spline = CurrentSplineInfo.Spline;

                if (m_AllowDeleteIfNoCurves && spline != null && spline.Count == 1)
                    CurrentSplineInfo.Container.RemoveSplineAt(CurrentSplineInfo.Index);
            }
        }

#if UNITY_2022_2_OR_NEWER
        public override bool gridSnapEnabled => true;
#endif

        public override GUIContent toolbarIcon => PathIcons.knotPlacementTool;

        static bool IsMouseInWindow(EditorWindow window) => new Rect(Vector2.zero, window.position.size).Contains(Event.current.mousePosition);

        static PlacementData s_PlacementData;

        static SplineInfo s_ClosestSpline = default;

        int m_ActiveObjectIndex = 0;
        readonly List<Object> m_SortedTargets = new List<Object>();
        readonly List<SplineInfo> m_SplineBuffer = new List<SplineInfo>(4);
        static readonly List<SelectableKnot> s_KnotsBuffer = new List<SelectableKnot>();
        DrawingOperation m_CurrentDrawingOperation;
        Object m_MainTarget;

        public override void OnActivated()
        {
            base.OnActivated();
            SplineToolContext.UseCustomSplineHandles(true);
            SplineSelection.Clear();
            SplineSelection.UpdateObjectSelection(targets);
            m_ActiveObjectIndex = 0;
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            SplineToolContext.UseCustomSplineHandles(false);
            EndDrawingOperation();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var targets = GetSortedTargets(out m_MainTarget);
            var allSplines = EditorSplineUtility.GetSplinesFromTargetsInternal(targets);

            //If the spline being drawn on doesn't exist anymore, end the drawing operation
            if (m_CurrentDrawingOperation != null &&
                ( !allSplines.Contains(m_CurrentDrawingOperation.CurrentSplineInfo) ||
                  m_CurrentDrawingOperation.CurrentSplineInfo.Spline.Count == 0 ))
                EndDrawingOperation();

            DrawSplines(targets, allSplines, m_MainTarget);

            if (m_CurrentDrawingOperation == null)
                KnotPlacementHandle(allSplines, null, AddKnotOnKnot, AddKnotOnSurface, DrawKnotCreationPreview);
            else
                m_CurrentDrawingOperation.OnGUI(allSplines);

            HandleCancellation();
        }

        // Curve id to SelectableKnotList - if we're inserting on a curve, we need 3 knots to preview the change, for other cases it's 2 knots
        internal static List<(Spline spline, int curveIndex, List<BezierKnot> knots)> previewCurvesList = new();

        void DrawSplines(IReadOnlyList<Object> targets, IReadOnlyList<SplineInfo> allSplines, Object mainTarget)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EditorSplineUtility.TryGetNearestKnot(allSplines, out SelectableKnot hoveredKnot);

            foreach (var target in targets)
            {
                EditorSplineUtility.GetSplinesFromTarget(target, m_SplineBuffer);
                bool isMainTarget = target == mainTarget;

                var previewIndex = 0;


                //Draw curves
                foreach (var splineInfo in m_SplineBuffer)
                {
                    var spline = splineInfo.Spline;
                    var localToWorld = splineInfo.LocalToWorld;

                    for (int i = 0, count = spline.GetCurveCount(); i < count; ++i)
                    {
                        if (previewIndex < previewCurvesList.Count)
                        {
                            var currentPreview = previewCurvesList[previewIndex];

                            if (currentPreview.spline.Equals(spline) && currentPreview.curveIndex == i)
                            {
                                var curveKnots = currentPreview.knots;
                                for (int knotIndex = 0; knotIndex + 1 < curveKnots.Count; ++knotIndex)
                                {
                                    var previewCurve =
                                        new BezierCurve(curveKnots[knotIndex], curveKnots[knotIndex + 1]);
                                    previewCurve = previewCurve.Transform(localToWorld);
                                    CurveHandles.Draw(previewCurve, isMainTarget);
                                    if (isMainTarget)
                                    {
                                        CurveHandles.DrawFlow(
                                            previewCurve,
                                            null,
                                            -1,
                                            math.rotate(new SelectableKnot(splineInfo, i).Rotation, math.up()),
                                            math.rotate(new SelectableKnot(splineInfo, SplineUtility.NextIndex(i, spline.Count, spline.Closed)).Rotation, math.up()));
                                    }
                                }

                                previewIndex++;
                                continue;
                            }
                        }

                        var curve = spline.GetCurve(i).Transform(localToWorld);
                        CurveHandles.Draw(curve, isMainTarget);
                        if (isMainTarget)
                        {
                            CurveHandles.DrawFlow(
                                curve,
                                splineInfo.Spline,
                                i,
                                math.rotate(new SelectableKnot(splineInfo, i).Rotation, math.up()),
                                math.rotate(new SelectableKnot(splineInfo, SplineUtility.NextIndex(i, spline.Count, spline.Closed)).Rotation, math.up()));
                        }
                    }
                }

                //Draw knots
                foreach (var splineInfo in m_SplineBuffer)
                {
                    var spline = splineInfo.Spline;

                    for (int knotIndex = 0, count = spline.Count; knotIndex < count; ++knotIndex)
                    {
                        bool isHovered = hoveredKnot.SplineInfo.Equals(splineInfo) &&
                                         hoveredKnot.KnotIndex == knotIndex;
                        if (isMainTarget || isHovered)
                        {
#if UNITY_2022_2_OR_NEWER
                            KnotHandles.Draw(new SelectableKnot(splineInfo, knotIndex), Handles.elementColor, false, isHovered);
#else
                            KnotHandles.Draw(new SelectableKnot(splineInfo, knotIndex), SplineHandleUtility.knotColor, false, isHovered);
#endif
                            if (EditorSplineUtility.AreTangentsModifiable(spline.GetTangentMode(knotIndex)))
                            {
                                //Tangent In
                                if (spline.Closed || knotIndex != 0)
                                {
                                    var tangentIn = new SelectableTangent(splineInfo, knotIndex, BezierTangent.In);
                                    TangentHandles.DrawInformativeTangent(tangentIn, isMainTarget);
                                }

                                //Tangent Out
                                if (spline.Closed || knotIndex + 1 != spline.Count)
                                {
                                    var tangentOut = new SelectableTangent(splineInfo, knotIndex, BezierTangent.Out);
                                    TangentHandles.DrawInformativeTangent(tangentOut, isMainTarget);
                                }
                            }
                        }
                        else
                            KnotHandles.DrawInformativeKnot(new SelectableKnot(splineInfo, knotIndex));
                    }
                }
            }
        }

        void AddKnotOnKnot(SelectableKnot startFrom, float3 tangent)
        {
            Undo.RecordObject(startFrom.SplineInfo.Target, "Draw Spline");

            EndDrawingOperation();

            m_ActiveObjectIndex = GetTargetIndex(startFrom.SplineInfo);

            // If we start from one of the ends of the spline we just append to that spline unless
            // the spline is already closed or there is other links knots.
            EditorSplineUtility.GetKnotLinks(startFrom, s_KnotsBuffer);
            if (s_KnotsBuffer.Count == 1 && !startFrom.SplineInfo.Spline.Closed)
            {
                if (EditorSplineUtility.IsEndKnot(startFrom))
                {
                    if (math.lengthsq(tangent) > float.Epsilon)
                    {
                        var current = startFrom;
                        var tOut = current.TangentOut;
                        current.Mode = TangentMode.Broken;
                        tOut.Direction = tangent;
                    }

                    m_CurrentDrawingOperation = new DrawingOperation(startFrom.SplineInfo,
                        DrawingOperation.DrawingDirection.End, false);
                    return;
                }

                if (startFrom.KnotIndex == 0)
                {
                    if (math.lengthsq(tangent) > float.Epsilon)
                    {
                        var current = startFrom;
                        var tIn = current.TangentIn;
                        current.Mode = TangentMode.Broken;
                        tIn.Direction = tangent;
                    }

                    m_CurrentDrawingOperation = new DrawingOperation(startFrom.SplineInfo,
                        DrawingOperation.DrawingDirection.Start, false);
                    return;
                }
            }

            // Otherwise we start a new spline
            var knot = EditorSplineUtility.CreateSpline(startFrom, tangent);
            EditorSplineUtility.LinkKnots(knot, startFrom);
            m_CurrentDrawingOperation =
                new DrawingOperation(knot.SplineInfo, DrawingOperation.DrawingDirection.End, true);
        }

        void AddKnotOnSurface(float3 position, float3 normal, float3 tangentOut)
        {
            Undo.RecordObject(m_MainTarget, "Draw Spline");

            EndDrawingOperation();

            var container = (ISplineContainer)m_MainTarget;

            // Check component count to ensure that we only move the transform of a newly created
            // spline. I.e., we don't want to move a GameObject that has other components like
            // a MeshRenderer, for example.
            if (( container.Splines.Count == 1 && container.Splines[0].Count == 0
                  || container.Splines.Count == 0 )
                && ( (Component)m_MainTarget ).GetComponents<Component>().Length == 2)
            {
                ( (Component)m_MainTarget ).transform.position = position;
            }

            SplineInfo splineInfo;

            // Spline gets created with an empty spline so we add to that spline first if needed
            if (container.Splines.Count == 1 && container.Splines[0].Count == 0)
                splineInfo = new SplineInfo(container, 0);
            else
                splineInfo = EditorSplineUtility.CreateSpline(container);

            EditorSplineUtility.AddKnotToTheEnd(splineInfo, position, normal, tangentOut);
            m_CurrentDrawingOperation = new DrawingOperation(splineInfo, DrawingOperation.DrawingDirection.End, false);
        }

        //SelectableKnot is not used and only here as this method is used as a `Action<float3, float3, float3, SelectableKnot>` by the `KnotPlacementHandle` method
        void DrawKnotCreationPreview(float3 position, float3 normal, float3 tangentOut, SelectableKnot _)
        {
            if (!Mathf.Approximately(math.length(tangentOut), 0))
                TangentHandles.Draw(position + tangentOut, position, normal);

#if UNITY_2022_2_OR_NEWER
            KnotHandles.Draw(position, SplineUtility.GetKnotRotation(tangentOut, normal), Handles.elementColor, false, false);
#else
            KnotHandles.Draw(position, SplineUtility.GetKnotRotation(tangentOut, normal), SplineHandleUtility.knotColor, false, false);
#endif
        }

        static void KnotPlacementHandle(
            IReadOnlyList<SplineInfo> splines,
            DrawingOperation drawingOperation,
            Action<SelectableKnot, float3> createKnotOnKnot,
            Action<float3, float3, float3> createKnotOnSurface,
            Action<float3, float3, float3, SelectableKnot> drawPreview)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;

            if (s_PlacementData != null && GUIUtility.hotControl != controlId)
                s_PlacementData = null;

            switch (evt.GetTypeForControl(controlId))
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddDefaultControl(controlId);
                    break;

                case EventType.Repaint:
                {
                    var mousePosition = Event.current.mousePosition;
                    if (GUIUtility.hotControl == 0
                        && SceneView.currentDrawingSceneView != null
                        && !IsMouseInWindow(SceneView.currentDrawingSceneView))
                        break;

                    if (GUIUtility.hotControl == 0)
                    {
                        if (EditorSplineUtility.TryGetNearestKnot(splines, out SelectableKnot knot))
                        {
                            drawPreview.Invoke(knot.Position, math.rotate(knot.Rotation, math.up()), float3.zero, knot);
                        }
                        else if (EditorSplineUtility.TryGetNearestPositionOnCurve(splines, out SplineCurveHit hit))
                        {
                            drawPreview.Invoke(hit.Position, hit.Normal, float3.zero, default);
                        }
                        else if (SplineHandleUtility.GetPointOnSurfaces(mousePosition, out Vector3 position,
                                     out Vector3 normal))
                        {
                            drawPreview.Invoke(position, normal, float3.zero, default);
                        }
                    }

                    if (s_PlacementData != null)
                    {
                        var knotPosition = s_PlacementData.Position;
                        var tangentOut = s_PlacementData.TangentOut;

                        if (!Mathf.Approximately(math.length(s_PlacementData.TangentOut), 0))
                        {
                            TangentHandles.Draw(knotPosition - tangentOut, knotPosition, s_PlacementData.Normal);
                            TangentHandles.Draw(knotPosition + tangentOut, knotPosition, s_PlacementData.Normal);
                        }

                        drawPreview.Invoke(s_PlacementData.Position, s_PlacementData.Normal, s_PlacementData.TangentOut,
                            default);
                    }

                    break;
                }

                case EventType.MouseMove:
                    var mouseMovePosition = Event.current.mousePosition;
                    previewCurvesList.Clear();

                    s_ClosestSpline = default;
                    var hasNearKnot = EditorSplineUtility.TryGetNearestKnot(splines, out SelectableKnot k);
                    if (hasNearKnot)
                        s_ClosestSpline = k.SplineInfo;
                    else if (EditorSplineUtility.TryGetNearestPositionOnCurve(splines, out SplineCurveHit curveHit))
                    {
                        s_ClosestSpline = curveHit.PreviousKnot.SplineInfo;
                        EditorSplineUtility.GetAffectedCurves(curveHit, previewCurvesList);
                    }

                    if (SplineHandleUtility.GetPointOnSurfaces(mouseMovePosition, out Vector3 pos, out Vector3 _))
                    {
                        if (drawingOperation != null)
                        {
                            var lastKnot = drawingOperation.GetLastAddedKnot();
                            var previousKnotIndex = drawingOperation.Direction == DrawingOperation.DrawingDirection.End
                                ? drawingOperation.CurrentSplineInfo.Spline.PreviousIndex(lastKnot.KnotIndex)
                                : drawingOperation.CurrentSplineInfo.Spline.NextIndex(lastKnot.KnotIndex);

                            EditorSplineUtility.GetAffectedCurves(
                                drawingOperation.CurrentSplineInfo,
                                drawingOperation.CurrentSplineInfo.Transform.InverseTransformPoint(pos),
                                lastKnot, previousKnotIndex, previewCurvesList);
                        }
                    }

                    if (HandleUtility.nearestControl == controlId)
                        HandleUtility.Repaint();

                    break;

                case EventType.MouseDown:
                {
                    if (evt.button != 0)
                        break;

                    if (HandleUtility.nearestControl == controlId)
                    {
                        GUIUtility.hotControl = controlId;
                        evt.Use();

                        var mousePosition = Event.current.mousePosition;
                        if (EditorSplineUtility.TryGetNearestKnot(splines, out SelectableKnot knot))
                        {
                            s_PlacementData = new KnotPlacementData(knot);
                        }
                        else if (EditorSplineUtility.TryGetNearestPositionOnCurve(splines, out SplineCurveHit hit))
                        {
                            s_PlacementData = new CurvePlacementData(hit);
                        }
                        else if (SplineHandleUtility.GetPointOnSurfaces(mousePosition, out Vector3 position,
                                     out Vector3 normal))
                        {
                            s_PlacementData = new PlacementData(position, normal);
                        }
                    }

                    break;
                }

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && evt.button == 0)
                    {
                        evt.Use();

                        if (s_PlacementData != null)
                        {
                            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                            if (s_PlacementData.Plane.Raycast(ray, out float distance))
                            {
                                s_PlacementData.TangentOut =
                                    ( ray.origin + ray.direction * distance ) - s_PlacementData.Position;
                            }
                        }
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if (s_PlacementData != null)
                        {
                            var linkedKnot = s_PlacementData.GetOrCreateLinkedKnot();
                            if (linkedKnot.IsValid())
                                createKnotOnKnot.Invoke(linkedKnot, s_PlacementData.TangentOut);
                            else
                                createKnotOnSurface.Invoke(s_PlacementData.Position, s_PlacementData.Normal,
                                    s_PlacementData.TangentOut);

                            s_PlacementData = null;
                            previewCurvesList.Clear();
                        }
                    }

                    break;

                case EventType.KeyDown:
                    if (GUIUtility.hotControl == controlId &&
                        ( evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return ))
                    {
                        s_PlacementData = null;
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;
            }
        }

        void HandleCancellation()
        {
            var evt = Event.current;
            if (GUIUtility.hotControl == 0 && evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    //If we are currently drawing, end the drawing operation and start a new one. If we haven't started drawing, switch to move tool instead
                    if (m_CurrentDrawingOperation != null)
                        EndDrawingOperation();
                    else
                        ToolManager.SetActiveTool<SplineMoveTool>();
                }

#if !UNITY_2022_1_OR_NEWER
                //For 2022.1 and after the ESC key is handled in the EditorToolManager
                if (evt.keyCode == KeyCode.Escape)
                    ToolManager.SetActiveTool<SplineMoveTool>();
#endif
            }
        }

        void EndDrawingOperation()
        {
            m_CurrentDrawingOperation?.Dispose();
            m_CurrentDrawingOperation = null;
        }

        int GetTargetIndex(SplineInfo info)
        {
            return targets.ToList().IndexOf(info.Target);
        }

        IReadOnlyList<Object> GetSortedTargets(out Object mainTarget)
        {
            m_SortedTargets.Clear();
            m_SortedTargets.AddRange(targets);

            if (m_ActiveObjectIndex >= m_SortedTargets.Count)
                m_ActiveObjectIndex = 0;

            mainTarget = m_SortedTargets[m_ActiveObjectIndex];
            if (m_CurrentDrawingOperation != null)
                mainTarget = m_CurrentDrawingOperation.CurrentSplineInfo.Target;
            else if (!s_ClosestSpline.Equals(default))
                mainTarget = s_ClosestSpline.Target;

            // Move main target to the end for rendering/picking
            m_SortedTargets.Remove(mainTarget);
            m_SortedTargets.Add(mainTarget);

            return m_SortedTargets;
        }

        static Vector3 ApplyIncrementalSnap(Vector3 current, Vector3 origin)
        {
#if UNITY_2022_2_OR_NEWER
            if (EditorSnapSettings.incrementalSnapActive)
                return SplineHandleUtility.DoIncrementSnap(current, origin);
#endif
            return current;
        }

        void CycleActiveTarget()
        {
            m_ActiveObjectIndex = ( m_ActiveObjectIndex + 1 ) % targets.Count();
            SceneView.RepaintAll();
        }

        [Shortcut("Splines/Cycle Active Spline", typeof(SceneView), KeyCode.S)]
        static void ShortcutCycleActiveSpline(ShortcutArguments args)
        {
            if (activeTool is KnotPlacementTool tool)
                tool.CycleActiveTarget();
        }
    }
}