using UnityEngine;
using UnityEngine.EventSystems;

public class Boombox : MonoBehaviour, IPointerClickHandler, IShot, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private float _moveAcceleration = 30f;
    public int CurrentOccurrences { get; set; }
    private AudioSource _audioSource;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody;
    private Vector2 _startPoint, _startClickPoint;
    private bool _moving;
    private Collider2D[] _results = new Collider2D[2];
    private Transform _transform;
    private Animator _animator;

    private void Awake() {
        _audioSource = GetComponent<AudioSource>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _transform = transform;
    }
    
    private void FixedUpdate() {
        if (!_moving) return;
        
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Move(pos);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            _audioSource.mute = !_audioSource.mute;
            _animator.SetFloat("PlaySpeed", _audioSource.mute ? 0f : 1f);
        }
    }

    private void OnCollisionEnter2D(Collision2D col) {
        if (!col.gameObject.name.Contains("Ball")) return;
        if (GameManager.Instance.Mode is GameType.OnlineLobby or GameType.OnlineMatch) {
            var ball = col.gameObject.GetComponent<MPBasketball>();
            if (ball.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging)
                if (col.relativeVelocity.sqrMagnitude > 10f) {
                    _audioSource.mute = !_audioSource.mute;
                    _animator.SetFloat("PlaySpeed", _audioSource.mute ? 0f : 1f);
                }
            if (ball.TurnPhase != TurnPhase.Shooting) return;
        }
        else {
            if (GameManager.Instance.TurnPhase is not TurnPhase.Moving and not TurnPhase.Charging)
                if (col.relativeVelocity.sqrMagnitude > 10f) {
                    _audioSource.mute = !_audioSource.mute;
                    _animator.SetFloat("PlaySpeed", _audioSource.mute ? 0f : 1f);
                }
            if (GameManager.Instance.TurnPhase != TurnPhase.Shooting) return;
        }
        
        CurrentOccurrences++;
    }
    public void OnPointerDown(PointerEventData eventData) {
        if (MenuManager.InMenu || !MenuManager.Instance.BoomboxEnabled) return;

        if (TrickShotsSelector.InMenu)
            TrickShotsSelector.Instance.CloseMenu();
        
        var pos = Camera.main.ScreenToWorldPoint(eventData.pressPosition);
        
        if (eventData.button == PointerEventData.InputButton.Right && 
                    (GameManager.Instance.TurnPhase is TurnPhase.Moving or TurnPhase.Resting)) {
            StartMoving(pos);
        } 
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (MenuManager.InMenu || !MenuManager.Instance.BoomboxEnabled) return;
        if (GameManager.Instance.TurnPhase is not TurnPhase.Resting and not TurnPhase.Moving) return;
        
        Color.RGBToHSV(_spriteRenderer.color, out var h, out var s, out _);
        _spriteRenderer.color = Color.HSVToRGB(h, s, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (MenuManager.InMenu || !MenuManager.Instance.BoomboxEnabled) return;
        
        Color.RGBToHSV(_spriteRenderer.color, out var h, out var s, out _);
        _spriteRenderer.color = Color.HSVToRGB(h, s, 1f);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (MenuManager.InMenu || !MenuManager.Instance.BoomboxEnabled) return;

        if (eventData.button == PointerEventData.InputButton.Right && _moving)
            EndMoving();
    }

    private void StartMoving(Vector2 position) {
        _moving = true;
        _startClickPoint = position;
        _startPoint = _transform.position;
        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        ResetRigidbody();
    }

    private void EndMoving() {
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        ResetRigidbody();
        _moving = false;
    }
    
    private void ResetRigidbody() {
        _rigidbody.gravityScale = 0f;
        _rigidbody.velocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
    }
    
    public void ResetPosition(Vector3 pos) {
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        ResetRigidbody();
        _rigidbody.position = pos;
    }

    private void Move(Vector2 position) {
        var xDistance = _startClickPoint.x - position.x;
        var yDistance = _startClickPoint.y - position.y;
        var newLocation = _startPoint - new Vector2(xDistance, yDistance);

        _rigidbody.position = Vector2.MoveTowards(
            _rigidbody.position, 
            newLocation,
            _moveAcceleration);
    }
}
