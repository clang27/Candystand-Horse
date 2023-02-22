using UnityEngine;

public class MPLighting : MonoBehaviour {
    private Transform _shadowTransform, _lightingTransform;
    private Vector3 _startScale;
    
    private void Awake() {
        _shadowTransform = transform.parent.GetChild(1);
        _lightingTransform = transform.GetChild(1);
        _startScale = _shadowTransform.localScale;
    }
    
    private void LateUpdate() {
        var hit = Physics2D.Raycast(_shadowTransform.parent.position, Vector2.down, 
            Mathf.Infinity, LayerMask.GetMask("Floor"));

        if (hit) {
            _shadowTransform.position = hit.point;
            _shadowTransform.localScale = (1.5f*_startScale) / ((hit.distance/8f)+1.2f);
        }
            
        _lightingTransform.rotation = Quaternion.identity;
        _shadowTransform.rotation = Quaternion.identity;
    }
}