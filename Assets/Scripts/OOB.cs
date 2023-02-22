using UnityEngine;

public class OOB : MonoBehaviour  {
    private void OnTriggerEnter2D(Collider2D col) {
        if (!Utility.ObjectIsBall(col.gameObject)) return;
        
        GameManager.Instance.OutOfBounds(col.gameObject);
    }
}
