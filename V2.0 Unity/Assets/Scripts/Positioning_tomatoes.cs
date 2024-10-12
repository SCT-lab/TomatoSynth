using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// script to position the tomatoes within the greenhouse 
public class Positioning_tomatoes : MonoBehaviour
{
    public bool activateGT = false;
    public bool IsFSPM = false;
    public bool ShowAllRows = true;

    public GameObject Plant1_Comp1;
    public GameObject Plant2_Comp1;
    public GameObject Plant3_Comp1;
    public GameObject Plant1_Comp2;
    public GameObject Plant2_Comp2;
    public GameObject Plant3_Comp2;
    public GameObject Plant1_Comp3;
    public GameObject Plant2_Comp3;
    public GameObject Plant3_Comp3;
    public GameObject Plant1_Comp4;
    public GameObject Plant2_Comp4;
    public GameObject Plant3_Comp4;

    public GameObject Plant1gt_Comp1;
    public GameObject Plant2gt_Comp1;
    public GameObject Plant3gt_Comp1;
    public GameObject Plant1gt_Comp2;
    public GameObject Plant2gt_Comp2;
    public GameObject Plant3gt_Comp2;
    public GameObject Plant1gt_Comp3;
    public GameObject Plant2gt_Comp3;
    public GameObject Plant3gt_Comp3;
    public GameObject Plant1gt_Comp4;
    public GameObject Plant2gt_Comp4;
    public GameObject Plant3gt_Comp4;

    private List<GameObject> plant_non_gt = new List<GameObject>();
    private List<GameObject> plant_gt = new List<GameObject>();
    //public GameObject Greenhouse;

    public GameObject[,] plantRows = new GameObject[4,6];
    public Transform[] plantRows1;

    public Material plantOriginal;
    public Material plantOriginal1;
    public Material plantGT;

    public Material skyBoxOriginal;
    public Material skyBoxOriginal1;
    public Material skyBoxBlack;
    
    public GameObject assetGH;

    
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(1);
        plantOriginal.CopyPropertiesFromMaterial(plantOriginal1);
        skyBoxOriginal.CopyPropertiesFromMaterial(skyBoxOriginal1);
        
        //GameObject GrHouse = Instantiate(Greenhouse, new Vector3(158.5f, -0.36f, 30.3f), Quaternion.Euler(0, 0, 0)); //Position greenhouse
        //GrHouse.transform.localScale = new Vector3(5.5f, 5.5f, 5.5f); //Scale greenhouse
        
        //List<GameObject> plantsCompartment1 = new List<GameObject> { Plant1, Plant2, Plant3 }; //Use this when using one type of plants in the whole greenhouse

        List<List<GameObject>> plants = new List<List<GameObject>> //List of lists consisting plants which are used for each compartment
        {
            new List<GameObject> {Plant1_Comp1, Plant2_Comp1, Plant3_Comp1},
            new List<GameObject> { Plant1_Comp2, Plant2_Comp2, Plant3_Comp2},
            new List<GameObject> { Plant1_Comp3, Plant2_Comp3, Plant3_Comp3},
            new List<GameObject> { Plant1_Comp4, Plant2_Comp4, Plant3_Comp4},
        };

        List<List<GameObject>> plants_gt = new List<List<GameObject>> //List of lists consisting plants which are used for each compartment
        {
            new List<GameObject> {Plant1gt_Comp1, Plant2gt_Comp1, Plant3gt_Comp1},
            new List<GameObject> { Plant1gt_Comp2, Plant2gt_Comp2, Plant3gt_Comp2},
            new List<GameObject> { Plant1gt_Comp3, Plant2gt_Comp3, Plant3gt_Comp3},
            new List<GameObject> { Plant1gt_Comp4, Plant2gt_Comp4, Plant3gt_Comp4},
        };

        List<List<float>> startLocations = new List<List<float>> //Startlocations of the rows due to 4 different compartments 
        {
            new List<float> { 0.0f, 0.0f },
            new List<float> { 50.0f, 0.0f },
            new List<float> { 0.0f, -60.0f },
            new List<float> { 50.0f, -60.0f }
        };
        for (var b = 0; b < 4; b++) 
        {
            float startLocationx = startLocations[b][0];
            float startLocationz = startLocations[b][1];

            for (var a = 0; a < 5; a++) // 6 rows for each compartment
            {
                GameObject newRow = new GameObject($"Row {b}{a}");
                newRow.transform.parent = transform;
                plantRows[b, a] = newRow;

                for (var i = 0; i < 5; i++) // 5 plants for each row
                {
                    GameObject currentPlantPrefab = plants[b][i % plants[b].Count]; // Picking plants from the list
                    GameObject currentPlantPrefab1 = plants_gt[b][i % plants_gt[b].Count]; // Picking plants from the list
                    //float rotationAngle = (a * 15.0f) + (i * 60.0f); // Creating variation in the rotation of plants
                    float rotationAngle = (a * Random.Range(-15f, 15f)) + (i * Random.Range(-60f, 60f)); // Creating variation in the rotation of plants

                    float xValue = startLocationx + (a * 0.1f) + (i * 9.0f); // Determination of x position of each plant
                    float zValue = startLocationz + (a * -9.7f); // Determination of z position
                    
                    // Instantiate the entire plant prefab (which includes the pot and plant)
                    GameObject newPlantInstance = Instantiate(currentPlantPrefab, new Vector3(xValue, 7, zValue), Quaternion.identity, newRow.transform);
                    newPlantInstance.transform.SetParent(newRow.transform, false);
                    plant_non_gt.Add(newPlantInstance);
                    
                    GameObject newPlantInstance1 = Instantiate(currentPlantPrefab1, new Vector3(xValue, 7, zValue), Quaternion.identity, newRow.transform);
                    newPlantInstance1.transform.SetParent(newRow.transform, false);
                    plant_gt.Add(newPlantInstance1);

                    newPlantInstance1.SetActive(false);
                    // Find the plant part inside the instantiated prefab
                    Transform plantTransform = newPlantInstance.transform.Find("MainPlant");
                    Transform plantTransform1 = newPlantInstance1.transform.Find("MainPlant");
                    
                    if (plantTransform != null)
                    {
                        // Apply the rotation only to the plant part
                        Quaternion customRotation = Quaternion.Euler(0, rotationAngle, 0); // Use this one with realistic plant models

                        if (IsFSPM)
                        {
                            customRotation = Quaternion.Euler(-90, rotationAngle, 0); // Use this one with FSPM models
                        }
                        
                        plantTransform.localRotation = customRotation; // Rotate only the plant part
                        plantTransform1.localRotation = customRotation; // Rotate only the plant part
                    }
                }
            }
        }
        
/*         List<GameObject> foundRows = new List<GameObject>();
        
        for (int i = 0; i < 35; i++) // Assuming you have 35 rows named Row 00 to Row 34
        {
            GameObject row = GameObject.Find($"Row {i:D2}"); // Format to find "Row 00" to "Row 34"
            if (row != null)
            {
                foundRows.Add(row);
            }
        }
        plantRows1 = foundRows.ToArray(); */
        foreach (GameObject plant_gt_indiv in plant_gt)
            {
                plant_gt_indiv.SetActive(true);
            }

        OrganizeAndSortPlantsIntoRows();
        
        foreach (GameObject plant_gt_indiv in plant_gt)
            {
                plant_gt_indiv.SetActive(false);
            }
        AttachWindEffectToBranches();

    }

    private void AttachWindEffectToBranches()
    {
        foreach (GameObject branch in GameObject.FindObjectsOfType<GameObject>())
        {
            if (branch.name.StartsWith("Branch")) 
            {
                WindEffect windEffect = branch.AddComponent<WindEffect>();
                windEffect.windStrength = 0.003f;
                windEffect.windSpeed = 4f;
                windEffect.windDirection = 1f;
            }
            if (branch.name.StartsWith("Tomato_truss")) 
            {
                WindEffect windEffect = branch.AddComponent<WindEffect>();
                windEffect.windStrength = 0.003f;
                windEffect.windSpeed = 4f;
                windEffect.windDirection = 1f;
            }
            if (branch.name.StartsWith("Truss")) 
            {
                WindEffect windEffect = branch.AddComponent<WindEffect>();
                windEffect.windStrength = 0.003f;
                windEffect.windSpeed = 4f;
                windEffect.windDirection = 1f;
            }
        }
    }

    public void SetRowActive(int row, int col)
    {
        if(ShowAllRows == true)
        {
            return;
        }

        for(int i = 0; i < 2; i++)
        {
            for(int j = 0; j < 3; j++)
            {
                plantRows[i,j].SetActive(false);
            }
        }

        plantRows[row,col].SetActive(true);
    }
    
    public void ActivateGT(bool activate_gt_button)
    {
        activateGT = activate_gt_button;
        

            foreach (GameObject plant_non_gt_indiv in plant_non_gt)
            {
                plant_non_gt_indiv.SetActive(!activateGT);
            }

            foreach (GameObject plant_gt_indiv in plant_gt)
            {
                plant_gt_indiv.SetActive(activateGT);
            }

        if (activateGT)
        {
            plantOriginal.CopyPropertiesFromMaterial(plantGT);  // GT material is active
            skyBoxOriginal.CopyPropertiesFromMaterial(skyBoxBlack);  // GT material is active
            assetGH.SetActive(false);
        }
        else
        {
            plantOriginal.CopyPropertiesFromMaterial(plantOriginal1); // Original non-GT material is active
            skyBoxOriginal.CopyPropertiesFromMaterial(skyBoxOriginal1);  // GT material is active
            assetGH.SetActive(true);
        }
    }

public void OrganizeAndSortPlantsIntoRows()
{
    // Find all plants in the scene with the tag "Plant_GT"
    GameObject[] allPlants = GameObject.FindGameObjectsWithTag("Plant_GT");

    // Sort the plants by their x-coordinate
    List<GameObject> sortedPlants = new List<GameObject>(allPlants);
    sortedPlants.Sort((plant1, plant2) => plant1.transform.position.x.CompareTo(plant2.transform.position.x));

    // Initialize the rows based on the GameObjects named Row0, Row1, ..., Row9
    // Create a list of row GameObjects
    List<Transform> rows = new List<Transform>();
    for (int i = 0; i < 10; i++)
    {
        GameObject row = GameObject.Find($"Row{i}");
        if (row != null)
        {
            rows.Add(row.transform); // Corrected to use row.transform
        }
        else
        {
            Debug.LogError($"Row{i} not found!");
            return;
        }
    }

    // Assign sorted plants to rows
    int plantsPerRow = sortedPlants.Count / 10; // Assuming 10 rows

    int plantIndex = 0;

    for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
    {
        Transform currentRow = rows[rowIndex];

        // Calculate the number of plants for the current row
        int currentRowPlantCount = plantsPerRow;
        if (rowIndex < sortedPlants.Count % 10) // Distribute any remainder plants to the first few rows
        {
            currentRowPlantCount++;
        }

        // Place each plant under the current row
        for (int i = 0; i < currentRowPlantCount; i++)
        {
            if (plantIndex < sortedPlants.Count)
            {
                GameObject currentPlant = sortedPlants[plantIndex];

                // Set the current plant's parent to the current row
                currentPlant.transform.SetParent(currentRow, false); // 'false' keeps the local position unchanged

                plantIndex++;
            }
            else
            {
                Debug.LogWarning("Not enough plants to fill all rows.");
                return;
            }
        }
    }

    Debug.Log("Plants organized into rows without changing their positions.");
    }
}

