using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    private static List<Boid> boids = new List<Boid>();
    public float radius = 1.0f;
    public float maxSpeed = 1.0f;
    public Vector2 velocity;
    public Vector2 acceleration;

    public float cohesionWeight = 1.0f;
    public float alignmentWeight = 1.0f;
    public float separationWeight = 1.0f;


    public Transform goal;


    // Start is called before the first frame update
    void Start()
    {
        boids.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 separation = Vector2.zero;
        int count = 0;

        foreach (Boid boid in boids)
        {
            if (boid != this)
            {
                float distance = Vector2.Distance(boid.transform.position.XZ(), transform.position.XZ());
                if (distance < radius)
                {
                    cohesion += boid.transform.position.XZ();
                    alignment += boid.velocity;
                    separation += transform.position.XZ() - boid.transform.position.XZ();
                    count++;
                }
            }
        }

        if (count > 0)
        {
            cohesion /= count;
            alignment /= count;
            separation /= count;
            cohesion = (cohesion - transform.position.XZ()).normalized;
            alignment = alignment.normalized;
            separation = separation.normalized;
        }

        acceleration = Vector2.zero;
        if (goal != null)
        {
            Vector2 desired = (goal.position - transform.position).XZ().normalized * maxSpeed;
            acceleration = (desired - velocity).normalized;
        }
        acceleration += cohesion * cohesionWeight;
        acceleration += alignment * alignmentWeight;
        acceleration += separation * separationWeight;

        velocity += acceleration * Time.deltaTime;
        velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        transform.position += velocity.X0Y() * Time.deltaTime;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
    }
}
