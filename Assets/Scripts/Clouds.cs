using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour {
    [SerializeField] private float _speed = -1f, _goalX = -64f;
    private Transform _transform;
    
    private void Awake() {
        _transform = transform;
    }
    
    private void Update() {
        _transform.Translate(_speed * Time.deltaTime, 0f, 0f);
        
        if (_transform.position.x <= _goalX)
            _transform.position = Vector3.zero;
    }
    
}
