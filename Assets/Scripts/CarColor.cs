using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarColor : MonoBehaviour {

    public MeshRenderer car;
    public Slider red;
    public Slider green;
    public Slider blue;

    // Update is called once per frame
    public void OnEdit()
    {
        Color color = car.material.color;
        color.r = red.value;
        color.g = green.value;
        color.b = blue.value;
        car.material.color = color;
        car.material.SetColor("_EmissionColor", color);
    }
}
