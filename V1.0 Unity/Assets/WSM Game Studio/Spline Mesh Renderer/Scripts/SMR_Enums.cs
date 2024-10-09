namespace WSMGameStudio.Splines
{
    public enum BakingBehaviour
    {
        ExportAndReplace,
        JustExport
    }

    public enum MeshGenerationMethod
    {
        Manual,
        Realtime
    }

    public enum HandlesVisibility
    {
        ShowOnlyActiveHandles,
        ShowAllHandles,
        DebugOrientedPoints
    }

    public enum BezierHandlesAlignment
    {
        Aligned,
        Mirrored,
        Free,
        Automatic
    }

    public enum SplineFollowerBehaviour
    {
        StopAtTheEnd,
        Loop,
        BackAndForward
    }

    public enum LinkedFollowerBehaviour
    {
        Wrap,
        Overflow
    }

    public enum SplineFollowerSpeedUnit
    {
        ms, // Meters per second
        kph, // Kilometers per hour
        mph, // Miles per hour
        kn // Knots
    }

    [System.Obsolete]
    public enum SplineFollowerReference
    {
        Spline,
        Terrain
    }

    public enum SplineFollowerStops
    {
        Disabled,
        LastSpline,
        EachSpline
    }

    public enum SplineInspectorMenu
    {
        Curve,
        Spline,
        Handles,
        Terrain,
        SceneView
    }

    public enum FollowerInspectorMenu
    {
        FollowerSettings,
        LinkedFollowers
    }

    public enum Vector3Directions
    {
        Up,
        Down,
        Right,
        Left,
        Forward,
        Back
    }
    
    public enum Vector3Axis
    {
        x, y, z
    }

    public enum MeshRendererInspectorMenu
    {
        MeshGenerationSettings,
        CollisionSettings
    }

    public enum SpawningMethod
    {
        DisconnectedSplines,
        ConnectedSplines
    }

    public enum MessageType
    {
        Success,
        Error,
        Warning
    }
}
