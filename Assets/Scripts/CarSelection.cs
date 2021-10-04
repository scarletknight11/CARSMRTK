using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSelection : MonoBehaviour
{
    public GameObject car1;
    public GameObject car2;
    public GameObject car3;
    public GameObject car4;
    public GameObject car5;
    public GameObject car6;
    public GameObject button1;
    public GameObject button2;
    public GameObject button3;
    public GameObject button4;
    public GameObject button5;
    public GameObject button6;

    public void loadcar1()
    {
        car1.SetActive(true);
        car2.SetActive(false);
        car3.SetActive(false);
        car4.SetActive(false);
        car5.SetActive(false);
        car6.SetActive(false);
        button1.SetActive(true);
        button2.SetActive(false);
        button3.SetActive(false);
        button4.SetActive(false);
        button5.SetActive(false);
        button6.SetActive(false);

    }

    public void loadcar2()
    {
        car1.SetActive(false);
        car2.SetActive(true);
        car3.SetActive(false);
        car4.SetActive(false);
        car5.SetActive(false);
        car6.SetActive(false);
        button1.SetActive(false);
        button2.SetActive(true);
        button3.SetActive(false);
        button4.SetActive(false);
        button5.SetActive(false);
        button6.SetActive(false);
    }

    public void loadcar3()
    {

    }

    public void loadcar4()
    {

    }

    public void loadcar5()
    {

    }

    public void loadcar6()
    {

    }
}
