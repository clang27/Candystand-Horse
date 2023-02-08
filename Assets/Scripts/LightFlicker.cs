using UnityEngine;
public class LightFlicker : MonoBehaviour {
    [SerializeField] private float _speed = 1f, _minLighting = 0.2f, _maxLighting = 0.5f;
    private SpriteRenderer _spriteRenderer;
    private bool _goingUp = true;

    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update() {
        var c = _spriteRenderer.color;
        _spriteRenderer.color = new Color(c.r, c.g, c.b, 
            Mathf.Clamp(
                c.a + (_speed * (_goingUp ? 1f : -1f) * Time.deltaTime),
                _minLighting, 
                _maxLighting)
            );

        if (Mathf.Approximately(_spriteRenderer.color.a, _maxLighting) && _goingUp) {
            _goingUp = false;
        } else if (Mathf.Approximately(_spriteRenderer.color.a, _minLighting) && !_goingUp) {
            _goingUp = true;
        }
    }
}
