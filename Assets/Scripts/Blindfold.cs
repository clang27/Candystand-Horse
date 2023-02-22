using UnityEngine;

public class Blindfold : MonoBehaviour, IShot {
    public int CurrentOccurrences { get; set; }
    private CanvasGroup _canvasGroup;
    private void Awake() {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void PutOn(bool b) {
        LeanTween.alphaCanvas(_canvasGroup, (b) ? 1f : 0f, 0.2f);
        if (b) {
            Utility.AddToNetworkTrick("blind");
            CurrentOccurrences++;
        }
    }
}
