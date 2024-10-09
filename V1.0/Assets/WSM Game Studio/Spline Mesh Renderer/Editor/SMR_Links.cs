using System.IO;
using UnityEditor;
using UnityEngine;

namespace WSMGameStudio.Splines
{
    public class SMR_Links
    {
        [MenuItem("WSM Game Studio/Spline Mesh Renderer/Documentation")]
        static void OpenDocumentation()
        {
            string documentationFolder = "WSM Game Studio/Spline Mesh Renderer/Documentation/Spline Mesh Renderer v2.1.pdf";
            DirectoryInfo info = new DirectoryInfo(Application.dataPath);
            string documentationPath = Path.Combine(info.Name, documentationFolder);
            Application.OpenURL(documentationPath);
        }

        [MenuItem("WSM Game Studio/Spline Mesh Renderer/Write a Review")]
        static void Review()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/129857");
        }
    } 
}
