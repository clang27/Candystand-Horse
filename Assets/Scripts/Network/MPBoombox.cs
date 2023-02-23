using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

public class MPBoombox : NetworkBehaviour , IShot, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private float _moveAcceleration = 30f;
    public int CurrentOccurrences { get; set; }
    public AudioSource AudioSource { get; private set; }
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody;
    private Vector2 _startPoint, _startClickPoint;
    private Transform _transform;
    private Animator _animator;
    private bool _cooldown;
    
    public Vector2 AimPoint;
    public bool Moving;
    private bool _startedMoving;
    private BasketballSounds _basketballSounds;
    
    [Networked] public NetworkBool Active { get; set; }
    private void Awake() {
        AudioSource = GetComponent<AudioSource>();
        _spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = _spriteRenderer.GetComponent<Animator>();
        _basketballSounds = GetComponentInChildren<BasketballSounds>();
        _transform = transform;
    }

    private void Update() {
        AimPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public override void Spawned() {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Boombox"), !Active);
        
        if (!MPSpawner.Boombox)
            MPSpawner.Boombox = this;
    }
    
    public override void FixedUpdateNetwork() {
        if (!GetInput(out NetworkInputData data)) return;

        if (data.BoomboxMoving && !_startedMoving) {
            _startedMoving = true;
            StartMoving(data.BoomboxAimPoint);
        } else if (!data.BoomboxMoving && _startedMoving) {
            _startedMoving = false;
            EndMoving();
        }
        
        if (data.BoomboxMoving)
            Move(data.BoomboxAimPoint);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        _basketballSounds.PlaySound(100000f);
        AudioSource.mute = !AudioSource.mute;
        _animator.SetFloat("PlaySpeed", AudioSource.mute ? 0f : 1f);
    }

    private void OnCollisionEnter2D(Collision2D col) {
        if (_cooldown) return;
        if (Utility.PlayBallSound(col.gameObject)) {
            _basketballSounds.PlaySound(col.relativeVelocity.sqrMagnitude);
            AudioSource.mute = !AudioSource.mute;
            _animator.SetFloat("PlaySpeed", AudioSource.mute ? 0f : 1f);
        }
        if (!Utility.ActivateShotCollision(col.gameObject)) return;

        StartCoroutine(Cooldown());
        CurrentOccurrences++;
    }
    public void OnPointerDown(PointerEventData eventData) {
        if (MenuManager.InMenu || !Active || !MPSpawner.Ball.Player.IsTurn) return;

        if (TrickShotsSelector.InMenu)
            TrickShotsSelector.Instance.CloseMenu();

        if (eventData.button == PointerEventData.InputButton.Right && 
            (MPSpawner.Ball.TurnPhase is TurnPhase.Moving or TurnPhase.Resting)) {
            Moving = true;
        } 
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (MenuManager.InMenu || !Active || !MPSpawner.Ball.Player.IsTurn) return;
        if (MPSpawner.Ball.TurnPhase is not TurnPhase.Resting and not TurnPhase.Moving) return;
        
        Color.RGBToHSV(_spriteRenderer.color, out var h, out var s, out _);
        _spriteRenderer.color = Color.HSVToRGB(h, s, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (MenuManager.InMenu || !Active || !MPSpawner.Ball.Player.IsTurn) return;
        
        Color.RGBToHSV(_spriteRenderer.color, out var h, out var s, out _);
        _spriteRenderer.color = Color.HSVToRGB(h, s, 1f);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (MenuManager.InMenu || !Active || !MPSpawner.Ball.Player.IsTurn) return;

        if (eventData.button == PointerEventData.InputButton.Right && Moving)
            Moving = false;
    }

    private void StartMoving(Vector2 position) {
        Moving = true;
        _startClickPoint = position;
        _startPoint = _transform.position;
        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        ResetRigidbody();
    }

    private void EndMoving() {
        Moving = false;
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        ResetRigidbody();
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
        yield return new WaitForSeconds(0.5f);
        _cooldown = false;
    }
}
