using UnityEngine;
using UnityEditor;

namespace WSMGameStudio.Splines
{
    public class SMR_CreateObjectMenus
    {
        [MenuItem("WSM Game Studio/Spline Mesh Renderer/Create/Spline", false, 10)]
        [MenuItem("GameObject/WSM Game Studio/Spline", false, 10)]
        static void CreateNewSpline(MenuCommand menuCommand)
        {
            GameObject go = CreateAndSelectNewObject(menuCommand, "Spline");

            Spline spline = go.AddComponent<Spline>();
            ApplyDefaultTheme(spline);
        }

        [MenuItem("WSM Game Studio/Spline Mesh Renderer/Create/Spline Prefab Spawner", false, 10)]
        [MenuItem("GameObject/WSM Game Studio/Spline Prefab Spawner", false, 10)]
        static void CreateNewSplinePrefabSpawner(MenuCommand menuCommand)
        {
            GameObject go = CreateAndSelectNewObject(menuCommand, "SplinePrefabSpawner");

            SplinePrefabSpawner spline = go.AddComponent<SplinePrefabSpawner>();
        }

        [MenuItem("WSM Game Studio/Spline Mesh Renderer/Create/Spline Mesh Renderer", false, 10)]
        [MenuItem("GameObject/WSM Game Studio/Spline Mesh Renderer", false, 10)]
        static void CreateNewSplineMeshRenderer(MenuCommand menuCommand)
        {
            GameObject go = CreateAndSelectNewObject(menuCommand, "SplineMeshRenderer");

            GameObject aux1 = new GameObject("Aux1");
            GameObject aux2 = new GameObject("Aux2");
            aux1.transform.SetParent(go.transform);
            aux2.transform.SetParent(go.transform);

            Spline spline = go.AddComponent<Spline>();
            ApplyDefaultTheme(spline);

            SplineMeshRenderer splineMeshRenderer = go.AddComponent<SplineMeshRenderer>();
            splineMeshRenderer.Spline = spline;
        }

        /// <summary>
        /// Create and select new object on the Hierarchy
        /// </summary>
        /// <param name="menuCommand"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static GameObject CreateAndSelectNewObject(MenuCommand menuCommand, string name)
        {
            Vector3 worldPos = Vector3.zero;

            if (SceneView.lastActiveSceneView.camera != null)
            {
                float distanceToGround;
                Ray worldRay = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                groundPlane.Raycast(worldRay, out distanceToGround);
                worldPos = worldRay.GetPoint(distanceToGround);
            }

            // Create a custom game object
            GameObject newObject = new GameObject(name);
            newObject.transform.position = worldPos;
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(newObject, menuCommand.context as GameObject);
            // Ensure Unique naming for this object
            GameObjectUtility.EnsureUniqueNameForSibling(newObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(newObject, "Create " + newObject.name);
            Selection.activeObject = newObject;
            return newObject;
        }

        /// <summary>
        /// Tries to locate and applies default UI theme
        /// </summary>
        /// <param name="spline"></param>
        private static void ApplyDefaultTheme(Spline spline)
        {
            // Try to locate default theme
            string path = "Assets/WSM Game Studio/Spline Mesh Renderer/Themes/SMR-Default-Theme.asset";
            SMR_Theme defaultTheme = AssetDatabase.LoadAssetAtPath(path, typeof(SMR_Theme)) as SMR_Theme;

            if (defaultTheme != null)
                spline.Theme = defaultTheme;
        }
    } 
}
