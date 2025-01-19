using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TargetLockOn : MonoBehaviour
{

	/// <summary>
	/// Determines if entity is locking onto a target
	/// </summary>
	public bool isLocking = false;

	public float targetDetectionRadius = 10.0f;

	/// <summary>
	/// How far player has to go while locking on until lock on camera is disabled
	/// </summary>
	public float LockOnBreakDistance = 10.0f;

	/// <summary>
	/// The leninecy the dot product of target switching system allows.
	/// Switching system will only allow targets greater than or equal to this value.
	/// </summary>
	public float dotLeninency = 0.5f;

	/// <summary>
	/// The value of which a dot product is considered equivalent with the current best target.
	/// Higher values will result in target switching considering more potential targets to switch to.
	/// </summary>
	public float dotDiffEquivalence = 0.15f;

	public bool planeTargeting;

    /// <summary>
    /// Layer mask of everything that is considered an obstacle that prevents the player from locking on
    /// </summary>
    [SerializeField] private int obstacleLayerMask = 8;

    private PlayerControls _controls;

	private CinemachineFreeLook _followCam;

	private CinemachineVirtualCamera _targetCam;

	private CinemachineStateDrivenCamera _camStateMachine;

	private Animator _camAnimator;

	private Vector2 _input;

	private Vector3 printVec;

    [SerializeField] private Transform _currentPlayerTransform;

    private TargetIndicator _targetIndicator;



	/// <summary>
	/// Represents the current target of the player 
	/// </summary>
	public Transform currentTarget;


	// Start is called before the first frame update
	void Start()
	{
		_controls = GlobalPlayerInput.PlayerControls;
		isLocking = false;

		_followCam = GameObject.Find("CM_FollowCam").GetComponent<CinemachineFreeLook>();
		_targetCam = GameObject.Find("CM_TargetCam").GetComponent<CinemachineVirtualCamera>();
		_camStateMachine = GameObject.Find("State-Driven Camera").GetComponent<CinemachineStateDrivenCamera>();
		_camAnimator = GameObject.Find("State-Driven Camera").GetComponent<Animator>();
		_targetIndicator = GameObject.Find("Target_Indicator").GetComponent<TargetIndicator>();

		_currentPlayerTransform = GameObject.FindAnyObjectByType<PlayerController>().transform;//Change this initialization later

		// Player Targetting
		_controls.MoveControls.Targetting.started += ctx =>
		{
			Target();
		};
		_controls.MoveControls.Targetting.canceled += ctx =>
		{

		};

		_controls.MoveControls.TargetSwitch.started += ctx =>
		{
			if (isLocking)
			{
				_input = ctx.ReadValue<Vector2>();
				if (_input == Vector2.zero)
				{
					return;
				}

				SwitchTarget(ctx.ReadValue<Vector2>());
			}
		};
	}

	// Update is called once per frame
	void Update()
	{
		if (isLocking)
		{
			Debug.DrawLine(currentTarget.transform.position, Camera.main.transform.position);
			Debug.DrawRay(currentTarget.transform.position, printVec);
			// Ensure that the layer for a gameobject that is targetable is set to Targetable
			Collider[] potentialTargets = Physics.OverlapSphere(_currentPlayerTransform.position, targetDetectionRadius, 64);

			foreach(Collider collider in potentialTargets)
			{
				if (collider.gameObject == currentTarget)
				{
					continue;
				}
				Debug.DrawLine(currentTarget.transform.position, collider.gameObject.transform.position, Color.red);
			}

			if (!isInRange(currentTarget) || Blocked(currentTarget))
			{
				if(!isInRange(currentTarget))
				{
					Debug.Log("Target was out of range");
				}
				if(Blocked(currentTarget))
				{
					Debug.Log("Target was blocked");
				}
				ResetTargetLockCamera();
				Debug.Log("LOS has been lost");
			}
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		if(_currentPlayerTransform != null)
		{
            Gizmos.DrawWireSphere(_currentPlayerTransform.position, targetDetectionRadius);
        }
	}

	/// <summary>
	/// Finds a target for player to lock onto
	/// </summary>
	protected void Target()
	{
		if (isLocking)
		{
			ResetTargetLockCamera();
			return;
		}

		// Ensure that the layer for a gameobject that is targetable is set to Targetable
		Collider[] potentialTargets = Physics.OverlapSphere(_currentPlayerTransform.position, targetDetectionRadius, 64);

		float maxCamPriority = Mathf.NegativeInfinity;
		Transform target = null;

		// Cycle through potential targets, and determine best one based on camera forward
		foreach (Collider collider in potentialTargets)
		{
			if(Blocked(collider.transform))
			{
				continue;
			}

			Vector3 potentialTargetVec = (collider.transform.position - Camera.main.transform.position).normalized;
			float newDot = Vector3.Dot(potentialTargetVec, Camera.main.transform.forward.normalized);

			//Debug.Log("Target:  " + collider.gameObject.name + "\nDot Prod:  " + Vector3.Dot(potentialTargetVec, Camera.main.transform.forward.normalized));
			if (newDot > 0.87f && newDot > maxCamPriority)
			{
				target = collider.transform;
				maxCamPriority = newDot;
			}
		}
		Debug.Log("Target is:  " + target?.name);

		if (target == null)
		{
			ResetTargetLockCamera();
			return;
		}
		currentTarget = target;
		_camAnimator.Play("TargetCamera");
		_targetCam.LookAt = currentTarget.transform;
		_targetIndicator.enabled = true;
		_targetIndicator.target = currentTarget.transform;
		isLocking = true;
	}

	/// <summary>
	/// Logic that handles switching between targets
	/// </summary>
	/// <param name="switchInput">2D Vector of user input</param>
	protected void SwitchTarget(Vector2 switchInput)
	{
		// Note:  All calculations only take into effect x and z coordinates, y is ignored in nearly all these calculations
		Debug.Log($"Right Stick Input:  {switchInput} Right Stick Input Normalized:  {switchInput.normalized}");
		// Ensure that the layer for a gameobject that is targetable is set to Targetable
		Collider[] potentialTargets = Physics.OverlapSphere(_currentPlayerTransform.position, targetDetectionRadius, 64);
		switchInput.Normalize();

		Transform newTarget = null;

        float bestDot = Mathf.NegativeInfinity;
        float bestDist = Mathf.NegativeInfinity;
		
		Vector2 currTarViewPos = Camera.main.WorldToViewportPoint(currentTarget.transform.position);
		Vector2 currTarPos2d = GetVector2DNoY(currentTarget.transform.position);

        // Targeting system that reads input and translates it based on xz plane
        if (planeTargeting)
		{
			Vector3 cameraForward = Camera.main.transform.forward;
			Vector3 cameraRight = Camera.main.transform.right;
			cameraForward.y = 0;
			cameraRight.y = 0;
			cameraForward.Normalize();
			cameraRight.Normalize();

			Vector3 dirFromCur = (cameraForward * switchInput.y) + (cameraRight * switchInput.x);
			dirFromCur.Normalize();

			printVec = dirFromCur;
			Debug.Log($"Camera Forward: {cameraForward} Camera Right {cameraRight}");
            Debug.Log($"Camera Mod Forward: {cameraForward * switchInput.y} Camera Mod Right: {cameraRight * switchInput.x}");
            Debug.Log($"Dir from cur is: {dirFromCur} Normalized: {dirFromCur.normalized}" );

			//Iterate through potential targets to find best one
			foreach(Collider collider in potentialTargets)
			{
				Vector2 potTarScreenPos = Camera.main.WorldToViewportPoint(collider.transform.position);

				if(collider.gameObject == currentTarget.gameObject || isOffScreen(potTarScreenPos))
				{
					continue;
				}

				Vector3 dirToPotTar = GetVectorNoY(collider.transform.position - currentTarget.transform.position);

				float newDot = Vector3.Dot(dirToPotTar.normalized, dirFromCur);
				float newDist = dirToPotTar.magnitude;
				float dotDiff = Mathf.Abs(bestDot - newDot);

				if(newDot < dotLeninency || Blocked(collider.transform) || !isInRange(collider.transform))
				{
					continue;
				}

				if(newDot >= bestDot || dotDiff <= dotDiffEquivalence)
				{
					if((dotDiff <= dotDiffEquivalence && newDist < bestDist) || (newDot >= bestDot && dotDiff > dotDiffEquivalence))
					{
						newTarget = collider.transform;
						bestDist = newDist;
						bestDot = newDot;
					}
				}
			}


			// If potential target is too far off angle, then ignore
				/*if (newDot < 0.4f || Blocked(collider.transform))
				{
					continue;
				}

				if (newDot >= bestDot)
				{
					float dotDiff = Mathf.Abs(bestDot - newDot);
					if ((dotDiff <= 0.15f && newDist < bestDist) || dotDiff > 0.15f)
					{
						newTarget = collider.transform;
						bestDist = newDist;
						bestDot = newDot;
					}
				}*/
		}
		else
		{
            //Vector2 dirFromCur = (new Vector2(currTarViewPos.x, currTarViewPos.y) + switchInput).normalized;
            Debug.Log($"{currentTarget.name}\nScreen Pos: {currTarViewPos}");

			// Cycle through potential targets
            foreach (Collider collider in potentialTargets)
			{
                Vector2 potTarScreenPos = Camera.main.WorldToViewportPoint(collider.transform.position);

                if (currentTarget == collider.transform || isOffScreen(potTarScreenPos) || !isInRange(collider.transform))
                {
                    continue;
                }

				Vector2 dirToPotTar = (potTarScreenPos - currTarViewPos);

				float newDot = Vector2.Dot(dirToPotTar.normalized, switchInput);
				float newDist = dirToPotTar.magnitude;
				float dotDiff = Mathf.Abs(bestDot - newDot);

				Debug.Log($"{collider.gameObject.name}, Screen Pos: {potTarScreenPos}, Dot: {newDot}, Dir to New{dirToPotTar}");

				if (newDot < dotLeninency || Blocked(collider.transform))
				{
					continue;
				}

                if (newDot >= bestDot || dotDiff <= dotDiffEquivalence)
                {
                    if ((dotDiff <= dotDiffEquivalence && newDist < bestDist) || (newDot >= bestDot && dotDiff > dotDiffEquivalence))
                    {
                        newTarget = collider.transform;
                        bestDist = newDist;
                        bestDot = newDot;
                    }
                }


            }
		}

        if (newTarget != null)
        {
            currentTarget = newTarget;
            _targetIndicator.target = currentTarget;
            _targetCam.LookAt = currentTarget.transform;
        }
    }

	/// <summary>
	/// Resets the current camera view to be the follow camera
	/// </summary>
	private void ResetTargetLockCamera()
	{
		// Set follow camera to orientation of target camera on reset
		if (currentTarget != null)
		{

			float topHeight = _followCam.m_Orbits[0].m_Height;
			float bottomHeight = _followCam.m_Orbits[2].m_Height;
			float yAxisValue = Mathf.InverseLerp(bottomHeight, topHeight, _targetCam.transform.localPosition.y);

			_followCam.m_XAxis.Value = _targetCam.transform.eulerAngles.y;
			_followCam.m_YAxis.Value = yAxisValue;
		}
		_camAnimator.Play("FollowCamera");
		currentTarget = null;
		isLocking = false;
		_targetCam.LookAt = null;
		_targetIndicator.enabled = false;
		return;
	}

	/// <summary>
	/// See if camera is blocked from locking on to a transform
	/// </summary>
	/// <param name="transformCheck"></param>
	/// <returns></returns>
	private bool Blocked(Transform transformCheck)
	{
		return Physics.Linecast(_currentPlayerTransform.position, transformCheck.position, obstacleLayerMask);
	}

	/// <summary>
	/// See if the current target is in range of the player
	/// </summary>
	/// <param name="transformCheck"></param>
	/// <returns></returns>
	private bool isInRange(Transform transformCheck)
	{
		return Vector3.Distance(_currentPlayerTransform.position, transformCheck.position) <= LockOnBreakDistance;
	}

	private bool isOffScreen(Vector2 vec2)
	{
		return vec2.x < 0 || vec2.x > 1.0f || vec2.y < 0 || vec2.y > 1.0f;
	}

	/// <summary>
	/// Returns a Vector3 as a Vector2 with x and z coordinates and no Y coordinate
	/// </summary>
	/// <param name="vec3">Vector to translate</param>
	/// <returns>Vector3 as Vector2(x,z)</returns>
	private Vector2 GetVector2DNoY(Vector3 vec3)
	{
		return new Vector2(vec3.x, vec3.z);
	}

    /// <summary>
    /// Returns a Vector3 as a Vector3 with y = 0
    /// </summary>
    /// <param name="vec3">Vector to translate</param>
    /// <returns>The Vector3 with y=0</returns>
    private Vector3 GetVectorNoY(Vector3 vec3)
	{
		return new Vector3(vec3.x, 0, vec3.z);
	}
}
