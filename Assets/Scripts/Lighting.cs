using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lighting : MonoBehaviour {
    private Transform _shadowTransform, _lightingTransform;
    private Vector3 _startScale;
    
    private void Awake() {
        _shadowTransform = transform.GetChild(1);
        _lightingTransform = transform.GetChild(2);
        _startScale = _shadowTransform.localScale;
    }
    
    private void Update() {
        var hit = Physics2D.Raycast(_shadowTransform.parent.position, Vector2.down, Mathf.Infinity, LayerMask.GetMask("Floor"));

        if (hit) {
            _shadowTransform.position = hit.point;
            _shadowTransform.localScale = (1.5f*_startScale) / ((hit.distance/8f)+1.2f);
        }
            
        _lightingTransform.rotation = Quaternion.identity;
        _shadowTransform.rotation = Quaternion.identity;
    }
}
