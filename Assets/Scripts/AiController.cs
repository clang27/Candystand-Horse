using UnityEngine;

public class AiController : MonoBehaviour {
    public int Difficulty = 0;
    public float MovementSpeed = 10f;
    
    private BasketballFlick _basketball;
    private Vector2 _shotPoint, _startPoint, _destinationPoint, _aimPoint;
    private Rigidbody2D _rigidbody;
    private float _timer;
    
    private void Awake() {
        _basketball = GetComponent<BasketballFlick>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() {
        _timer = 0f;
        _shotPoint = MenuManager.Instance.CurrentLevel.goalPoint;
        _startPoint = _rigidbody.position;
        // Temporary
        _destinationPoint = MenuManager.Instance.CurrentLevel.ballRespawnPoint;
        _aimPoint = new Vector2((_shotPoint.x + _destinationPoint.x) / 2f, 10f);
        
        if (GameManager.Instance.TurnPhase == TurnPhase.Moving) {
            _basketball.StartMoving(_startPoint);
        } else if (GameManager.Instance.TurnPhase == TurnPhase.Responding) {
            _basketball.StartShooting(_startPoint);
        }
    }
    private void Update() {
        if (GameManager.Instance.TurnPhase == TurnPhase.Moving) {
            var point = Vector2.MoveTowards(_rigidbody.position, _destinationPoint, Time.deltaTime * MovementSpeed);
            _basketball.Move(point);
            if (Mathf.Abs(Vector2.Distance(_destinationPoint, _rigidbody.position)) < 0.1f) {
                _basketball.EndMoving();
                _basketball.StartShooting(_destinationPoint);
            }
        } else if (GameManager.Instance.TurnPhase == TurnPhase.Charging) {
            // Temporary
            _timer += Time.deltaTime;
            var point = Vector2.MoveTowards(_rigidbody.position, _destinationPoint - new Vector2(2f, 2f),
                Time.deltaTime * MovementSpeed);
            _basketball.Move(point);
            if (_timer > 0.5f) {
                _basketball.EndShooting();
                enabled = false;
            }
                
        }
    }
}
