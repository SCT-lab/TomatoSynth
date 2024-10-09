using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Camerasystem : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of camera movement
    public float yStartPos = 8.4f; // Start height (8.4 units)
    public float yInitialStep = 0.55f; // First vertical movement step (55 cm)
    public float yStep = 0.45f; // Vertical steps (45 cm each)
    public float zStart = 0.58f; // Start position in the z-axis
    public float zEnd = -100.8f; // End position in the z-axis
    public float xStartLeft = -4.54f; // Starting x position for left-side rows
    public float xEndRight = 81.44f; // Ending x position for right-side rows
    public int numRows = 10; // Number of rows to cover
    public int numLevels = 5; // Number of vertical levels (1 initial + 4 additional)

    public GameObject cameraAngle;

    public Menu2 menu2Reference;
    public Positioning_tomatoes tomatoPos = null;

    public List<Vector3> waypoints = new List<Vector3>();
    public int currentWaypointIndex = 0;

    public GameObject restartButton;

    public RecordingScreenshots recordingScript;
    public Menu2 menu2Script;
    
    public TMP_Text wayPointText;

    void Start()
    {
        Random.InitState(1);
        tomatoPos = FindObjectOfType<Positioning_tomatoes>(); 
        StartCoroutine(StartAfterDelay());
    }

    IEnumerator StartAfterDelay()
    {
        yield return new WaitForSeconds(0); // Adjust delay if needed for large model loading
        InitializeWaypoints();
        StartCoroutine(FollowPath());
    }

    void InitializeWaypoints()
    {
        waypoints.Clear();
        //float[] yPositions = { 8.4f, 14f, 17f, 19f, 21f, 23f };
        float[] xPositions = { -3.12f, 7.88f, 17.88f, 24.88f, 33.88f, 49.835f, 58.88f, 67.88f, 76.88f, 85.88f };
        float[] yPositions = {
            8.4f,     // Starting position 0.75 scale
            10.3725f, // 55 cm
            13.71f,   // 1st 45 cm
            17.0475f, // 2nd 45 cm
            20.385f,  // 3rd 45 cm
            23.7225f  // 4th 45 cm
        };



        float startZ = 0.58f;  // Start z position
        float endZ = -100f;    // End z position

        // Iterate through the rows
        for (int row = 0; row < 10; row++)
        {
            // Calculate x position based on the row (adjust according to your needs)
            //float xPosition = row % 2 == 0 ? -7.1f : -4f;  // Alternate x position based on row index


                // Move to start position (z=0.58)
            waypoints.Add(new Vector3(xPositions[row], yPositions[0], startZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[0], endZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[1], endZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[1], startZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[2], startZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[2], endZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[3], endZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[3], startZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[4], startZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[4], endZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[5], endZ));
            waypoints.Add(new Vector3(xPositions[row], yPositions[5], startZ));

            // Move up to the next row's starting position (this happens after the last position of the current row)
            if (row < 9)
            {
                float nextXPosition = xPositions[row + 1];
                float nextYPosition = yPositions[0];
                waypoints.Add(new Vector3(nextXPosition, nextYPosition, startZ));
            }
        }

        Debug.Log(waypoints);
    }

    IEnumerator FollowPath()
    {
        while (currentWaypointIndex < waypoints.Count)
        {
            Vector3 targetPosition = waypoints[currentWaypointIndex];
            yield return StartCoroutine(MoveToTarget(targetPosition));
            currentWaypointIndex++;
            wayPointText.text = currentWaypointIndex.ToString("F0");

            if (currentWaypointIndex < waypoints.Count && waypoints[currentWaypointIndex].y == waypoints[currentWaypointIndex - 1].y)
            { 
            ChangeCameraAngle();
            }  
            if(tomatoPos.activateGT)
            {
                ManageRowVisibility();    
            }             

            Debug.Log($"Moving to waypoint {currentWaypointIndex}: {targetPosition}");
        }

        if (currentWaypointIndex == waypoints.Count)
        {
            restartButton.SetActive(true);
        }
    }

    IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.005f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            Boundaries();
            yield return null;
        }
        transform.position = targetPosition;
    }

    void Boundaries()
    {
        float[] yPositions = {
            8.4f,     // Starting position 0.75 scale
            10.3725f, // 55 cm
            13.71f,   // 1st 45 cm
            17.0475f, // 2nd 45 cm
            20.385f,  // 3rd 45 cm
            23.7225f  // 4th 45 cm
        };

        float clampedZ = Mathf.Clamp(transform.position.z, zEnd, zStart);
        float clampedY = Mathf.Clamp(transform.position.y, yPositions[0], yPositions[yPositions.Length - 1]);


        if (transform.position.z != clampedZ || transform.position.y != clampedY)
        {
            transform.position = new Vector3(transform.position.x, clampedY, clampedZ);
            
            if (transform.position.z < zEnd - 0.5f || transform.position.z > zStart + 0.5f)
            {
                currentWaypointIndex--;
            }
        }
    }

    void ChangeCameraAngle()
    {
        float yCameraRotation = 90 + Random.Range(-25f, 25f);
        cameraAngle.transform.rotation = Quaternion.Euler(0, yCameraRotation, 0);
    }

    public void ManageRowVisibility()
    {
        // Define the X positions for each row based on the provided coordinates
        float[] rowXPositions = { -3.12f, 7.88f, 17.88f, 24.88f, 33.88f, 49.835f, 58.88f, 67.88f, 76.88f, 85.88f };

        if(transform.position.x == -3.12f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[0].gameObject.SetActive(true);
        }
        if(transform.position.x == 7.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[1].gameObject.SetActive(true);
        }
        if(transform.position.x ==  17.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[2].gameObject.SetActive(true);
        }
        if(transform.position.x == 24.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[3].gameObject.SetActive(true);
        }
        if(transform.position.x == 33.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[4].gameObject.SetActive(true);
        }
        if(transform.position.x == 49.835f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[5].gameObject.SetActive(true);
        }
        if(transform.position.x == 58.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[6].gameObject.SetActive(true);
        }
        if(transform.position.x == 67.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[7].gameObject.SetActive(true);
        }
        if(transform.position.x == 76.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[8].gameObject.SetActive(true);
        }
        if(transform.position.x == 85.88f)
        {
            for(int i = 0; i < tomatoPos.plantRows1.Length; i++)
            {
                tomatoPos.plantRows1[i].gameObject.SetActive(false);
            }
            tomatoPos.plantRows1[9].gameObject.SetActive(true);
        }
    }

    public void RecordingMovement()
    {
        if (waypoints.Count >= 0)
        {
            transform.position = waypoints[0];
            currentWaypointIndex = 0;
            menu2Script.ChangeSpeed(3f);

            StartRecording();
        }
    }

    void StartRecording()
    {
        recordingScript.StartRecording(true);
    }
}
