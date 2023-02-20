using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;

public class MPBasketball : NetworkBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler {
    public static List<MPPlayer> Players => FindObjectsOfType<MPPlayer>().OrderBy(p => p.PlayerIndex).ToList();
    
    [SerializeField] private float _moveAcceleration = 2f;
    [SerializeField] private float _flickForce = 20f;
    [SerializeField] private float _maxDistance = 2f;
    [Networked] public MPPlayer Player { get; private set; }
    [Networked(OnChanged = nameof(OnGameStarted))] public NetworkBool GameStarted { get; set; }
    public bool IsMe => _mpSpawner.Ball == this;

    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private Transform _arrowTransform, _ballTransform;
    private SpriteRenderer _arrowSpriteRenderer, _ballSpriteRenderer;
    public Vector2 _startClickPoint, _startBallPoint;
    private Vector2 _ballAimDirection => _startBallPoint - _rigidbody.position;
    private Camera _camera;
    
    public Vector2 AimPoint;
    public bool Moving, Shooting;
    private bool _startedMoving, _startedShooting;
    private MPSpawner _mpSpawner;

    public void Awake() {
        _ballTransform = transform.GetChild(0);
        _arrowTransform = _ballTransform.GetChild(0);
        _arrowSpriteRenderer = _arrowTransform.GetComponent<SpriteRenderer>();
        _ballSpriteRenderer = _ballTransform.GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponentInChildren<Collider2D>();
        _camera = FindObjectOfType<Camera>();
        _mpSpawner = FindObjectOfType<MPSpawner>();
    }
    
    private void Update() {
        AimPoint = _camera.ScreenToWorldPoint(Input.mousePosition);
    }
    public override void Spawned() {
        if (!_mpSpawner.Ball)
            _mpSpawner.Ball = this;
    }

    public override void FixedUpdateNetwork() {
        if (!GetInput(out NetworkInputData data)) return;
        
        if (data.Moving && !_startedMoving) {
            _startedMoving = true;
            StartMoving(data.AimPoint);
        } else if (!data.Moving && _startedMoving) {
            _startedMoving = false;
            EndMoving();
        }
            
        if (data.Shooting && !_startedShooting) {
            _startedShooting = true;
            StartShooting(data.AimPoint);
        } else if (!data.Shooting && _startedShooting) {
            _startedShooting = false;
            EndShooting();
        }

        if (data.Moving || data.Shooting) {
            Move(data.AimPoint);
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (!IsMe || !Player.IsTurn) return;
        
        if (eventData.button == PointerEventData.InputButton.Right) {
            Moving = true;
        } else if (eventData.button == PointerEventData.InputButton.Left) {
            Shooting = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Debug.Log(Player.PlayerIndex + ": " + IsMe + " + " + Player.IsTurn);
            
        if (!IsMe || !Player.IsTurn) return;
        
        Color.RGBToHSV(_ballSpriteRenderer.color, out var h, out var s, out _);
        _ballSpriteRenderer.color = Color.HSVToRGB(h, s, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (!IsMe || !Player.IsTurn) return;
        
        Color.RGBToHSV(_ballSpriteRenderer.color, out var h, out var s, out _);
        _ballSpriteRenderer.color = Color.HSVToRGB(h, s, 1f);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (!IsMe || !Player.IsTurn) return;
        
        if (eventData.button == PointerEventData.InputButton.Right)
            Moving = false;
        else if (eventData.button == PointerEventData.InputButton.Left)
            Shooting = false;
    }

    public void StartMoving(Vector2 position) {
        _startClickPoint = position;
        _startBallPoint = _ballTransform.position;
        ResetRigidbody();
        
        if (TrickShotsSelector.InMenu)
            TrickShotsSelector.Instance.CloseMenu();
    }

    public void EndMoving() {
        ResetRigidbody();
    }
    
    public void StartShooting(Vector2 position) {
        Color.RGBToHSV(_ballSpriteRenderer.color, out var h, out var s, out _);
        _ballSpriteRenderer.color = Color.HSVToRGB(h, s, 1f);
        
        _startClickPoint = position;
        _startBallPoint = _rigidbody.position;
        ResetRigidbody();
        
        _arrowSpriteRenderer.enabled = true;
        _arrowSpriteRenderer.size = new Vector2(_ballAimDirection.magnitude + 0.5f, 1f);
        _arrowTransform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(_ballAimDirection.y, _ballAimDirection.x));
        
        if (TrickShotsSelector.InMenu)
            TrickShotsSelector.Instance.CloseMenu();
    }

    public void EndShooting() {
        GameManager.Instance.EndedShot();
        _arrowSpriteRenderer.enabled = false;
        ResetGravity();
        if (_ballAimDirection.magnitude < 1f && _collider.IsTouchingLayers(LayerMask.GetMask("Floor"))) {
            Color.RGBToHSV(_ballSpriteRenderer.color, out var h, out var s, out _);
            _ballSpriteRenderer.color = Color.HSVToRGB(h, s, 0.5f);
            GameManager.Instance.ShotMissed(gameObject);
        } else
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
            _moveAcceleration * Time.fixedDeltaTime);

        if (Shooting) {
            if (Vector2.Distance(_startBallPoint, newBallLocation) > _maxDistance)
                _rigidbody.position = Vector2.MoveTowards(_rigidbody.position,
                    _startBallPoint - (aimDirection.normalized * _maxDistance),
                    _moveAcceleration * Time.fixedDeltaTime);

            _arrowSpriteRenderer.size = new Vector2(_ballAimDirection.magnitude + 0.5f, 1f);
            _arrowTransform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(_ballAimDirection.y, _ballAimDirection.x));
        }
    }

    public void CancelActions() {
        _arrowSpriteRenderer.enabled = false;
        Shooting = false;
        Moving = false;
        ResetGravity();
    }

    public void ChangeColor(Color c) {
        _ballSpriteRenderer.color = c;
    }
    
    public void ResetRigidbody() {
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
    
    public void SwapPositionToShot(MPBasketball otherBall) {
        otherBall.ResetPosition(_startBallPoint);
        otherBall.ResetRigidbody();

        ResetPosition(Vector3.up * 1000f);
        ResetRigidbody();
    }
    
    public void SwapPositionToReset(MPBasketball otherBall) {
        otherBall.ResetPosition(MenuManager.Instance.CurrentLevel.ballRespawnPoint);

        ResetPosition(Vector3.up * 1000f);
        ResetRigidbody();
    }
    
    public static void OnGameStarted(Changed<MPBasketball> changed) {
        if (!changed.Behaviour.GameStarted) return;
        
        GameManager.Instance.GoToOnlineMatch();
    }
}
