using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//script to simulate rotation of the sun
public class DayandNight : MonoBehaviour
{

    public float degpersec = 6; //degrees per second 
    Vector3 rot = Vector3.zero;
    
    // Update is called once per frame
    void Update()
    {
        rot.x = degpersec * Time.deltaTime;
        transform.Rotate(rot, Space.World);
    }
}
