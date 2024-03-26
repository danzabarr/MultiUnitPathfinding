using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AgentState
{
	Controlled, // controlled by the player
	Idle,		// not moving
	Walking,	// moving to a waypoint
	Talking,	// in dialogue
}

public enum ControlMode
{
	/// <summary>
	/// Up moves the character forward, left and right turn the character gradually and down moves the character backwards
	/// </summary>
	Tank,
	/// <summary>
	/// Character freely moves in the chosen direction from the perspective of the camera
	/// </summary>
	Direct
}

[RequireComponent(typeof(Animator), typeof(Rigidbody))]
public class Agent : Waypoint//, IAgent<Node>
{


	// Inspector variables
	[SerializeField] AgentState agentState = AgentState.Idle;
	[SerializeField] Waypoint agentGoal;
	[SerializeField] List<Node> agentPath = new List<Node>();
	[SerializeField] ControlMode controlMode = ControlMode.Direct;
	[SerializeField] float movementSpeed = 2;
	[SerializeField] float rotationSpeed = 200;
	[SerializeField] float jumpForce = 4;
	[SerializeField] string idleAnimatorTrigger = "Idle_A";	
	[SerializeField] string walkAnimatorTrigger = "Walk";
	[SerializeField] string talkAnimatorTrigger = "Talk";


	// Cached component refs
	private Animator animator;
	private Rigidbody rigidBody;

	// Private variables
	float verticalInput = 0;
	float horizontalInput = 0;
	readonly float inputInterpolation = 10;
	readonly float sneakMultiplier = 0.33f;
	readonly float backwardSneakMultiplier = 0.16f;
	readonly float backwardMultiplier = 0.66f;
	bool grounded;
	Vector3 m_currentDirection = Vector3.zero;
	List<Collider> collisions = new List<Collider>();
	float jumpStartTime = 0;
	float jumpMinInterval = 0.25f;
	bool jumpInput = false;
	bool jumpingToSafety = false;
	Vector3 lastSafePosition;
	Vector3 unsafePosition; 

	void Awake()
	{
		animator = GetComponent<Animator>();
		rigidBody = GetComponent<Rigidbody>();
	}

	public override void Update()
	{
		base.Update(); // update the waypoint position

		switch (agentState)
		{
			case AgentState.Controlled:
				Controlled();
				break;
			case AgentState.Idle:
				Idle();
				break;
			case AgentState.Walking:
				Walking();
				break;
			case AgentState.Talking:
				Talking();
				break;
		}
	}

	void Walking()
	{
		if (HasPath())
		{
			Node next = GetNext();

			// If the next node is not visible, recalculate the path
			if (!map.IsVisible(transform.position.XZ(), next.position.XZ(), true, false, true))
			{
				// Try to find a new path to the target
				if (agentGoal != null && map.AStar(Node, agentGoal.Node, out List<Node> path))
				{
					// Set the new path
					SetPath(path);
					return;
				}

				// If we can't find a path, clear the current path
				ClearPath();
				return;
			}

			// Move towards the next node
			Vector3 direction = (next.position - transform.position).normalized;
			transform.position += direction * movementSpeed * Time.deltaTime;
			
			// Rotate smoothly
			Quaternion targetRotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, direction, Vector3.up), Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			
			// Animation triggers
			PlayAnimation(walkAnimatorTrigger);
			animator.SetFloat("MoveSpeed", movementSpeed);

			// If we're close enough to the next node, pop it off the path
			const float Epsilon = 0.01f;
			if (Vector3.Distance(transform.position, next.position) < Epsilon)
				Pop();
		}
		else
		{
			// If we don't have a path, stop moving
			PlayAnimation(idleAnimatorTrigger);
			animator.SetFloat("MoveSpeed", 0);

			// If we still have a target and no path, try to find a path to it
			if (agentGoal != null && Vector2.Distance(transform.position.XZ(), agentGoal.transform.position.XZ()) > 0.1f)
				PathTo(agentGoal.Node);
			
			// Otherwise go idle
			else
				agentState = AgentState.Idle;
		}
	}

	void Idle()
	{
		agentPath.Clear();
		PlayAnimation(idleAnimatorTrigger);
		animator.SetFloat("MoveSpeed", 0);
	}

	void Talking()
	{
		PlayAnimation(talkAnimatorTrigger);
		animator.SetFloat("MoveSpeed", 0);
	}

	void Controlled()
	{
		if (jumpingToSafety)
		{
			// parabolic arc to reset position
			float t = (Time.time - jumpStartTime) / 0.5f;
			if (t > 1)
			{
				jumpingToSafety = false;
				transform.position = lastSafePosition;
				rigidBody.velocity = Vector3.zero;
			}
			else
			{
				transform.position = Vector3.Lerp(unsafePosition, lastSafePosition, t) + Vector3.up * 0.5f * Mathf.Sin(t * Mathf.PI);
				return;
			}
		}

		// interact with SimpleDialogue components if they are in range
		if (Input.GetButtonDown("Fire1"))
		{
			Debug.DrawRay(transform.position, transform.forward * 2, Color.red, 2);
			if 
			(
				Physics.SphereCast(transform.position, 0.5f, transform.forward, out RaycastHit hit, 2) && 
				hit.collider.TryGetComponent<SimpleDialogue>(out var dialogue)
			) 
			dialogue.StartDialogue(this);
		}

		if (Input.GetKey(KeyCode.Space))
			jumpInput = true;

		if (grounded && transform.position.y > 0)
		{
			Vector2Int tile = transform.position.ToTileCoord();
			if (map.IsAccessible(tile))
				// snap to tile
				lastSafePosition = tile.X0Y() + Vector3.up * transform.position.y;
		}

		if (transform.position.y < -1 && !jumpingToSafety)
		{
			rigidBody.velocity = Vector3.zero;
			jumpingToSafety = true;
			unsafePosition = transform.position;
			jumpStartTime = Time.time;
		}
	}

	void FixedUpdate()
	{
		animator.SetBool("Grounded", grounded);

		// if the character is not moving, play the idle animation
		if (verticalInput == 0 && horizontalInput == 0)
			animator.SetTrigger(idleAnimatorTrigger);
		
		// otherwise play walk animation
		else
			animator.SetTrigger(walkAnimatorTrigger);

		// Disable controls when jumping
		if (!jumpingToSafety)
			return;

		switch (controlMode)
		{
			case ControlMode.Direct:
				DirectUpdate();
				break;

			case ControlMode.Tank:
				TankUpdate();
				break;

			default:
				Debug.LogError("Unsupported state");
				break;
		}

		jumpInput = false;
	}

	void TankUpdate()
	{
		float v = Input.GetAxis("Vertical");
		float h = Input.GetAxis("Horizontal");

		bool walk = Input.GetKey(KeyCode.LeftShift);

		v *= (v < 0) ? (walk ? backwardSneakMultiplier : backwardMultiplier) : (walk && v >= 0) ? sneakMultiplier : 1;

		verticalInput = Mathf.Lerp(verticalInput, v, Time.deltaTime * inputInterpolation);
		horizontalInput = Mathf.Lerp(horizontalInput, h, Time.deltaTime * inputInterpolation);

		transform.position += transform.forward * verticalInput * movementSpeed * Time.deltaTime;
		transform.Rotate(0, horizontalInput * rotationSpeed * Time.deltaTime, 0);

		animator.SetFloat("MoveSpeed", verticalInput);
		
		JumpingAndLanding();
	}

	void DirectUpdate()
	{
		float v = Input.GetAxis("Vertical");
		float h = Input.GetAxis("Horizontal");

		Transform camera = Camera.main.transform;

		if (Input.GetKey(KeyCode.LeftShift))
		{
			v *= sneakMultiplier;
			h *= sneakMultiplier;
		}

		verticalInput = Mathf.Lerp(verticalInput, v, Time.deltaTime * inputInterpolation);
		horizontalInput = Mathf.Lerp(horizontalInput, h, Time.deltaTime * inputInterpolation);

		Vector3 direction = camera.forward * verticalInput + camera.right * horizontalInput;

		float directionLength = direction.magnitude;
		direction.y = 0;
		direction = direction.normalized * directionLength;

		if (direction != Vector3.zero)
		{
			m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * inputInterpolation);

			transform.rotation = Quaternion.LookRotation(m_currentDirection);
			transform.position += m_currentDirection * movementSpeed * Time.deltaTime;

			animator.SetFloat("MoveSpeed", direction.magnitude);
		}

		JumpingAndLanding();
	}

	void JumpingAndLanding()
	{
		bool jumpCooldownOver = (Time.time - jumpStartTime) >= jumpMinInterval;

		if (jumpCooldownOver && grounded && jumpInput)
		{
			jumpStartTime = Time.time;
			rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		ContactPoint[] contactPoints = collision.contacts;
		for (int i = 0; i < contactPoints.Length; i++)
        {
            if (Vector3.Dot(contactPoints[i].normal, Vector3.up) <= 0.5f)
                continue;

            if (!collisions.Contains(collision.collider))
                collisions.Add(collision.collider);

            grounded = true;
        }
    }

	void OnCollisionStay(Collision collision)
	{
		ContactPoint[] contactPoints = collision.contacts;
		for (int i = 0; i < contactPoints.Length; i++)
			if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
			{
				grounded = true;
				if (!collisions.Contains(collision.collider))
					collisions.Add(collision.collider);
				return;
			}

		collisions.Remove(collision.collider);
		
		if (collisions.Count == 0) 
			grounded = false;
	}

	void OnCollisionExit(Collision collision)
	{
		collisions.Remove(collision.collider);
		if (collisions.Count == 0) 
			grounded = false;
	}

	public void PlayAnimation(string trigger)
	{
		if (!string.IsNullOrEmpty(trigger))
			animator.SetTrigger(trigger);
	}

	public void TurnToFace(Vector3 position)
	{
		Vector3 direction = (position - transform.position).normalized;
		Quaternion targetRotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, direction, Vector3.up), Vector3.up);
		transform.rotation = targetRotation;
	}

	public IEnumerable<Node> GetPath() => agentPath;
	public void ClearPath() => agentPath?.Clear();
	public bool HasPath() => agentPath != null && agentPath.Count > 0;
	Node GetNext() => HasPath() ? agentPath[0] : null;

	Node Pop()
	{
		if (!HasPath())
			return null;
		Node next = agentPath[0];
		agentPath.RemoveAt(0);
		return next;
	}
	 
    public void SetPath(IEnumerable<Node> path)
    {
		if (path == null)
			agentPath?.Clear();
		else
			agentPath = new List<Node>(path);
    }

	public void PathTo(Vector3 goal)
	{
		ClearPath();

		if (map == null)
			map = FindObjectOfType<Map>();

		if (map != null)
			SetPath(map.AStar(Node, goal));
	}

	public void PathTo(Node goal)
	{
		ClearPath();

		if (map == null)
			map = FindObjectOfType<Map>();

		if (map != null)
			SetPath(map.AStar(Node, goal));
	}

	public void PathTo(string waypoint)
	{
		ClearPath();

		GameObject obj = GameObject.Find(waypoint);
		if (obj == null)
			return;

		if (!obj.TryGetComponent<Waypoint>(out var wp))
			return;

		if (!wp.enabled)
			return;

		PathTo(wp.Node);
	}
}
