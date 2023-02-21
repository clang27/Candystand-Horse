using UnityEngine;

public class OOB : MonoBehaviour  {
    private void OnTriggerEnter2D(Collider2D col) {
        if (!col.gameObject.name.Contains("Ball")) return;
        
        GameManager.Instance.OutOfBounds(col.gameObject);
    }
}
