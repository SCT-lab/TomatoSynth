using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionCamera : MonoBehaviour
{
    public GameObject redArrow;
    public GameObject cameraPosition;
    public float testY = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(1);
        testY = 30f - cameraPosition.transform.position.y;
        StartCoroutine(CheckPosition());
    }

    IEnumerator CheckPosition()
    {
        // Check the position of hte camera module and make sure that the Y value of the red arrow doesn't change when the camera moves up or down
        while (true) 
            {
            Vector3 arrowPosition = redArrow.transform.position; 
            arrowPosition.y = 30f;
            arrowPosition.x = cameraPosition.transform.position.x;
            arrowPosition.z = cameraPosition.transform.position.z;
            redArrow.transform.position = arrowPosition;

            yield return null;

            }
    }
}
