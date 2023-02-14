using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiController : MonoBehaviour {
    [SerializeField] private float WaitTime = 2f;
    [SerializeField] private ContactFilter2D Avoid;
    [SerializeField] private float SearchRadius = 9f;
    private BasketballFlick _basketball;
    private Vector2 _goalPoint, _startMovePoint, _startShotPoint, _endShotPoint;
    private Vector2[] _movePoints;
    private int _movePointIndex;
    
    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private RaycastHit2D[] _hits;

    private bool _waiting;

    [Header("Accuracy and Height")]
    [SerializeField] [Range(0.1f, 1f)] private float _initialAccuracy = 0.6f; 
    [SerializeField] [Range(5f, 10f)] private float _initialHeight = 6f; 
    
    [Header("Off The Floor")] 
    [SerializeField] [Range(0.1f, 3f)] private float _OTFHeightMult = 2f; 
    [SerializeField] [Range(0.1f, 3f)] private float _OTFAccuracyMult = 1f; 
    
    [Header("Off The Floor x2")] 
    [SerializeField] [Range(0.1f, 12f)] private float _OTF2HeightMult = 3f; 
    [SerializeField] [Range(0.1f, 3f)] private float _OTF2AccuracyMult = 1f;
    
    [Header("Off The Wall")] 
    [SerializeField] [Range(0.1f, 3f)] private float _OTWHeightMult = 0.9f; 
    [SerializeField] [Range(0.1f, 3f)] private float _OTWAccuracyMult = 1f; 
    
    [Header("Blindfolded")] 
    [SerializeField] [Range(0.1f, 3f)] private float _BHeightMult = 1f; 
    [SerializeField] [Range(0.1f, 3f)] private float _BAccuracyMult = 0.5f; 
    
    [Header("Swish")] 
    [SerializeField] [Range(0.1f, 3f)] private float _SHeightMult = 1.2f; 
    [SerializeField] [Range(0.1f, 3f)] private float _SAccuracyMult = 1.5f; 
    
    [Header("Off The Rim")] 
    [SerializeField] [Range(0.1f, 3f)] private float _OTRHeightMult = 0.9f; 
    [SerializeField] [Range(0.1f, 3f)] private float _OTRAccuracyMult = 0.9f; 
    
    [Header("Backboard")] 
    [SerializeField] [Range(0.1f, 3f)] private float _BBHeightMult = 1.2f; 
    [SerializeField] [Range(0.1f, 3f)] private float _BBAccuracyMult = 0.9f; 
    
    [Header("Moonshot")] 
    [SerializeField] [Range(0.1f, 3f)] private float _MHeightMult = 2f; 
    [SerializeField] [Range(0.1f, 3f)] private float _MAccuracyMult = 1.05f; 
    
    private void Awake() {
        _hits = new RaycastHit2D[4];
        _basketball = GetComponent<BasketballFlick>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void OnEnable() {
        _goalPoint = MenuManager.Instance.CurrentLevel.goalPoint;
        StartCoroutine(WaitABitBeforeStarting());
    }

    private IEnumerator WaitABitBeforeStarting() {
        _waiting = true;
        yield return new WaitForSeconds(WaitTime);
        _waiting = false;
        
        if (GameManager.Instance.TurnPhase is TurnPhase.Resting or TurnPhase.Moving)
            StartAiMoving();
        else if (GameManager.Instance.TurnPhase is TurnPhase.Responding)
            StartAiShooting(GetTrickshotBytes());
    }
    
    private IEnumerator WaitABitBeforeShooting() {
        _waiting = true;
        yield return new WaitForSeconds(WaitTime/4f);
        _waiting = false;
        
        _basketball.StartShooting(_startShotPoint);
    }
    
    private void FixedUpdate() {
        if (_waiting) return;
        
        switch (GameManager.Instance.TurnPhase) {
            case TurnPhase.Moving: {
                // for (var i = 0; i < _movePoints.Length-1; i++) {
                //     Debug.DrawLine(_movePoints[i], _movePoints[i+1], Color.yellow);
                // }

                _basketball.Move(_movePoints[_movePointIndex]);
                if (ReachedPoint(_movePoints[_movePointIndex])) {
                    _movePointIndex++;
                    if (_movePointIndex == _movePoints.Length) {
                        _movePointIndex = 0;
                        _basketball.EndMoving();
                        SelectAiShot();
                        StartAiShooting(GetTrickshotBytes());
                    }
                }

                break;
            }
            case TurnPhase.Charging: {
                // Debug.DrawLine(_startShotPoint, _goalPoint, Color.red);
                // Debug.DrawLine(_startShotPoint, _endShotPoint, Color.green);
                
                Debug.Log(_startShotPoint + " to " + _endShotPoint);
                _basketball.Move(_endShotPoint);
                if (ReachedPoint(_endShotPoint)) {
                    _basketball.EndShooting();
                    enabled = false;
                }

                break;
            }
        }
    }

    private bool ReachedPoint(Vector2 point) {
        return Mathf.Abs(Vector2.Distance(point, transform.position)) < 0.01f;
    }

    private int GetTrickshotBytes() {
        var trickshots = 0b0000000;
        
        var shootOffTheWall = TrickShotsSelector.Instance.HasShot("Off The Wall");
        var shootOffTheFloor = TrickShotsSelector.Instance.HasShot("Off The Floor");
        var shootOffTheFloorTwice = TrickShotsSelector.Instance.HasShot("Off The Floor x2");
        var shootOffTheRim = TrickShotsSelector.Instance.HasShot("Off The Rim");
        var moonshot = TrickShotsSelector.Instance.HasShot("Moonshot");
        var swish = TrickShotsSelector.Instance.HasShot("Swish");
        var blindfolded = TrickShotsSelector.Instance.HasShot("Blindfolded");
        var backboard = TrickShotsSelector.Instance.HasShot("Backboard");

        if (shootOffTheWall)
            trickshots ^= 0b00000001;
        if (shootOffTheFloor)
            trickshots ^= 0b00000010;
        if (shootOffTheFloorTwice)
            trickshots ^= 0b00000100;
        if (shootOffTheRim)
            trickshots ^= 0b00001000;
        if (moonshot)
            trickshots ^= 0b00010000;
        if (swish)
            trickshots ^= 0b00100000;
        if (blindfolded)
            trickshots ^= 0b01000000;
        if (backboard)
            trickshots ^= 0b10000000;

        return trickshots;
    }

    private void StartAiShooting(int trickshots) {
        _startShotPoint = transform.position;

        GetAccuracyAndHeight(trickshots, out var accuracy, out var height);

        var point = GetNewProjectionPoint(
            height, 
            accuracy, 
            (trickshots & (1 << 1)) > 0 || (trickshots & (1 << 2)) > 0,
            (trickshots & (1 << 0)) > 0);
        
        // It will try to reach the point, but if it can't, it will try a new angle
        while (trickshots < 0b1111111 && !GoodTravelPoint(_startShotPoint, point)) {
            trickshots++;
            
            GetAccuracyAndHeight(trickshots, out accuracy, out height);

            point = GetNewProjectionPoint(
                height, 
                accuracy, 
                (trickshots & (1 << 1)) > 0 || (trickshots & (1 << 2)) > 0,
                (trickshots & (1 << 0)) > 0);
        }

        _endShotPoint = !GoodTravelPoint(_startShotPoint, point) ? _startShotPoint : point;
        StartCoroutine(WaitABitBeforeShooting());
    }
    
    private bool CanMakeShot(int trickshots) {
        _startShotPoint = _rigidbody.position;

        GetAccuracyAndHeight(trickshots, out var accuracy, out var height);

        var point = GetNewProjectionPoint(
            height, 
            accuracy, 
            (trickshots & (1 << 1)) > 0 || (trickshots & (1 << 2)) > 0,
            (trickshots & (1 << 0)) > 0);

        // It will try to reach the point, but if it can't, it will try a new angle
        while (trickshots < 0b1111111 && !GoodTravelPoint(_startShotPoint, point)) {
            trickshots++;
            
            GetAccuracyAndHeight(trickshots, out accuracy, out height);

            point = GetNewProjectionPoint(
                height, 
                accuracy, 
                (trickshots & (1 << 1)) > 0 || (trickshots & (1 << 2)) > 0,
                (trickshots & (1 << 0)) > 0);
        }

        return trickshots < 0b1111111;
    }

    private void GetAccuracyAndHeight(int trickshots, out float a, out float h) {
        var shootOffTheWall = (trickshots & (1 << 0)) > 0;
        var shootOffTheFloor = (trickshots & (1 << 1)) > 0;
        var shootOffTheFloorTwice = (trickshots & (1 << 2)) > 0;
        var shootOffTheRim = (trickshots & (1 << 3)) > 0;
        var moonshot = (trickshots & (1 << 4)) > 0;
        var swish = (trickshots & (1 << 5)) > 0;
        var blindfolded = (trickshots & (1 << 6)) > 0;
        var backboard = (trickshots & (1 << 7)) > 0;

        a = _initialAccuracy;
        h = _initialHeight;

        if (shootOffTheWall) {
            a *= _OTWAccuracyMult;
            h *= _OTWHeightMult;
        } if (shootOffTheFloor) {
            a *= _OTFAccuracyMult;
            h *= _OTFHeightMult;
        } if (shootOffTheFloorTwice) {
            a *= _OTF2AccuracyMult;
            h *= _OTF2HeightMult;
        } if (shootOffTheRim) {
            a *= _OTRAccuracyMult;
            h *= _OTRHeightMult;
        } if (moonshot) {
            a *= _MAccuracyMult;
            h *= _MHeightMult;
        } if (swish) {
            a *= _SAccuracyMult;
            h *= _SHeightMult;
        } if (blindfolded) {
            a *= _BAccuracyMult;
            h *= _BHeightMult;
        } if (backboard) {
            a *= _BBAccuracyMult;
            h *= _BBHeightMult;
        }
        
        // Aim higher behind goal
        if (_startShotPoint.x > _goalPoint.x)
            h *= 2f;
    }

    private Vector2 GetNewProjectionPoint(float height, float accuracy, bool floorAngle = false, bool wallAngle = false) {
        var endShotPoint = _goalPoint;
        if (wallAngle) {
            endShotPoint = Vector2.left * 70f;
            if (endShotPoint.x > _startShotPoint.x)
                endShotPoint *= -1f;
        }

        GetProjection(
            _startShotPoint, 
            height, 
            endShotPoint, 
            accuracy,
            out var angle, 
            out var velocity
        );
        
        if (floorAngle)
            angle = (3f * Mathf.PI / 2f) + (Mathf.PI/2f - (angle*0.97f));
        if (floorAngle && wallAngle)
            angle = (5f * Mathf.PI) / 4f;

        velocity = Mathf.Clamp(velocity, 0f, _basketball.GetMaxShotDistance() * _basketball.GetShotForce());
        
        return _startShotPoint - new Vector2(
            Mathf.Cos(angle) * (velocity / _basketball.GetShotForce()),
            Mathf.Sin(angle) * (velocity / _basketball.GetShotForce())
        );
    }

    private static void GetProjection(Vector2 startPoint, float maxHeight, Vector2 endPoint, float accuracy, out float a, out float v) {
        var gravity = -Physics2D.gravity.y;
        maxHeight = Mathf.Max(startPoint.y + 2f, maxHeight);
        
        // Starts lower because it gets pulled back
        var initialHeight = startPoint.y - 
                            Random.Range((maxHeight - startPoint.y)/(1f/accuracy), (maxHeight - startPoint.y)/(0.75f/accuracy));
        // Travels further because goal is higher than ground
        var horizontalDistance = endPoint.x - startPoint.x;
        horizontalDistance += Random.Range((endPoint.x - startPoint.x)/(4f/accuracy),(endPoint.x - startPoint.x)/(2f/accuracy)) *
                              ((endPoint.x > startPoint.x) ? 1f : -1f);
        
        var yVelocity = Mathf.Sqrt((maxHeight - initialHeight) * 2f * gravity);
        var xVelocity = (horizontalDistance * gravity) / 
                        (yVelocity + Mathf.Sqrt((yVelocity*yVelocity) + (2f * gravity * initialHeight)));
        
        v = Mathf.Sqrt((xVelocity * xVelocity) + (yVelocity * yVelocity));
        a = Mathf.Acos(xVelocity / v);
    }
    
    private bool GoodTravelPoint(Vector2 startPoint, Vector2 endPoint) {
        return Physics2D.CircleCastNonAlloc(startPoint, 0.65f, (endPoint - startPoint).normalized, 
            _hits, Mathf.Abs(Vector2.Distance(startPoint, endPoint)), Avoid.layerMask) == 0;
    }
    
    private bool GoodEndPoint(Vector2 endPoint) {
        return Physics2D.CircleCastNonAlloc(endPoint, 0.65f, Vector2.zero,
            _hits, 0f, Avoid.layerMask) == 0;
    }
    
    private void StartAiMoving() {
        _startMovePoint = _rigidbody.position;
        var finalPoint = Vector2.zero;
        do {
            finalPoint = MenuManager.Instance.CurrentLevel.location +
                         new Vector3(Random.Range(-SearchRadius, SearchRadius),
                             Random.Range(-SearchRadius, SearchRadius), 0f);
        } while (!GoodEndPoint(finalPoint));

        var pointsToVisit = new List<Vector2>();

        pointsToVisit.Add(_startMovePoint);
        pointsToVisit.Add(finalPoint);
        _movePoints = pointsToVisit.ToArray();
        
        _basketball.StartMoving(_startMovePoint);
    }

    private void SelectAiShot() {
        foreach (var shot in FindObjectsOfType<TrickShot>()) {
            if (!(Random.Range(0f, 1f) > 0.8f)) continue;
            if (shot.Name.Equals("Off The Floor x2")) continue;
            
            shot.SelectShot();
        }
        
        if (!CanMakeShot(GetTrickshotBytes()))
            TrickShotsSelector.Instance.ClearTricks();
    }
}
