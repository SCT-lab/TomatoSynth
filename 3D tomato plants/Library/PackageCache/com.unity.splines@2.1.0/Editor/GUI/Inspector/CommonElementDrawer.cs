namespace UnityEditor.Splines
{
    sealed class CommonElementDrawer : ElementDrawer<ISplineElement>
    {
        readonly TangentModePropertyField<ISplineElement> m_Mode;
        readonly BezierTangentPropertyField<ISplineElement> m_BezierMode;

        public CommonElementDrawer()
        {
            Add(m_Mode = new TangentModePropertyField<ISplineElement>());
            m_Mode.changed += () => { m_BezierMode.Update(targets); };

            Add(m_BezierMode = new BezierTangentPropertyField<ISplineElement>());
            m_BezierMode.changed += () => { m_Mode.Update(targets); };

            Add(new Separator());
        }

        public override void Update()
        {
            base.Update();

            m_Mode.Update(targets);
            m_BezierMode.Update(targets);
        }

        public override string GetLabelForTargets()
        {
            int knotCount = 0;
            int tangentCount = 0;
            for (int i = 0; i < targets.Count; ++i)
            {
                if (targets[i] is SelectableKnot)
                    ++knotCount;
                else if (targets[i] is SelectableTangent)
                    ++tangentCount;
            }

            return $"<b>({knotCount}) Knots</b>, <b>({tangentCount}) Tangents</b> selected";
        }
    }
}
