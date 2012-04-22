using UnityEngine;
using System.Collections;

public class Billboard : MonoBehaviour {
    public Transform target;
    void Update() {
        transform.LookAt(target);
    }
}