using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class TangentHandles
    {
        const float k_ColorAlphaFactor = 0.3f;
        const float k_TangentLineWidthDefault = 2f;
        const float k_TangentLineWidthHover = 3.5f;
        const float k_TangentLineWidthSelected = 4.5f;
        const float k_TangentStartOffsetFromKnot = 0.22f;
        const float k_TangentEndOffsetFromHandle = 0.11f;
        const float k_TangentHandleWidth = 2f;
        const float k_TangentRotWidthDefault = 1.5f;
        const float k_TangentRotDiscWidth = 3f;

        public static void DrawInformativeTangent(SelectableTangent tangent, bool active = true)
        {
            DrawInformativeTangent(tangent.Position, tangent.Owner.Position, active);
        }

        public static void DrawInformativeTangent(Vector3 position, Vector3 knotPosition, bool active = true)
        {
            if (Event.current.type != EventType.Repaint)
                return;

#if UNITY_2022_2_OR_NEWER
            var tangentColor = Handles.elementColor;
#else
            var tangentColor = SplineHandleUtility.knotColor;
#endif
            if (!active)
                tangentColor = Handles.secondaryColor;

#if UNITY_2022_2_OR_NEWER
            var tangentArmColor = tangentColor == Handles.elementColor
                ? SplineHandleUtility.tangentColor
                : tangentColor;
#else
            var tangentArmColor = tangentColor == SplineHandleUtility.knotColor
                ? SplineHandleUtility.tangentColor
                : tangentColor;
#endif

            using (new ColorScope(tangentArmColor))
            {
                var toTangent = position - knotPosition;
                var toTangentNorm = math.normalize(toTangent);
                var length = math.length(toTangent);
                var knotHandleOffset = HandleUtility.GetHandleSize(knotPosition) * k_TangentStartOffsetFromKnot;

                length = Mathf.Max(0f, length - knotHandleOffset);
                knotPosition += (Vector3)toTangentNorm * knotHandleOffset;

                SplineHandleUtility.DrawLineWithWidth(knotPosition, knotPosition  + (Vector3)toTangentNorm * length, k_TangentLineWidthDefault);
            }
        }

        public static void Draw(Vector3 position, Vector3 knotPosition, float3 normal, bool active = true)
        {
            var knotToTangentDirection = position - knotPosition;
            var rotation = quaternion.LookRotationSafe(knotToTangentDirection, normal);
            Draw(-1, position, rotation, knotPosition, false, false, false, TangentMode.Broken, active);
        }

        public static void Draw(int controlId, SelectableTangent tangent, bool active = true)
        {
            var owner = tangent.Owner;
            Draw(
                controlId,
                tangent.Position,
                EditorSplineUtility.GetElementRotation(math.length(tangent.LocalPosition) > 0 ? (ISplineElement)tangent : tangent.Owner),
                owner.Position,
                SplineSelection.Contains(tangent),
                SplineSelection.Contains(tangent.OppositeTangent),
                SplineHandleUtility.IsLastHoveredElement(tangent.OppositeTangent),
                owner.Mode,
                active);
        }

        public static void Draw(int controlId, Vector3 position, Quaternion rotation, Vector3 knotPosition, bool selected, bool oppositeSelected, bool oppositeHovered, TangentMode mode, bool active)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var size = HandleUtility.GetHandleSize(position);
            var hovered = SplineHandleUtility.IsHoverAvailableForSplineElement() && SplineHandleUtility.IsElementHovered(controlId);

#if UNITY_2022_2_OR_NEWER
            var tangentColor = Handles.elementColor;
            if (hovered)
                tangentColor = Handles.elementPreselectionColor;
            else if (selected)
                tangentColor = Handles.elementSelectionColor;
#else
            var tangentColor = SplineHandleUtility.knotColor;
            if (hovered)
                tangentColor = Handles.preselectionColor;
            else if (selected)
                tangentColor = Handles.selectedColor;
#endif
            if (!active)
                tangentColor = Handles.secondaryColor;

#if UNITY_2022_2_OR_NEWER
            var tangentArmColor = tangentColor == Handles.elementColor ?
                SplineHandleUtility.tangentColor :
                tangentColor;
#else
            var tangentArmColor = tangentColor == SplineHandleUtility.knotColor ?
                SplineHandleUtility.tangentColor :
                tangentColor;
#endif

            if (mode == TangentMode.Mirrored)
            {
#if UNITY_2022_2_OR_NEWER
                if(oppositeHovered)
                    tangentArmColor = Handles.elementPreselectionColor;
                else if(tangentArmColor == SplineHandleUtility.tangentColor && oppositeSelected)
                    tangentArmColor = Handles.elementSelectionColor;
#else
                if(oppositeHovered)
                    tangentArmColor = Handles.preselectionColor;
                else if(tangentArmColor == SplineHandleUtility.tangentColor && oppositeSelected)
                    tangentArmColor = Handles.selectedColor;
#endif
            }

            var rotationDiscWidth = k_TangentRotWidthDefault;
            if (hovered)
                rotationDiscWidth = k_TangentRotDiscWidth;

            using (new ZTestScope(CompareFunction.Less))
            {
                // Draw tangent arm.
                using (new ColorScope(tangentArmColor))
                    DrawTangentArm(position, knotPosition, size, mode, selected, hovered, oppositeSelected, oppositeHovered);

                // Draw tangent shape.
                using (new Handles.DrawingScope(tangentColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                    DrawTangentShape(size, selected);
            }

            using (new ZTestScope(CompareFunction.Greater))
            {
                // Draw tangent arm.
                var newTangentArmColor = new Color(tangentArmColor.r, tangentArmColor.g, tangentArmColor.b, tangentArmColor.a * k_ColorAlphaFactor);
                using (new ColorScope(newTangentArmColor))
                    DrawTangentArm(position, knotPosition, size, mode, selected, hovered, oppositeSelected, oppositeHovered);

                // Draw tangent shape.
                var newDiscColor = new Color(tangentColor.r, tangentColor.g, tangentColor.b, tangentColor.a * k_ColorAlphaFactor);
                using (new Handles.DrawingScope(newDiscColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                    DrawTangentShape(size, selected);
            }

            // Draw tangent disc on hover.
            if (hovered)
            {
                var tangentHandleOffset = size * k_TangentEndOffsetFromHandle;
                using (new ZTestScope(CompareFunction.Less))
                {
                    using (new Handles.DrawingScope(tangentColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                        SplineHandleUtility.DrawAAWireDisc(Vector3.zero, Vector3.up, tangentHandleOffset, rotationDiscWidth);
                }

                using (new ZTestScope(CompareFunction.Greater))
                {
                    var newDiscColor = new Color(tangentColor.r, tangentColor.g, tangentColor.b, tangentColor.a * k_ColorAlphaFactor);
                    using (new Handles.DrawingScope(newDiscColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                        SplineHandleUtility.DrawAAWireDisc(Vector3.zero, Vector3.up, tangentHandleOffset, rotationDiscWidth);
                }
            }
        }

        static void DrawTangentArm(Vector3 position, Vector3 knotPosition, float size, TangentMode mode, bool selected, bool hovered, bool oppositeSelected, bool oppositeHovered)
        {
            var width = k_TangentLineWidthDefault;
            if (!DirectManipulation.IsDragging)
            {
                if (selected || (mode != TangentMode.Broken && oppositeSelected))
                    width = k_TangentLineWidthSelected;
                else if (hovered || (mode != TangentMode.Broken && oppositeHovered))
                    width = k_TangentLineWidthHover;
            }

            var startPos = knotPosition;
            var toTangent = position - knotPosition;
            var toTangentNorm = math.normalize(toTangent);
            var length = math.length(toTangent);

            var knotHandleSize = HandleUtility.GetHandleSize(startPos);
            var knotHandleOffset = knotHandleSize * k_TangentStartOffsetFromKnot;
            var tangentHandleOffset = size * k_TangentEndOffsetFromHandle;
            // Reduce the length slightly, so that there's some space between tangent line endings and handles.
            length = Mathf.Max(0f, length - knotHandleOffset - tangentHandleOffset);
            startPos += (Vector3)toTangentNorm * knotHandleOffset;

            SplineHandleUtility.DrawLineWithWidth(startPos + (Vector3)toTangentNorm * length, startPos, width, SplineHandleUtility.denseLineAATex);
        }

        static void DrawTangentShape(float size, bool selected)
        {
            var midVector = new Vector3(-.5f, 0, .5f);
            if (selected)
            {
                var factor = 0.7f;
                var radius = (selected ? SplineHandleUtility.knotDiscRadiusFactorSelected : SplineHandleUtility.knotDiscRadiusFactorHover) * size;
                // As Handles.DrawAAConvexPolygon has no thickness parameter, we're drawing a AA Polyline here so that the polygon has thickness when viewed from a shallow angle.
                Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex,
                    k_TangentHandleWidth,
                    factor * radius * midVector,
                    factor * radius * Vector3.forward,
                    factor * radius * Vector3.right,
                    -factor * radius * Vector3.forward,
                    -factor * radius * Vector3.right,
                    factor * radius * midVector);
                Handles.DrawAAConvexPolygon(
                    radius * midVector,
                    radius * Vector3.forward,
                    radius * Vector3.right,
                    -radius * Vector3.forward,
                    -radius * Vector3.right,
                    radius * midVector);
            }
            else
            {
                var radius = SplineHandleUtility.knotDiscRadiusFactorDefault * size;
                //Starting the polyline in the middle of a segment and not to a corner to get an invisible connection.
                //Otherwise the connection is really visible in the corner as a small part is missing there.
                Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex,
                    k_TangentHandleWidth,
                    radius * midVector,
                    radius * Vector3.forward,
                    radius * Vector3.right,
                    -radius * Vector3.forward,
                    -radius * Vector3.right,
                    radius * midVector);
            }
        }
    }
}