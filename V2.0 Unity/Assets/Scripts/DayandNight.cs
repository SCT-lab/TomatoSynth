using System.Collections;
using UnityEngine;

// Script to simulate rotation of the sun and adjust skybox exposure
public class DayandNight : MonoBehaviour
{
    public float degpersec = 6; // Degrees per second for sun rotation
    public Material skyboxMaterial; // Skybox material reference
    public Light directionalLight; // Reference to the directional light
    public GameObject directionLightObject;
    public float dayExposure = 1.3f; // Exposure for daytime
    public float nightExposure = 0.1f; // Exposure for nighttime
    public float maxLightIntensity = 1.0f; // Max intensity of the light during the day
    public float minLightIntensity = 0.1f; // Min intensity of the light during the night

    public Material lightMaterial;

    private Vector3 rot = Vector3.zero;
    private float elapsedTime = 0f;

    void Start()
    {
        StartCoroutine(UpdateMaterial());
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate the sun
        rot.x = degpersec * Time.deltaTime;
        transform.Rotate(rot, Space.World);
    }

    IEnumerator UpdateMaterial()
    {
        while (true)
        {
            // Cycle time variable (0 to 1 and back)
            float t = Mathf.PingPong(elapsedTime / 30f, 1f);

            // Determine new skybox exposure
            float newExposure = (t < 0.5f) 
                ? Mathf.Lerp(nightExposure, dayExposure, t * 2)  // Morning to noon
                : Mathf.Lerp(dayExposure, nightExposure, (t - 0.5f) * 2); // Noon to night

            // Set the skybox exposure
            skyboxMaterial.SetFloat("_Exposure", newExposure);

            // Adjust directional light intensity
            float newLightIntensity = (t < 0.5f) 
                ? Mathf.Lerp(minLightIntensity, maxLightIntensity, t * 2)  // Morning to noon
                : Mathf.Lerp(maxLightIntensity, minLightIntensity, (t - 0.5f) * 2); // Noon to night

            // Set the light intensity
            directionalLight.intensity = newLightIntensity;

            // Handle internal and directional lights based on the current time of day
            if (t < 0.2f || t > 0.8f)
            {
                // It's early morning or late night
                lightMaterial.EnableKeyword("_EMISSION");
                directionalLight.enabled = false;
                directionLightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Reset rotation to 90 degrees
            }
            else
            {
                // It's daytime
                lightMaterial.DisableKeyword("_EMISSION");
                directionalLight.enabled = true;
            }

            // Update elapsed time
            elapsedTime += Time.deltaTime;

            // Reset elapsed time after a full cycle
            if (elapsedTime > 30f)
            {
                elapsedTime = 0;
            }

            yield return null;
        }
    }
}
