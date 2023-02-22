using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Boombox : MonoBehaviour, IPointerClickHandler, IShot, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private float _moveAcceleration = 30f;
    [SerializeField] private AudioClip _clickSound;
    public int CurrentOccurrences { get; set; }
    private AudioSource _audioSource;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody;
    private Vector2 _startPoint, _startClickPoint;
    private bool _moving;
    private Transform _transform;
    private Animator _animator;
    private bool _cooldown;
    private BasketballSounds _basketballSounds;
    
    private void Awake() {
        _audioSource = GetComponent<AudioSource>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _basketballSounds = GetComponentInChildren<BasketballSounds>();
        _transform = transform;
    }
    
    private void FixedUpdate() {
        if (!_moving) return;
        
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Move(pos);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            _basketballSounds.PlaySound(100000f);
            _audioSource.mute = !_audioSource.mute;
            _animator.SetFloat("PlaySpeed", _audioSource.mute ? 0f : 1f);
        }
    }

    private void OnCollisionEnter2D(Collision2D col) {
        if (_cooldown) return;
        if (Utility.PlayBallSound(col.gameObject) && col.relativeVelocity.sqrMagnitude > 10f) {
            _basketballSounds.PlaySound(col.relativeVelocity.sqrMagnitude);
            _audioSource.mute = !_audioSource.mute;
            _animator.SetFloat("PlaySpeed", _audioSource.mute ? 0f : 1f);
        }
        if (!Utility.ActivateShotCollision(col.gameObject)) return;

        StartCoroutine(Cooldown());
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
    
    private IEnumerator Cooldown() {
        _cooldown = true;
        yield return new WaitForSeconds(0.25f);
        _cooldown = false;
    }
}
