using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUiManager : MonoBehaviour {
    public static GameUiManager Instance;
    private CanvasGroup _niceShotCanvas;
    private TextMeshProUGUI _message;
    private CanvasGroup[] _turnCanvases;
    private GameObject _shotsButton; 

    private void Awake() {
        Instance = this;
        _niceShotCanvas = transform.GetChild(0).GetComponent<CanvasGroup>();
        _turnCanvases = transform.GetChild(4).GetChild(1).GetComponentsInChildren<CanvasGroup>();
        _shotsButton = transform.GetChild(4).GetChild(2).gameObject;
        _message = _niceShotCanvas.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void ShowShotBanner(string message) {
        _message.text = message;
        _niceShotCanvas.alpha = 1f;
        StartCoroutine(HideShotBanner());
    }

    public void ShowShotsButton(bool b) {
        _shotsButton.SetActive(b);
    }

    public void UpdateScore(List<Player> players) {
        // For clearing canvases if player count is less than 4
        ClearPlayers();
        
        for (var i = 0; i < players.Count; i++) {
            _turnCanvases[i].alpha = (players[i].IsTurn) ? 1f : 0.2f;
            _turnCanvases[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = players[i].Name;
            var horseText = "HORSE".Substring(0, players[i].Score);
            horseText += "-----".Substring(0, 5 - players[i].Score);
            _turnCanvases[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = horseText;
        }
    }

    private void ClearPlayers() {
        foreach (var tc in _turnCanvases)
            tc.alpha = 0f;
    }
    
    private IEnumerator HideShotBanner() {
        yield return new WaitForSeconds(2f);
        _niceShotCanvas.alpha = 0f;
    }
}
