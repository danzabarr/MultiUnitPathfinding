using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BehaviourState
{
	Controlled, // controlled by the player
	Idle,       // not moving
	Roaming,    // paths to random waypoints
	Walking,    // moving to a waypoint
	Talking,    // in dialogue
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

/// <summary>
/// Agent class is responsible for controlled movement and pathing.
/// That way the player character can also be in the same states as an NPC - 
/// Idle, Talking, Walking, etc. which is useful for taking control from the player
/// at certain moments.
/// Also means that any of the NPCs can be made controllable by the player.
/// </summary>
[RequireComponent(typeof(Animator), typeof(Rigidbody))]
public class Agent : Waypoint//, IAgent<Node>
{
	// Inspector variables

	/// <summary>
	/// The state of the agent.
	/// </summary>
	[SerializeField] BehaviourState agentBehaviourState = BehaviourState.Idle;

	/// <summary>
	/// The goal the agent will path towards.
	/// </summary>
	[SerializeField] Waypoint agentGoal;
	[SerializeField] Waypoint idlePoint;

	/// <summary>
	/// The path the agent is currently following.
	/// </summary>
	[SerializeField] List<Node> agentPath = new List<Node>();

	/// <summary>
	/// The control mode of the agent, either tank or direct.
	/// Used when in the controlled state.
	/// </summary>
	[SerializeField] ControlMode controlMode = ControlMode.Direct;

	/// <summary>
	/// The speed at which the agent moves in units per second.
	/// </summary>
	[SerializeField] float movementSpeed = 2;

	/// <summary>
	/// The speed at which the agent rotates in degrees per second.
	/// </summary>
	[SerializeField] float rotationSpeed = 200;

	/// <summary>
	/// The force applied to the agent when jumping.
	/// </summary>
	[SerializeField] float jumpForce = 4;

	/// <summary>
	/// The distance from the goal at which the agent will consider itself to have arrived.
	/// </summary>
	[SerializeField] float goalTolerance = 0.1f;

	/// <summary>
	/// Set to zero for instant roaming when idle.
	/// Set to a positive value to wait before roaming.
	/// Set to a negative value to disable roaming.
	/// </summary>
	[SerializeField] float roamWhenIdle = -1;

	[Header("Animations")] // Animation triggers
	[SerializeField] string idleAnimation = "Idle";
	[SerializeField] string walkAnimation = "Walk";
	[SerializeField] string talkAnimation = "Talk";
	[SerializeField] string groundedAnimation = "Grounded";
	[SerializeField] string movespeedAnimation = "MoveSpeed";

	// Cached components
	private Animator animator;
	private Rigidbody rigidBody;

	// Internal variables
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
	float roamingCooldown;

	void Awake()
	{
		animator = GetComponent<Animator>();
		rigidBody = GetComponent<Rigidbody>();
		ResetRoamingCooldown();
	}

	public BehaviourState State
	{
		get => agentBehaviourState;
		set
		{
			if (agentBehaviourState == value)
				return;

			agentBehaviourState = value;

			// do something on state change
			switch (agentBehaviourState)
			{
				case BehaviourState.Idle:
					ResetRoamingCooldown();
					break;

				case BehaviourState.Walking:
				case BehaviourState.Roaming:
					map.UpdateWaypoint(this);
					break;
			}
		}
	}

	public override void Update()
	{
		base.Update(); // update the waypoint position

		// don't do the following in edit mode
		if (!Application.isPlaying)
			return;

		switch (agentBehaviourState)
		{
			case BehaviourState.Controlled:
				Controlled();
				break;
			case BehaviourState.Idle:
				Idle();
				break;
			case BehaviourState.Walking:
				Walking();
				break;
			case BehaviourState.Talking:
				Talking();
				break;
			case BehaviourState.Roaming:
				Roaming();
				Walking();
				break;
		}
	}

	void ResetRoamingCooldown()
	{
		roamingCooldown = roamWhenIdle >= 0 ? (roamWhenIdle * (0.5f + Random.value * 0.5f)) : float.MaxValue;
		Debug.Log($"{ name } idling for { roamingCooldown } seconds");
	}

	void Roaming()
	{
		if (agentGoal == null)
		{
			List<Agent> agents = new List<Agent>(FindObjectsOfType<Agent>());

			// Find a random waypoint
			Waypoint[] waypoints = FindObjectsOfType<Waypoint>();
			if (waypoints.Length == 0)
				return;

			bool found = false;
			int attempts = 0;
			while (!found && attempts < 10)
			{
				attempts++;

				// pick a random waypoint
				Waypoint randomWaypoint = waypoints[Random.Range(0, waypoints.Length)];

				if (randomWaypoint == null)
					continue;

				// make sure the waypoint is not this one
				if (randomWaypoint == this)
					continue;

				// make sure the waypoint is enabled
				if (!randomWaypoint.enabled)
					continue;

				// make sure the waypoint can see other nodes
				if (randomWaypoint.IsOrphaned())
					continue;

				// if the waypoint is an agent, make sure it's stationary
				if (randomWaypoint is Agent agent)
				{
					switch (agent.State)
					{
						case BehaviourState.Walking:
						case BehaviourState.Roaming:
						case BehaviourState.Controlled: // don't path to controlled agents
							continue;
					}
				}

				// if the waypoint already has an agent idling there, skip it
				if (agents.Exists(a => a.idlePoint == randomWaypoint))
					continue;

				// if the waypoint already has an agent pathing to it, skip it
				if (agents.Exists(a => a.agentGoal == randomWaypoint))
					continue;

				// if we can path to the waypoint, set it as the goal					
				if (PathTo(randomWaypoint.Node))
				{
					agentGoal = randomWaypoint;
					idlePoint = null;
					found = true;
					Debug.Log($"{name} started roaming to {agentGoal.name}");
				}
			}
		}
	}

	/// <summary>
	/// Also gets called in roaming state.
	/// </summary>
	void Walking()
	{
		if (rigidBody)
			rigidBody.isKinematic = true;

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
			PlayAnimation(walkAnimation);
			SetAnimatorMovementSpeed(movementSpeed);

			// If we're close enough to the next node, pop it off the path
			const float Epsilon = 0.01f;
			float tolerance = agentPath.Count <= 1 ? goalTolerance : Epsilon;
			if (Vector3.Distance(transform.position, next.position) < tolerance)
				Pop();

			// If we've reached the goal, clear the path.
			if (agentGoal == null || Vector3.Distance(transform.position, agentGoal.Node.position) < goalTolerance)
				ClearPath();
		}
		else
		{
			// If we don't have a path, stop moving
			PlayAnimation(idleAnimation);
			SetAnimatorMovementSpeed(0);

			// If we still have a target and no path, try to find a path to it
			if (agentGoal != null && Vector2.Distance(transform.position.XZ(), agentGoal.transform.position.XZ()) > goalTolerance)
				PathTo(agentGoal.Node);

			// Otherwise if in roaming state go back to idle
			else if (agentBehaviourState == BehaviourState.Roaming)
			{
				// if the goal was an agent, turn to face it
				if (agentGoal is Agent agent)
					TurnToFace(agent.transform.position);
					
				// otherwise align rotation with the waypoint orientation
				else
					transform.forward = agentGoal.transform.forward.XZ().X0Y();

				State = BehaviourState.Idle;
				idlePoint = agentGoal;
				agentGoal = null;
			}
		}
	}

	void Idle()
	{
		roamingCooldown -= Time.deltaTime;
		if (roamingCooldown <= 0)
			State = BehaviourState.Roaming;

		agentPath.Clear();
		PlayAnimation(idleAnimation);
		SetAnimatorMovementSpeed(0);
	}

	void Talking()
	{
		PlayAnimation(talkAnimation);
		SetAnimatorMovementSpeed(0);
	}

	void ResetStuckCharacter()
	{
		// If the player gets stuck somewhere, jump to the last safe position.
		if (jumpingToSafety)
		{
			SetAnimatorMovementSpeed(0);
			float t = (Time.time - jumpStartTime) / 0.5f;
			if (t <= 1)
			{
				// interpolate between unsafe position and last safe position
				transform.position = Vector3.Lerp(unsafePosition, lastSafePosition, t);
				// add a parabolic arc jump effect
				transform.position += 0.5f * Mathf.Sin(t * Mathf.PI) * Vector3.up;
				return;
			}
			else
			{
				jumpingToSafety = false;
				transform.position = lastSafePosition;
				rigidBody.velocity = Vector3.zero;
			}
		}

		// Update the last safe position
		if (grounded && transform.position.y > 0)
		{
			Vector2Int tile = transform.position.ToTileCoord();
			if (map.IsAccessible(tile))
				// snap to tile
				lastSafePosition = tile.X0Y() + Vector3.up * transform.position.y;
		}

		// If the player falls off the map, jump to the last safe position
		else if (transform.position.y < -1 && !jumpingToSafety)
		{
			rigidBody.velocity = Vector3.zero;
			jumpingToSafety = true;
			unsafePosition = transform.position;
			jumpStartTime = Time.time;
		}
	}

	void Controlled()
	{
		if (rigidBody)
			rigidBody.isKinematic = false;
		// Reset the character if they get stuck
		ResetStuckCharacter();
		if (jumpingToSafety)
			return;

		// Interact with SimpleDialogue components if they are in range
		if (Input.GetButtonDown("Fire1"))
		{
			if (
				Physics.SphereCast(transform.position + Vector3.up - transform.forward * 0.25f, 1f, transform.forward, out RaycastHit hit, 2) &&
				hit.collider.TryGetComponent<SimpleDialogue>(out var dialogue)
			)
				dialogue.StartDialogue(this);
		}

		// Jumping
		if (Input.GetButtonDown("Jump"))
			jumpInput = true;
	}

	void FixedUpdate()
	{
		// Disable controls when jumping
		if (jumpingToSafety)
			return;

		if (agentBehaviourState != BehaviourState.Controlled)
			return;

		if (!string.IsNullOrEmpty(groundedAnimation))
			animator.SetBool(groundedAnimation, grounded);

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

		SetAnimatorMovementSpeed(verticalInput);
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
		}

		SetAnimatorMovementSpeed(directionLength);
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

	public void SetAnimatorMovementSpeed(float speed)
	{
		if (!string.IsNullOrEmpty(movespeedAnimation))
			animator.SetFloat(movespeedAnimation, speed);
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

	public bool PathTo(Vector3 goal)
	{
		ClearPath();

		if (map == null)
			map = FindObjectOfType<Map>();

		if (map != null)
			SetPath(map.AStar(Node, goal));

		return HasPath();
	}

	public bool PathTo(Node goal)
	{
		ClearPath();

		if (map == null)
			map = FindObjectOfType<Map>();

		if (map != null)
			SetPath(map.AStar(Node, goal));

		return HasPath();
	}

	public bool PathTo(string waypoint)
	{
		ClearPath();

		GameObject obj = GameObject.Find(waypoint);
		if (obj == null)
			return false;

		if (!obj.TryGetComponent<Waypoint>(out var wp))
			return false;

		if (!wp.enabled)
			return false;

		return PathTo(wp.Node);
	}

    public override void OnDrawGizmos() {}

    public override void OnDrawGizmosSelected()
	{
		if (HasPath())
		{
			Gizmos.color = Color.green;
			for (int i = 0; i < agentPath.Count - 1; i++)
				Gizmos.DrawLine(agentPath[i].position, agentPath[i + 1].position);
		}
		else 
			base.OnDrawGizmosSelected();
	}
}
