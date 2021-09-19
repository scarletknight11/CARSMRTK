using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class awakeManager : MonoBehaviour {

    public GameObject toRotate;
    public GameObject player;
    public float rotateSpeed;

    private void FixedUpdate() {
        player = GameObject.FindGameObjectWithTag("Player");
        toRotate.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        player.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    } 
}
