using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour {

    private GameObject target;
    private bool targetLocked;

    public GameObject turretTopPart;
    public GameObject bullet;
    public float fireTimer;
    private bool shotReady;

    void Start()
    {
        shotReady = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (targetLocked)
        {
            turretTopPart.transform.LookAt(target.transform);
            turretTopPart.transform.Rotate(0, -90, 0);

            if (shotReady)
            {
                Shoot();
            }
        }
    }

    public void Shoot()
    {
        Transform _bullet = Instantiate(bullet.transform, transform.position, Quaternion.identity);
        _bullet.transform.rotation = turretTopPart.transform.rotation;
    }
}
