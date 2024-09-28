using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    public Transform target;
    private SpriteRenderer _spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        enabled = false;
    }

    private void OnDisable()
    {
        target = null;
        _spriteRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if(_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(target != null)
        {
            transform.position = target.position;
            Vector3 dir = target.position + Camera.main.transform.forward;
            transform.LookAt(dir, Vector3.up);
        }
    }
}
