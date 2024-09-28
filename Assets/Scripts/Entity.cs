using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

/// <summary>
/// Class that represents any game object that can be manipulated and/or damaged in the game world
/// </summary>
public class Entity : MonoBehaviour
{

	public float Health;

	public float MoveSpeed;

	[SerializeField] protected float FallMultiplier;

	[SerializeField] protected float LowJumpMultiplier = 2.5f;

	public bool IsAffectedByGravity;

	[SerializeField] protected Vector3 MoveVector;

	protected readonly float Gravity = -9.82f;

	protected bool IsGrounded() => (bool)(_characterController?.isGrounded);

	/// <summary>
	/// Used for rotation
	/// </summary>
	protected float smoothDamp = 0.05f;
	protected float _currentVelocity;


	/// <summary>
	/// Describes the velocity for entity in y direction
	/// </summary>
	protected float yVelocity;

	protected CharacterController _characterController;

	private void Awake()
	{
		_characterController = GetComponent<CharacterController>();
	}

	// Start is called before the first frame update
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{

	}

	private void OnDestroy()
	{
		UnsubscribeEvents();
	}
	
	protected virtual void ApplyGravity()
	{
	}

	protected virtual void Move()
	{
	}

	protected virtual void Look()
	{
		
	}

	protected virtual void UnsubscribeEvents()
	{
	}


}
