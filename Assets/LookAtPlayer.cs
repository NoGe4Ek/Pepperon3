using UnityEngine;

public class LookAtPlayer : MonoBehaviour {
    void LateUpdate() {
        transform.rotation = Quaternion.identity;
    }
}