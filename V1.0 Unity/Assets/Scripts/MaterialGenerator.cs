using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

//Script to generate materials and store the RGB combinations in a csv-file
public class MaterialGenerator : MonoBehaviour
{
    
    public string folderPath = "Assets/Textures/GT_COLOURS"; // Folder path to save the materials
    public int numberOfMaterials = 100; // Number of materials to create

    void Start()
    {
        GenerateMaterials();
    }

    void GenerateMaterials()
    {
        List<string> csvData = new List<string>();
        for (int i = 0; i < numberOfMaterials; i++)
        {
            // Create a new material
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));

            // Assign RGB values to the color of the material
            //float r = (i * 3 + 10)/255;
            //float g = (i * 3 + 10)/255;
            //float b = (i * 3 + 10)/255;
            float r = Random.value;
            float g = Random.value;
            float b = Random.value;
            newMaterial.color = new Color(r, g, b);

            // Save the material as an asset
            string materialPath = folderPath + "/GT_" + i + ".mat";
            AssetDatabase.CreateAsset(newMaterial, materialPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int r_scaled = Mathf.RoundToInt(r * 255);
            int g_scaled = Mathf.RoundToInt(g * 255);
            int b_scaled = Mathf.RoundToInt(b * 255);

            csvData.Add($"{i},{r_scaled},{g_scaled},{b_scaled}");

            //Debug.Log("Material created and saved at: " + materialPath);
        }
        WriteCSVFile(csvData);
    }

    void WriteCSVFile(List<string> data)
    {
        // Adjust the file path as needed
        string filePath = "Assets/MaterialData.csv";

        // Check if the file exists, if not, create it and write the header
        /*if (!File.Exists(filePath))
        {
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.WriteLine("Index,Red,Green,Blue");
            }
        }*/

        // Append the data to the CSV file
        using (StreamWriter sw = File.AppendText(filePath))
        {
            sw.WriteLine("id, red, green, blue");
            foreach (string line in data)
            {
                sw.WriteLine(line);
            }
        }

        Debug.Log($"CSV file written to: {filePath}");
    }



}