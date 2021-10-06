using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeLauncher : MonoBehaviour{

    public GameObject grenadePrefab;
    public float PropulsionForce;

    private Transform myTransform;

    // Start is called before the first frame update
    void Start()
    {
        SetInitializeReference();
    }

    // Update is called once per frame
    public void Fire()
    {
        //if(Input.GetButtonDown("Fire1"))
        //{
            SpawnGrenade();
        //}
    }

    void SpawnGrenade()
    {
       GameObject grenade = (GameObject) Instantiate(grenadePrefab, myTransform.transform.TransformPoint(0, 0, 2f), myTransform.rotation);
       grenade.GetComponent<Rigidbody>().AddForce(myTransform.up * PropulsionForce, ForceMode.Impulse);
       Destroy(grenade, 3);
    }

    void SetInitializeReference()
    {
        myTransform = transform;
    }
}
