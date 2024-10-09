using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

// script for a virtual camera to follow a rectangular path within a greenhouse
public class Camerasystem : MonoBehaviour
{
    public float moveSpeed = 5f; //Speed of camera movement
    public float xLimit = 40f;
    public float xStartPos = 0f;
    public float yLimit = 15f;
    public float yStartPos = 2f;
    // Start is called before the first frame update

    public Positioning_tomatoes tomatoPos = null;
    int randomSeed = 0;
    void Start()
    {
        StartCoroutine(StartAfterDelay());
    }

    IEnumerator StartAfterDelay()
    {
        yield return new WaitForSeconds(0); // Wait for x seconds, maybe needed for large model to be loaded first
        StartCoroutine(FollowRectangularPath());
    }

    List<List<float>> compartmentPositions = new List<List<float>> //Startlocation in each of the 4 compartments 
        {
            new List<float> { 0.0f, 0.0f },
            new List<float> { 60.0f, 0.0f },
            new List<float> { 0.0f, -66.5f },
            new List<float> { 60.0f, -66.5f }
        };
    private bool shouldContinue = true;
    IEnumerator FollowRectangularPath()
    {
        for (var c = 0; c < 4; c++)
        {
            float compartmentPositionx = compartmentPositions[c][0];
            float compartmentPositionz = compartmentPositions[c][1];
            for (var b = 0; b < 6; b++)
            {
                tomatoPos.SetRowActive(c,b);

                for (var a = 0; a < 2; a++)
                {
                    shouldContinue = true;
                    Random.InitState(randomSeed);
                    float xStart = compartmentPositionx + xStartPos;
                    float zStart = compartmentPositionz + (-9.7f * b);// + Random.Range(-3.0f, 0.0f);
                    transform.position = new Vector3(xStart, yStartPos, zStart);
                    float yCameraRotation = (-180f * a) + Random.Range(-25f, 25f);
                    transform.rotation = Quaternion.Euler(0, yCameraRotation, 0);
                    randomSeed++;
                    while (shouldContinue)
                    {
                        for (var i = 1; i < 4; i++)
                        {
                            // Move to the right
                            while (transform.position.x < (xLimit + compartmentPositionx))
                            {
                                MoveCamera(Vector3.right);
                                yield return null;
                            }

                            if (i == 3)
                            {
                                shouldContinue = false;
                            }

                            // Move up 
                            while (transform.position.y < yLimit / 3 * i)
                            {
                                MoveCamera(Vector3.up);
                                yield return null;
                            }

                            // Move to the left till x = xStart
                            while (transform.position.x > xStart)
                            {
                                MoveCamera(Vector3.left);
                                yield return null;
                            }

                            // Move up again with 1/6 height
                            while (transform.position.y < yLimit / 3 * (i + 0.5f))
                            {
                                MoveCamera(Vector3.up);
                                yield return null;
                            }
                        }
                    }
                }
            }
        }
    }

    void MoveCamera(Vector3 direction)
    {
        transform.position += direction * moveSpeed * Time.deltaTime;
    }
}