using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : Entity
{
	
	public Vector2 MouseMoveInput;

	public float JumpHeight;

	public float SprintSpeedMultiplier = 1.25f;

	public float WalkSpeedModifier = 0.5f;

	public Transform camTransform;

	private PlayerControls _controls;

	private Animator _animator;

	private bool fasterFall = false;

	private Vector2 _input;

	private bool _isSprinting = false;

	private float _currentSprintSpeed = 1;

	private float _currentWalkSpeed = 1;

	// Start is called before the first frame update
	void Start()
	{
		_controls = GlobalPlayerInput.PlayerControls;
		_animator = GetComponent<Animator>();
		IsAffectedByGravity = true;

		// Player movement
		_controls.MoveControls.MoveKeys.performed += ctx =>
		{
			_input = ctx.ReadValue<Vector2>();

			_animator.SetFloat("Move Speed", _input.magnitude);

			_currentWalkSpeed = _input.magnitude < 0.5f && !_isSprinting ? WalkSpeedModifier : 1;
            MoveVector = new Vector3(_input.x, MoveVector.y, _input.y);
		};
		/*_controls.MoveControls.MoveKeys.canceled += ctx =>
		{
			_input = ctx.ReadValue<Vector2>();
			MoveVector = new Vector3(_input.x, MoveVector.y, _input.y);
			if (IsGrounded())
			{
				momentumBeforeJump = new Vector2(MoveVector.x, MoveVector.z);
			}
		};*/

		// Player Jumping
		_controls.MoveControls.Jump.started += ctx =>
		{
			if (!IsGrounded()) return;
			yVelocity += Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y);
		};
		_controls.MoveControls.Jump.canceled += ctx =>
		{
			if (!IsGrounded())
			{
				fasterFall = true;
			}
		};

		// Player Sprinting
		_controls.MoveControls.Sprint.started += ctx =>
		{
			SetSprint(true);
		};
		_controls.MoveControls.Sprint.canceled += ctx =>
		{
			SetSprint(false);
		};

		// Player Basic Attack
		_controls.MoveControls.BasicAttack.started += ctx =>
		{
			
		};
	}

	// Update is called once per frame
	void Update()
	{
		ApplyGravity();
		Move();
		Look();
	}

	private void SetSprint(bool sprintBool)
	{
		_isSprinting = sprintBool;
		_currentSprintSpeed = _isSprinting ? SprintSpeedMultiplier : 1;
		_animator.SetBool("IsSprinting", _isSprinting);
	}

	protected override void ApplyGravity()
	{
		if (!IsAffectedByGravity) return;

		if (IsGrounded() && yVelocity < 0.0f)
		{
			yVelocity = -1.0f;
			fasterFall = false;
		}
		else
		{
			yVelocity += fasterFall && yVelocity > 0.0f ? Physics.gravity.y * LowJumpMultiplier * Time.deltaTime : Physics.gravity.y * FallMultiplier * Time.deltaTime;
		}
		
		MoveVector.y = yVelocity;
	}

	protected override void Move()
	{
		Vector3 moveDir = MoveVector;
		if(_input.sqrMagnitude > 0.0f)
		{
			float targetAngle = Mathf.Atan2(MoveVector.x, MoveVector.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
			float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothDamp);
			transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);

			moveDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
			moveDir.Set(moveDir.x, yVelocity, moveDir.z);
		}

		_characterController.Move(moveDir.normalized * Time.deltaTime * MoveSpeed *_currentWalkSpeed * _currentSprintSpeed);
	}

	protected override void Look()
	{

	}

	protected override void UnsubscribeEvents()
	{
		base.UnsubscribeEvents();
	}
}
