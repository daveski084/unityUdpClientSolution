/***********************************************************************;
* Project           : udpclient-server Assignment 1
*
* Program name      : "NetworkMan.cs"
*
* Author            : David Gasinec
* 
* Student Number    : 101187910
*
* Date created      : 20/10/07
*
* Description       : Move player cube.
*
* Last modified     : 20/10/07
*
* Revision History  :
*
*Date        Author Ref    Revision (Date in YYYYMMDD format) 
*20/10/07    David Gasinec        Edited Script 
*
|**********************************************************************/using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float playerSpeed = 10;
    NetworkMan networkMan;


    // Start is called before the first frame update
    void Start()
    {
        networkMan = GameObject.Find("NetworkMan").GetComponent<NetworkMan>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.name != networkMan.myAddress)
        {
            return;
        }
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(-Vector3.forward * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(-Vector3.left * Time.deltaTime * playerSpeed);
        }
    }
}
