using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class GlobalPlayerInput : MonoBehaviour
{
	private static PlayerControls _playerControls;
	public static PlayerControls PlayerControls { 
		get {
			if(_playerControls == null)
			{
				_playerControls = new PlayerControls();
				_playerControls.Enable();
			}
			return _playerControls; 
		}
	}



}
