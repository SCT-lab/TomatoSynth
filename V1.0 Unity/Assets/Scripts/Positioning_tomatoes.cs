using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// script to position the tomatoes within the greenhouse 
public class Positioning_tomatoes : MonoBehaviour
{
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
    public GameObject Greenhouse;

    private GameObject[,] plantRows = new GameObject[4,6];
    // Start is called before the first frame update
    void Start()
    {
        GameObject GrHouse = Instantiate(Greenhouse, new Vector3(158.5f, -0.36f, 30.3f), Quaternion.Euler(0, 0, 0)); //Position greenhouse
        GrHouse.transform.localScale = new Vector3(5.5f, 5.5f, 5.5f); //Scale greenhouse
        
        //List<GameObject> plantsCompartment1 = new List<GameObject> { Plant1, Plant2, Plant3 }; //Use this when using one type of plants in the whole greenhouse

        List<List<GameObject>> plants = new List<List<GameObject>> //List of lists consisting plants which are used for each compartment
        {
            new List<GameObject> {Plant1_Comp1, Plant2_Comp1, Plant3_Comp1},
            new List<GameObject> { Plant1_Comp2, Plant2_Comp2, Plant3_Comp2},
            new List<GameObject> { Plant1_Comp3, Plant2_Comp3, Plant3_Comp3},
            new List<GameObject> { Plant1_Comp4, Plant2_Comp4, Plant3_Comp4},
        };

        List<List<float>> startLocations = new List<List<float>> //Startlocations of the rows due to 4 different compartments 
        {
            new List<float> { 0.0f, 0.0f },
            new List<float> { 60.0f, 0.0f },
            new List<float> { 0.0f, -66.5f },
            new List<float> { 60.0f, -66.5f }
        };
        for (var b = 0; b < 4; b++) 
        {
            float startLocationx = startLocations[b][0];
            float startLocationz = startLocations[b][1];
            for (var a = 0; a < 6; a++) //6 rows for each compartment
            {
                GameObject newRow = new GameObject($"Row {b}{a}");
                newRow.transform.parent = transform;
                plantRows[b,a] = newRow;

                for (var i = 0; i < 15; i++) //15 plants for each row
                {
                    GameObject currentPlant = plants[b][i % plants[b].Count]; //Picking plants from the list
                    float rotationAngle = (a * 5.0f) + (i * 45.0f); //Creating variation in rotation of plants
                    Quaternion customRotation = Quaternion.Euler(0, rotationAngle, 0); // Use this one with realistic plant models
                    
                    if (IsFSPM == true)
                    {
                        customRotation = Quaternion.Euler(-90, rotationAngle, 0); // Use this one with FSPM models
                    }
                    
                    float xValue = startLocationx + (a * 0.1f) + (i * 3.0f); //Determination of xValue of each plant, also with some variation in each row
                    float zValue = startLocationz + (a * -9.7f); //determination of zValue, no variation 
                    Instantiate(currentPlant, new Vector3(xValue, 0, zValue), customRotation, newRow.transform); // Placement of plants in the row
                }
            }
        }
        

    }

    public void SetRowActive(int row, int col)
    {
        if(ShowAllRows == true)
        {
            return;
        }

        for(int i = 0; i < 4; i++)
        {
            for(int j = 0; j < 6; j++)
            {
                plantRows[i,j].SetActive(false);
            }
        }

        plantRows[row,col].SetActive(true);
    }
}

