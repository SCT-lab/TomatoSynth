using UnityEngine;
using UnityEditor;
using System.IO;

namespace WSMGameStudio.Splines
{
    public class BakeMeshWindow : EditorWindow
    {
        private string _prefabName;
        private string _outputDirectory = string.Empty;
        private string _selectedFolder = string.Empty;
        private BakingBehaviour _bakingBehaviour;
        private bool _saveSpline = true;
        private bool _includeCaps = true;
        private string _defaultDiretory = "WSM Game Studio/Spline Mesh Renderer/Baked Meshes/";
        private string _txtMessage;
        private Color _txtColor = Color.black;
        private GUIStyle _menuBoxStyle;

        private GUIStyle _errorMessageStyle;

        [MenuItem("WSM Game Studio/Spline Mesh Renderer/Utilities/Mesh Baker", false, 11)]
        [MenuItem("Window/WSM Game Studio/Mesh Baker", false, 11)]
        public static void ShowWindow()
        {
            BakeMeshWindow currentWindow = GetWindow<BakeMeshWindow>("Mesh Baker");
            currentWindow.minSize = new Vector2(580, 160);
        }

        /// <summary>
        /// Render Window
        /// </summary>
        private void OnGUI()
        {
            //Set up the box style if null
            if (_menuBoxStyle == null)
            {
                _menuBoxStyle = new GUIStyle(GUI.skin.box);
                _menuBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                _menuBoxStyle.fontStyle = FontStyle.Bold;
                _menuBoxStyle.alignment = TextAnchor.UpperLeft;
            }

            GUILayout.Label("Mesh Baker", EditorStyles.boldLabel);

            if (_outputDirectory == string.Empty)
                _outputDirectory = _defaultDiretory;

            GUILayout.BeginVertical(_menuBoxStyle);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(110));

            EditorGUILayout.LabelField("Name", GUILayout.Width(110));
            EditorGUILayout.LabelField("Baking Behaviour", GUILayout.Width(110));
            EditorGUILayout.LabelField("Save Spline", GUILayout.Width(110));
            EditorGUILayout.LabelField("Output Directory", GUILayout.Width(110));

            GUILayout.EndVertical();
            GUILayout.BeginVertical();

            _prefabName = EditorGUILayout.TextField(_prefabName);
            _bakingBehaviour = (BakingBehaviour)EditorGUILayout.EnumPopup(_bakingBehaviour);

            GUILayout.BeginHorizontal();
            _saveSpline = EditorGUILayout.Toggle(_saveSpline);
            _includeCaps = EditorGUILayout.Toggle("Include Caps", _includeCaps);
            GUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(true))
            {
                _outputDirectory = EditorGUILayout.TextField(string.Empty, _outputDirectory);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Browse Folder"))
            {
                _selectedFolder = EditorUtility.OpenFolderPanel("Select Output Directory", _defaultDiretory, "");
                if (_selectedFolder != string.Empty)
                    _outputDirectory = string.Concat(_selectedFolder.Replace(string.Format("{0}/", Application.dataPath), string.Empty), "/");
            }

            if (GUILayout.Button("Bake"))
            {
                Bake();
            }

            GUILayout.EndVertical();

            _errorMessageStyle = new GUIStyle();
            _errorMessageStyle.normal.textColor = _txtColor;
            GUILayout.Label(_txtMessage, _errorMessageStyle);
        }

        /// <summary>
        /// Bake generated meshes into a prefab
        /// </summary>
        private void Bake()
        {
            _txtMessage = string.Empty;

            #region VALIDATION
            if (_prefabName == string.Empty)
            {
                ShowMessage("Please, enter a name for your prefab before proceding", MessageType.Error);
                return;
            }

            if (_outputDirectory == string.Empty)
            {
                ShowMessage("Please, select an Output Directory before proceding", MessageType.Error);
                return;
            }

            GameObject selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject == null)
            {
                ShowMessage("No object selected", MessageType.Error);
                return;
            }

            SplineMeshRenderer splineMeshRenderer = selectedGameObject.GetComponent<SplineMeshRenderer>();
            if (splineMeshRenderer == null)
            {
                ShowMessage("Selected object is not a Spline Mesh Renderer", MessageType.Error);
                return;
            }

            if(splineMeshRenderer.Spline == null)
            {
                ShowMessage("Selected object Spline reference not found", MessageType.Error);
                return;
            }

            if (splineMeshRenderer.GeneratedMeshes == null || splineMeshRenderer.GeneratedMeshes.Length == 0)
            {
                ShowMessage("Generated mesh not found. Please generate a mesh before proceding.", MessageType.Error);
                return;
            }

            #endregion

            DirectoryInfo info = new DirectoryInfo(Application.dataPath);
            string prefabFolderPath = Path.Combine(info.Name, _outputDirectory);
            string prefabDataFolderPath = Path.Combine(prefabFolderPath, _prefabName + "_Data/");
            string prefabFilePath = Path.Combine(prefabFolderPath, _prefabName + "_prefab.prefab");

            //Check folder existence
            if (!Directory.Exists(prefabFolderPath))
            {
                ShowMessage(string.Format("Folder does not exist: {0}", prefabFolderPath), MessageType.Error);
                return;
            }

            //Overwrite dialog
            if (Exists(prefabFilePath))
            {
                if (!ShowOverwriteDialog(_prefabName))
                    return;
            }

            //Create prefab data folder
            if (!Directory.Exists(prefabDataFolderPath))
            {
                Directory.CreateDirectory(prefabDataFolderPath);
            }

            //Create parent object
            GameObject sceneObject = new GameObject(_prefabName);
            LODGroup lodGroup = sceneObject.AddComponent<LODGroup>();
            sceneObject.transform.position = splineMeshRenderer.transform.position;
            sceneObject.transform.rotation = splineMeshRenderer.transform.rotation;
            sceneObject.layer = splineMeshRenderer.gameObject.layer;
            sceneObject.tag = splineMeshRenderer.gameObject.tag;

            // Create mesh LODs
            LOD[] lods = new LOD[splineMeshRenderer.GeneratedMeshes.Length];
            for (int i = 0; i < splineMeshRenderer.GeneratedMeshes.Length; i++)
            {
                string meshFilePath = Path.Combine(prefabDataFolderPath, _prefabName + "_" + splineMeshRenderer.GeneratedMeshes[i].GetGameObject.name + ".asset");
                Mesh meshInstance = (Mesh)Instantiate(splineMeshRenderer.GeneratedMeshes[i].Mesh);
                meshInstance = SaveMeshFile(meshInstance, meshFilePath);

                GameObject meshChild = new GameObject(splineMeshRenderer.GeneratedMeshes[i].GetGameObject.name);
                meshChild.transform.SetParent(sceneObject.transform);
                meshChild.transform.localPosition = Vector3.zero;
                meshChild.transform.localRotation = Quaternion.identity;
                meshChild.layer = splineMeshRenderer.GeneratedMeshes[i].GetGameObject.layer;
                meshChild.tag = splineMeshRenderer.GeneratedMeshes[i].GetGameObject.tag;

                MeshFilter meshFilter = meshChild.AddComponent<MeshFilter>();
                MeshRenderer meshRend = meshChild.AddComponent<MeshRenderer>();

                meshFilter.sharedMesh = meshInstance;
                meshRend.sharedMaterials = splineMeshRenderer.GeneratedMeshes[i].GetGameObject.GetComponent<MeshRenderer>().sharedMaterials;

                Renderer[] renderers = new Renderer[1];
                renderers[0] = meshChild.GetComponent<Renderer>();
                float transitionHeight = (i < splineMeshRenderer.GeneratedMeshes.Length - 1) ? (1.0f / (i + 1.5f)) : splineMeshRenderer.MeshGenerationProfile.cullingRatio;
                lods[i] = new LOD(transitionHeight, renderers);
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            // Create Colliders
            for (int i = 0; i < splineMeshRenderer.GeneratedColliders.Length; i++)
            {
                string meshFilePath = Path.Combine(prefabDataFolderPath, _prefabName + "_" + splineMeshRenderer.GeneratedColliders[i].GetGameObject.name + ".asset");
                Mesh meshInstance = (Mesh)Instantiate(splineMeshRenderer.GeneratedColliders[i].Mesh);
                meshInstance = SaveMeshFile(meshInstance, meshFilePath);

                GameObject newCollider = new GameObject(splineMeshRenderer.GeneratedColliders[i].GetGameObject.name);
                newCollider.transform.SetParent(sceneObject.transform);
                newCollider.transform.localPosition = Vector3.zero;
                newCollider.transform.localRotation = Quaternion.identity;
                newCollider.layer = splineMeshRenderer.GeneratedColliders[i].GetGameObject.layer;
                newCollider.tag = splineMeshRenderer.GeneratedColliders[i].GetGameObject.tag;

                MeshCollider meshCollider = newCollider.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshInstance;
                meshCollider.sharedMaterial = splineMeshRenderer.GeneratedColliders[i].GetGameObject.GetComponent<MeshCollider>().sharedMaterial;

                newCollider.AddComponent<SMR_IgnoredObject>();
            }

            // Create endpoint reference
            GameObject endPoint = new GameObject("EndPoint");
            endPoint.transform.SetParent(sceneObject.transform);
            Vector3 lastPoint = splineMeshRenderer.Spline.ControlPointsPositions[splineMeshRenderer.Spline.ControlPointsPositions.Length - 1];
            Vector3 penultimatePoint = splineMeshRenderer.Spline.ControlPointsPositions[splineMeshRenderer.Spline.ControlPointsPositions.Length - 2];
            Vector3 lookTarget = splineMeshRenderer.transform.TransformPoint(lastPoint + (lastPoint - penultimatePoint));
            endPoint.transform.localPosition = lastPoint;
            endPoint.transform.LookAt(lookTarget);

            BakedSegment bakedSegment = sceneObject.AddComponent<BakedSegment>();
            bakedSegment.EndPoint = endPoint.transform;

            // Save spline
            if (_saveSpline)
            {
                GameObject splineBackup = new GameObject(string.Format("{0}_Spline", _prefabName));
                splineBackup.transform.SetParent(sceneObject.transform);
                splineBackup.transform.localPosition = Vector3.zero;
                splineBackup.transform.localRotation = Quaternion.identity;

                Spline spline = splineBackup.AddComponent<Spline>();

                UnityEditorInternal.ComponentUtility.CopyComponent(splineMeshRenderer.Spline);
                UnityEditorInternal.ComponentUtility.PasteComponentValues(spline);
            }

            // Caps
            if (_includeCaps)
            {
                SMR_MeshCapTag[] caps = selectedGameObject.GetComponentsInChildren<SMR_MeshCapTag>();

                if (caps != null)
                {
                    foreach (SMR_MeshCapTag cap in caps)
                    {
                        GameObject newCap = Instantiate(cap.gameObject, sceneObject.transform);
                        newCap.name = cap.name;
                    }
                }
            }

            //Save prefab as .prefab file
            bool success = false;
            GameObject prefab = SavePrefabFile(sceneObject, prefabFilePath, out success);

            if (success)
            {
                switch (_bakingBehaviour)
                {
                    case BakingBehaviour.ExportAndReplace:
                        sceneObject.transform.SetParent(splineMeshRenderer.transform.parent);
                        sceneObject.transform.localPosition = splineMeshRenderer.transform.localPosition;
                        sceneObject.transform.localRotation = splineMeshRenderer.transform.localRotation;
                        splineMeshRenderer.gameObject.SetActive(false);
                        break;
                    case BakingBehaviour.JustExport:
                        GameObject.DestroyImmediate(sceneObject);
                        break;
                }

                ShowMessage(string.Format("Prefab {0} Created Succesfuly!", _prefabName), MessageType.Success);
            }
            else
                ShowMessage(string.Format("Could not create prefab.{0}Please check Console Window for more information.", System.Environment.NewLine), MessageType.Error);
        }

        #region Auxiliar Methods
        /// <summary>
        /// Save prefab as .prefab file
        /// </summary>
        /// <param name="prefabName"></param>
        /// <param name="prefabFilePath"></param>
        /// <returns></returns>
        private GameObject SavePrefabFile(GameObject sceneObject, string prefabFilePath, out bool success)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(sceneObject, prefabFilePath, InteractionMode.UserAction, out success);
            return prefab;
        }

        /// <summary>
        /// Save mesh as .asset file
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="meshFilePath"></param>
        /// <returns>Saved mesh reference</returns>
        private static Mesh SaveMeshFile(Mesh mesh, string meshFilePath)
        {
            MeshUtility.Optimize(mesh);
            AssetDatabase.CreateAsset(mesh, meshFilePath);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        /// <summary>
        /// Show message on Window
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        private void ShowMessage(string message, MessageType type)
        {
            _txtMessage = message;

            switch (type)
            {
                case MessageType.Success:
                    _txtColor = new Color(0, 0.5f, 0); //Dark green;
                    break;
                case MessageType.Warning:
                    _txtColor = Color.yellow;
                    break;
                case MessageType.Error:
                    _txtColor = Color.red;
                    break;
            }
        }

        /// <summary>
        /// Show overwrite dialog confirmation
        /// </summary>
        /// <param name="meshName"></param>
        /// <returns></returns>
        private bool ShowOverwriteDialog(string meshName)
        {
            return EditorUtility.DisplayDialog("Are you sure?",
                            string.Format("A prefab named {0} already exists. Do you want to overwrite it?", meshName.ToUpper()),
                            "Yes",
                            "No");
        }

        /// <summary>
        /// Check if file already exists
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool Exists(string filePath)
        {
            return File.Exists(filePath);
        }
        #endregion
    }
}
