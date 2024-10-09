using UnityEngine;

public class WindEffect : MonoBehaviour
{
    public float windStrength = 0.1f; // Strength of the wind effect
    public float windSpeed = 1.0f; // Speed of the wind effect
    public float windDirection = 1.0f; // Direction of the wind effect (1 for Z, -1 for Y)

    private Vector3 originalPosition;

    void Start()
    {
        // Store the original position of the plant
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        // Calculate the new position using sine wave for smooth movement
        float yOffset = Mathf.Sin(Time.time * windSpeed) * windStrength;
        float zOffset = Mathf.Sin((Time.time * windSpeed) + windDirection) * windStrength;

        // Apply the offsets to the original position
        transform.localPosition = new Vector3(originalPosition.x, originalPosition.y + yOffset, originalPosition.z + zOffset);
    }
}
