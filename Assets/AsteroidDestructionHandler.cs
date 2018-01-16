﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidDestructionHandler : MonoBehaviour {

    // Constants

    public const int DEFAULT_HEALTH = 5;

    //[InspectorComment("asdasdsd foo bar")]
    //[InspectorNote("asdsd")]

    // Fields

    [Tooltip("a single projectile hit will count as 1 lost health point.")]
    public int Health = DEFAULT_HEALTH;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    /*void OnCollisionEnter(Collision collisionInfo)
    {
        if (collisionInfo.gameObject.tag == "Projectile")
        {
            Destroy(this);
            Destroy(collisionInfo.gameObject);
        }
    }*/

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Projectile")
        {
            Health--;

            if (Health == 0)
            {
                Destroy(gameObject);
                Destroy(other.gameObject);
            }            
        }
    }
}
