using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private PlayerControls _controls;
    private Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        _controls = GlobalPlayerInput.PlayerControls;
        _animator = GetComponent<Animator>();

        // Player Basic Attack
        _controls.MoveControls.BasicAttack.started += ctx =>
        {
            // Important Note:  When attacking, probably slow down or entirely remove rotation of player
            _animator.SetTrigger("BaseAtkTrig");
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        
    }
}
