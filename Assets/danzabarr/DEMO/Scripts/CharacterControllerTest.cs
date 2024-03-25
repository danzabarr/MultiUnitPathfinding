using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControllerTest : MonoBehaviour
{
    public Vector3 velocity;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(transform.position + velocity * Time.deltaTime, Vector3.down, out RaycastHit hitInfo, 1))
        {
            Vector3 normal = hitInfo.normal;
            float angleFromUp = Vector3.Angle(Vector3.up, normal);

            // find the direction with the least resistance
            Vector3 direction = Vector3.Cross(Vector3.up, normal);
            Vector3 tangent = Vector3.Cross(normal, direction);

            // find the velocity in the direction of the least resistance
            velocity += Vector3.Project(velocity, tangent);
        }

        transform.position += velocity * Time.deltaTime;

        if (Input.GetKey(KeyCode.W))
			velocity += transform.forward * 5 * Time.deltaTime;

        if (Input.GetKey(KeyCode.S))
            velocity -= transform.forward * 5 * Time.deltaTime;

        if (Input.GetKey(KeyCode.A))
            transform.Rotate(0, -90 * Time.deltaTime, 0);

        if (Input.GetKey(KeyCode.D))
            transform.Rotate(0, 90 * Time.deltaTime, 0);
    }
}
