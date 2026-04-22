using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
	// BaseAtk will refer to X button presses in this game
	private PlayerControls _controls;

	private TargetLockOn _lockOn;

	private PlayerController _playerController;

	private Animator _animator;
	private Dictionary<int, string> _stateHashMap;
	private AnimatorStateInfo _stateInfo() => _animator.GetCurrentAnimatorStateInfo(0);
	private AnimatorClipInfo[] _clipInfo() => _animator.GetCurrentAnimatorClipInfo(0);

    private string GetCurrentAnimName()
    {
        foreach (string val in _stateHashMap.Values)
        {
            if (_stateInfo().IsName(val)) return val;
        }
        return string.Empty;
    }

    // Start is called before the first frame update
    void Start()
	{
		_controls = GlobalPlayerInput.PlayerControls;
		_animator = GetComponent<Animator>();
		_playerController = GetComponent<PlayerController>();
		_lockOn = GameObject.Find("State-Driven Camera").GetComponent<TargetLockOn>();

		_stateHashMap = new Dictionary<int, string>
		{
			{ Animator.StringToHash("Base Layer.Idle"), "Idle" },
			{ Animator.StringToHash("Base Layer.Movement Blend Tree"), "Movement Blend Tree"},
			{ Animator.StringToHash("Base Layer.Standard Sprint"), "Standard Sprint" },
			/*{ Animator.StringToHash("Base Layer.BaseAtk1"), "BaseAtk1" },
			{ Animator.StringToHash("Base Layer.BaseAtk2"), "BaseAtk2" },
			{ Animator.StringToHash("Base Layer.BaseAtk3"), "BaseAtk3" }*/
		};



		// Player Basic Attack
		_controls.MoveControls.BasicAttack.started += ctx =>
		{
			CheckComboTree();
		};
	}

	// Update is called once per frame
	void Update()
	{
		
	}


	private void CheckComboTree()
	{
		string currentAnimationName = GetCurrentAnimName();

		/*if (IsInBaseAtkComboTree(currentAnimationName) || currentAnimationName.Equals("Idle") || currentAnimationName.Equals("Movement Blend Tree"))
		{
			if(_lockOn.isLocking)
			{
				transform.LookAt(_lockOn.currentTarget.position);
			}
			// Important Note:  When attacking, probably slow down or entirely remove rotation of player
			// Also important, make sure to reset triggers when you implement strong attacks.  Remember to deal with case of player pressing multiple buttons during animation playing
            _animator.SetTrigger("BaseAtkTrig");
        }*/
	}

	private bool IsInBaseAtkComboTree(string currentAnimation)
	{
		if (currentAnimation.Contains("BaseAtk"))
		{
            Debug.Log(_stateInfo().normalizedTime);
			if (_stateInfo().normalizedTime >= .5f)
				Debug.Log("Should attack again");
        }

		return currentAnimation.Contains("BaseAtk") && _stateInfo().normalizedTime >= .5f;
	}

	private void OnDestroy()
	{
		
	}
}
