using UnityEngine;
using UnityEngine.EventSystems;

public class BasketballFlick : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler {
    [SerializeField] private float _moveAcceleration = 2f;
    [SerializeField] private float _flickForce = 20f;
    [SerializeField] private float _maxDistance = 2f;
    
    private Transform _arrowTransform, _ballTransform;
    private SpriteRenderer _arrowSpriteRenderer, _ballSpriteRenderer;
    private Rigidbody2D _rigidbody;
    private Vector2 _startClickPoint, _startBallPoint;
    private bool _moving, _shooting;
    private Collider2D[] _results = new Collider2D[2];
    private AiController _ai;
    private Vector2 _ballAimDirection => _startBallPoint - (Vector2)_ballTransform.position;
    private void Awake() {
        _ballTransform = transform;
        _arrowTransform = _ballTransform.GetChild(0);
        _arrowSpriteRenderer = _arrowTransform.GetComponent<SpriteRenderer>();
        _ballSpriteRenderer = GetComponent<SpriteRenderer>();
        _ai = GetComponent<AiController>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() {
        if (!_moving && !_shooting) return;
        if (_ai.enabled) return;
        
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Move(pos);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (TrickShotsSelector.InMenu || MenuManager.InMenu || _ai.enabled) return;

        var pos = Camera.main.ScreenToWorldPoint(eventData.pressPosition);
        
        if (eventData.button == PointerEventData.InputButton.Right && 
                    (GameManager.Instance.TurnPhase is TurnPhase.Moving or TurnPhase.Resting)) {
            StartMoving(pos);
        } else if (eventData.button == PointerEventData.InputButton.Left && 
                    (GameManager.Instance.TurnPhase is TurnPhase.Moving or TurnPhase.Resting or TurnPhase.Responding)) {
            StartShooting(pos);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (TrickShotsSelector.InMenu || MenuManager.InMenu || _ai.enabled) return;
        if (GameManager.Instance.TurnPhase is not TurnPhase.Resting and not TurnPhase.Responding and not TurnPhase.Moving) return;
        
        Color.RGBToHSV(_ballSpriteRenderer.color, out var h, out var s, out _);
        _ballSpriteRenderer.color = Color.HSVToRGB(h, s, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (TrickShotsSelector.InMenu || MenuManager.InMenu || _ai.enabled) return;
        
        Color.RGBToHSV(_ballSpriteRenderer.color, out var h, out var s, out _);
        _ballSpriteRenderer.color = Color.HSVToRGB(h, s, 1f);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (TrickShotsSelector.InMenu || MenuManager.InMenu || _ai.enabled) return;

        if (eventData.button == PointerEventData.InputButton.Right && GameManager.Instance.TurnPhase == TurnPhase.Moving)
            EndMoving();
        else if (eventData.button == PointerEventData.InputButton.Left && GameManager.Instance.TurnPhase == TurnPhase.Charging)
            EndShooting();
    }

    public void StartMoving(Vector2 position) {
        _moving = true;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), true);
        GameManager.Instance.TurnPhase = TurnPhase.Moving;
        _startClickPoint = position;
        _startBallPoint = _ballTransform.position;
        ResetRigidbody();
        
        if (GameManager.Instance.InLobby)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
    }

    public void EndMoving() {
        _moving = false;
        ResetRigidbody();
    }
    public void StartShooting(Vector2 position) {
        _shooting = true;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), !GameManager.Instance.BoomboxEnabled);
        GameManager.Instance.TurnPhase = TurnPhase.Charging;
        Color.RGBToHSV(_ballSpriteRenderer.color, out var h, out var s, out _);
        _ballSpriteRenderer.color = Color.HSVToRGB(h, s, 1f);
        _startClickPoint = position;
        _startBallPoint = _ballTransform.position;
        ResetRigidbody();
        _arrowSpriteRenderer.enabled = true;
            
        _arrowSpriteRenderer.size = new Vector2(_ballAimDirection.magnitude + 1f, 1f);
        //_arrowTransform.localScale = new Vector3((_ballAimDirection * 0.0795f).magnitude + 0.1f, _arrowTransform.localScale.y, _arrowTransform.localScale.z);
        _arrowTransform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(_ballAimDirection.y, _ballAimDirection.x));
        
        if (GameManager.Instance.InLobby)
            MenuManager.Instance.CurrentLevel.goal.ResetGoal();
    }

    public void EndShooting() {
        _shooting = false;
        GameManager.Instance.TurnPhase = GameManager.Instance.InLobby ? TurnPhase.Resting : TurnPhase.Shooting;
        _arrowSpriteRenderer.enabled = false;
        ResetGravity();
        _rigidbody.AddForce(_ballAimDirection * (_flickForce * _rigidbody.mass), ForceMode2D.Impulse);
    }
    
    public void Move(Vector2 position) {
        var xDistance = _startClickPoint.x - position.x;
        var yDistance = _startClickPoint.y - position.y;
        var newBallLocation = _startBallPoint - new Vector2(xDistance, yDistance);
        var aimDirection = _startClickPoint - position;

        _rigidbody.position = Vector2.MoveTowards(
            _rigidbody.position, 
            newBallLocation,
            _moveAcceleration);

        if (_shooting) {
            if (Vector2.Distance(_startBallPoint, newBallLocation) > _maxDistance)
                _rigidbody.position = _startBallPoint - (aimDirection.normalized * _maxDistance);

            _arrowSpriteRenderer.size = new Vector2(_ballAimDirection.magnitude + 1f, 1f);
            //_arrowTransform.localScale = new Vector3((_ballAimDirection * 0.0795f).magnitude + 0.1f, _arrowTransform.localScale.y, _arrowTransform.localScale.z);
            _arrowTransform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(_ballAimDirection.y, _ballAimDirection.x));
        }
    }

    public void CancelActions() {
        _arrowSpriteRenderer.enabled = false;
        _shooting = false;
        _moving = false;
        ResetGravity();
    }

    public void ResetShotPosition() {
        _rigidbody.position = _startClickPoint;
        ResetRigidbody();
    }

    public void ChangeColor(Color c) {
        _ballSpriteRenderer.color = c;
    }
    
    private void ResetRigidbody() {
        _rigidbody.gravityScale = 0f;
        _rigidbody.velocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
    }

    public void ResetPosition(Vector3 pos) {
        ResetGravity();
        _rigidbody.velocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.position = pos;
    }

    public void ResetGravity() {
        _rigidbody.gravityScale = 1f;
    }
}
