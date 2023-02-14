using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuItem : MonoBehaviour {
    [SerializeField] private List<GameMode> _modesAvailable;
    private CanvasGroup _canvasGroup;

    private void Awake() {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ActivateIfRightMode(GameMode mode) {
        var b = _modesAvailable.Any(m => m.Equals(mode));

        _canvasGroup.interactable = b;
        _canvasGroup.blocksRaycasts = b;
        _canvasGroup.alpha = (b) ? 1f : 0.2f;
    }
}
