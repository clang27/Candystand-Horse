using UnityEngine;

public class OOB : MonoBehaviour  {
    private BasketballSounds _basketballSounds;
    
    private void Awake() {
        _basketballSounds = GetComponent<BasketballSounds>();
    }
    
    private void OnTriggerEnter2D(Collider2D col) {
        if (!Utility.ObjectIsBall(col.gameObject)) return;
        
        _basketballSounds.PlaySound(100f);
        GameManager.Instance.OutOfBounds(col.gameObject);
    }
}
