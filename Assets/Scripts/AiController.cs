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
    
    private void Awake() {
        _basketball = GetComponent<BasketballFlick>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void OnEnable() {
        _goalPoint = MenuManager.Instance.CurrentLevel.goalPoint;
        StartCoroutine(WaitABitBeforeStarting());
    }

    private IEnumerator WaitABitBeforeStarting() {
        yield return new WaitForSeconds(WaitTime);
        
        if (GameManager.Instance.TurnPhase is TurnPhase.Resting or TurnPhase.Moving)
            StartAiMoving();
        else if (GameManager.Instance.TurnPhase is TurnPhase.Responding)
            StartAiShooting();
    }
    private void Update() {
        switch (GameManager.Instance.TurnPhase) {
            case TurnPhase.Moving: {
                for (var i = 0; i < _movePoints.Length-1; i++) {
                    Debug.DrawLine(_movePoints[i], _movePoints[i+1], Color.yellow);
                }

                _basketball.Move(_movePoints[_movePointIndex]);
                if (ReachedPoint(_movePoints[_movePointIndex])) {
                    _movePointIndex++;
                    if (_movePointIndex == _movePoints.Length) {
                        _movePointIndex = 0;
                        _basketball.EndMoving();
                        StartAiShooting();
                    }
                }

                break;
            }
            case TurnPhase.Charging: {
                Debug.DrawLine(_startShotPoint, _goalPoint, Color.red);
                Debug.DrawLine(_startShotPoint, _endShotPoint, Color.green);
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
        return Mathf.Abs(Vector2.Distance(point, _rigidbody.position)) < 0.01f;
    }

    private void StartAiShooting() {
        _startShotPoint = _rigidbody.position;

        var point = GetNewProjectionPoint(6f);
        if (!GoodPoint(point) || TrickShotsSelector.Instance.HasShot("Off The Floor"))
            point = GetNewProjectionPoint(12f, true);
        if (!GoodPoint(point) || TrickShotsSelector.Instance.HasShot("Off The Floor x2"))
            point = GetNewProjectionPoint(24f, true);
        if (!GoodPoint(point) || TrickShotsSelector.Instance.HasShot("Off The Wall"))
            point = GetNewProjectionPoint(12f, false, true);
        if (!GoodPoint(point) || (TrickShotsSelector.Instance.HasShot("Off The Wall") && TrickShotsSelector.Instance.HasShot("Off The Floor")))
            point = GetNewProjectionPoint(16f, true, true);
        if (!GoodPoint(point) || (TrickShotsSelector.Instance.HasShot("Off The Wall") && TrickShotsSelector.Instance.HasShot("Off The Floor")))
            point = GetNewProjectionPoint(16f, true, false, true);

        _endShotPoint = point;
        _basketball.StartShooting(_startShotPoint);
    }

    private Vector2 GetNewProjectionPoint(float height, bool floorAngle = false, bool wallAngle1 = false, bool wallAngle2 = false) {
        var endShotPoint = _goalPoint;
        if (wallAngle1)
            endShotPoint = Vector2.left * 50f;
        if (wallAngle2)
            endShotPoint = Vector2.right * 50f;
        
        GetProjection(
            _startShotPoint, 
            (TrickShotsSelector.Instance.HasShot("Moonshot") ? height * 2f : height), 
            endShotPoint, 
            out var angle, 
            out var velocity
        );
        
        if (floorAngle && wallAngle1)
            angle = (3f * Mathf.PI / 2f) - (angle*0.95f);
        else if (floorAngle)
            angle = (3f * Mathf.PI / 2f) + (Mathf.PI/2f - (angle*0.95f));
        else if (wallAngle1)
            angle = (Mathf.PI) - (angle*1.1f);

        velocity = Mathf.Clamp(velocity, 0f, _basketball.GetMaxShotDistance() * _basketball.GetShotForce());
        
        return _startShotPoint - new Vector2(
            Mathf.Cos(angle) * (velocity / _basketball.GetShotForce()),
            Mathf.Sin(angle) * (velocity / _basketball.GetShotForce())
        );
    }

    private static void GetProjection(Vector2 startPoint, float maxHeight, Vector2 endPoint, out float a, out float v) {
        var gravity = -Physics2D.gravity.y;
        maxHeight = Mathf.Max(startPoint.y + 1f, maxHeight);
        // Starts lower because it gets pulled back
        var initialHeight = startPoint.y - Random.Range((maxHeight - startPoint.y)/3f, (maxHeight - startPoint.y)/1.25f);
        // Travels further because goal is higher than ground
        var horizontalDistance = endPoint.x - startPoint.x + Random.Range((endPoint.x - startPoint.x)/6.5f,(endPoint.x - startPoint.x)/1.5f);
        var yVelocity = Mathf.Sqrt((maxHeight - initialHeight) * 2f * gravity);
        var xVelocity = (horizontalDistance * gravity) / 
                        (yVelocity + Mathf.Sqrt((yVelocity*yVelocity) + (2f * gravity * initialHeight)));
        
        v = Mathf.Sqrt((xVelocity * xVelocity) + (yVelocity * yVelocity));
        a = Mathf.Asin(yVelocity / v);
    }
    
    private bool GoodPoint(Vector2 point) {
        _hits = new RaycastHit2D[4];
        return Physics2D.CircleCastNonAlloc(point, 0.7f, Vector2.zero, _hits, 0f, Avoid.layerMask) == 0;
    }
    
    private void StartAiMoving() {
        _startMovePoint = _rigidbody.position;
        _hits = new RaycastHit2D[4];
        var finalPoint = Vector2.zero;
        do {
            finalPoint = MenuManager.Instance.CurrentLevel.location +
                         new Vector3(Random.Range(-SearchRadius, SearchRadius),
                             Random.Range(-SearchRadius, SearchRadius), 0f);
        } while (!GoodPoint(finalPoint));

        var pointsToVisit = new List<Vector2>();

        pointsToVisit.Add(_startMovePoint);
        pointsToVisit.Add(finalPoint);
        _movePoints = pointsToVisit.ToArray();
        
        _basketball.StartMoving(_startMovePoint);
    }
}
