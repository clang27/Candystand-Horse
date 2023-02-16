using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUiManager : MonoBehaviour {
    [SerializeField] private LeanTweenType _tweenType;
    [SerializeField] private float _tweenTime;
    [Header("Words")] 
    [SerializeField] private string _twoPlayerWord = "HORSE";
    [SerializeField] private string _threePlayerWord = "DUCK";
    [SerializeField] private string _fourPlayerWord = "PIG";
    
    public static GameUiManager Instance;
    private RectTransform _niceShotRectTransform;
    private CanvasGroup _niceShotCanvas;
    private TextMeshProUGUI _message;
    private CanvasGroup[] _turnCanvases;
    private Vector2 _startSize;
    private Coroutine _hideNiceShotRoutine;

    private void Awake() {
        Instance = this;
        _niceShotCanvas = transform.GetChild(0).GetComponent<CanvasGroup>();
        _niceShotRectTransform = transform.GetChild(0).GetComponent<RectTransform>();
        _turnCanvases = transform.GetChild(3).GetChild(1).GetComponentsInChildren<CanvasGroup>();
        _message = _niceShotRectTransform.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start() {
        _startSize = _niceShotRectTransform.sizeDelta;
        _niceShotRectTransform.sizeDelta = Vector2.zero;
    }

    public void ShowBanner(string message, float time) {
        // Already showing, so hide and restart banner timer
        if (_niceShotCanvas.alpha > 0.9f) {
            _niceShotRectTransform.sizeDelta = Vector2.zero;
            _niceShotCanvas.alpha = 0f;
            LeanTween.cancel(_niceShotRectTransform);
            StopCoroutine(_hideNiceShotRoutine);
        }
        
        _message.text = message;
        _niceShotCanvas.alpha = 1f;
        LeanTween.size(_niceShotRectTransform, _startSize, _tweenTime).setEase(_tweenType);
        _hideNiceShotRoutine = StartCoroutine(HideShotBanner(time));
    }

    public void ShowLoading(bool b) {
        if (!b) {
            _niceShotRectTransform.sizeDelta = Vector2.zero;
            _niceShotCanvas.alpha = 0f;
            return;
        }

        // Already showing, so hide and restart banner timer
        if (_niceShotCanvas.alpha > 0.9f) {
            _niceShotRectTransform.sizeDelta = Vector2.zero;
            _niceShotCanvas.alpha = 0f;
            LeanTween.cancel(_niceShotRectTransform);
            StopCoroutine(_hideNiceShotRoutine);
        }
        
        _message.text = "Loading...";
        _niceShotCanvas.alpha = 1f;
        LeanTween.size(_niceShotRectTransform, _startSize, _tweenTime).setEase(_tweenType);
    }

    public void UpdateScore(List<Player> players) {
        // For clearing canvases if player count is less than 4
        ClearPlayers();

        var word = players.Count switch {
            2 => _twoPlayerWord,
            3 => _threePlayerWord,
            4 => _fourPlayerWord,
            _ => _twoPlayerWord
        };
        
        var blanks = players.Count switch {
            2 => "-----",
            3 => "----",
            4 => "---",
            _ => "-----"
        };

        for (var i = 0; i < players.Count; i++) {
            _turnCanvases[i].alpha = (players[i].IsTurn) ? 1f : 0.2f;

            var animalText = word.Substring(0, players[i].Score);
            animalText += blanks.Substring(0, word.Length - players[i].Score);

            var nameMesh = _turnCanvases[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            nameMesh.text = players[i].Name;
            nameMesh.color = players[i].Color;
                
            var animalMesh = _turnCanvases[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            animalMesh.text = animalText;
        }
    }

    private void ClearPlayers() {
        foreach (var tc in _turnCanvases)
            tc.alpha = 0f;
    }
    
    private IEnumerator HideShotBanner(float time) {
        yield return new WaitForSeconds(time);
        _niceShotRectTransform.sizeDelta = Vector2.zero;
        _niceShotCanvas.alpha = 0f;
    }
}
