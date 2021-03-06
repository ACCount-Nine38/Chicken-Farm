﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chicken : Creature
{
    // base chicken status
    public float hunger, eggCooldown;
    public int type;
    public bool isDead, canButcher, eating;

    public BoxCollider2D collider;
    public ParticleSystem bloodEffect;
    public ParticleSystem smokeEffect;
    public GameObject egg, raw, bonesLeft, bonesRight;

    // variables for this chicken if it is named
    public bool isNamed, butcherProcess;
    public Text nametag;

    private float hungerTimer, butcherTimer, eggTimer, randomHunger, eatTimer;

    //chicken movement
    public float accelerationForce;
    List<Collision2D> currentCollisions = new List<Collision2D>();

    private GameObject currentFood;
    private Vector3 currentFoodPos;

    public bool young;
    private float age;
    
    //chicken will go after object with this tag
    public string foodTag = "Egg";

    public void Awake()
    {
        object[] data = photonView.instantiationData;
        if (data != null && data[0] != null)
        {
            int startType = (int)data[0];

            if(startType == 0)
            {
                StartYoung();
            }
            else if(startType == 1)
            {
                photonView.RPC("Spawn", PhotonTargets.AllBufferedViaServer);
                StartNormal();
            }
            else
            {
                StartRandom();
            }
        }
    }

    public void StartYoung()
    {
        hunger = 50;
        young = true;
        anim.SetBool("isYoung", true);
        collider.offset = new Vector2(collider.offset.x, 0.035f);
        collider.size = new Vector2(0.15f, 0.15f);
        selectBound.offset = new Vector2(0, 0.08f);

        rb.mass = 10f;
        maxSpeed = 1.5f;

        acceleration = 2000;
        eggCooldown = 100;
        original = sr.color;
    }

    public void StartNormal()
    {
        hunger = 50;

        acceleration = 2000;
        eggCooldown = 100;
        original = sr.color;
    }

    public void StartRandom()
    {
        status = "run";
        acceleration = 2000;
        eggCooldown = 100;
        original = sr.color;
        randomHunger = Random.Range(0, 100);
        accelerationForce = rb.mass * acceleration;
        photonView.RPC("SetRandomHunger", PhotonTargets.AllBufferedViaServer);
        photonView.RPC("UpdateType", PhotonTargets.AllBufferedViaServer);
    }

    [PunRPC]
    private void SetRandomHunger()
    {
        hunger = randomHunger;
    }

    // Update is called once per frame
    void Update()
    {
        age += Time.deltaTime;
        if(age >= 5f && young)
        {
            young = false;
            anim.SetBool("isYoung", false);
            collider.offset = new Vector2(collider.offset.x, 0.06f);
            collider.size = new Vector2(0.2f, 0.1f);
            selectBound.offset = new Vector2(0, 0.12f);
            selectBound.size = new Vector2(0.2f, 0.22f);
            photonView.RPC("UpdateType", PhotonTargets.AllBufferedViaServer);
        }

        CheckHovering();

        if (!isDead)
        {
            if (PhotonNetwork.isMasterClient)
            {
                UpdateMovingAnimation();
            }

            hungerTimer += Time.deltaTime;

            if (hungerTimer > 4)
            {
                hunger--;
                hungerTimer = 0;
                if(hunger <= 0)
                {
                    photonView.RPC("Starve", PhotonTargets.AllBufferedViaServer);
                    photonView.RPC("ToBones", PhotonTargets.MasterClient, transform.position.x, transform.position.y, direction);
                    return;
                }
            }

            if(CurrentType() != -1 && type != CurrentType())
                photonView.RPC("UpdateType", PhotonTargets.MasterClient);

            if (butcherProcess)
            {
                butcherTimer += Time.deltaTime;
                if (butcherTimer >= 0.35f)
                {
                    photonView.RPC("Butcher", PhotonTargets.AllBufferedViaServer);
                    if (!young)
                    {
                        photonView.RPC("DropMeat", PhotonTargets.MasterClient, transform.position.x, transform.position.y);
                    }
                    butcherTimer = 0;
                }
            }

        }
        else
        {
            if (!bloodEffect.isPlaying && !smokeEffect.isPlaying)
            {
                Die();
            }
        }

    }

    private int CurrentType()
    {
        if(young)
        {
            return -1;
        }

        if(hunger <= 33)
        {
            return 1;
        }
        else if(hunger <= 66)
        {
            return 2;
        }
        else
        {
            return 3;
        }
    }

    [PunRPC]
    private void UpdateType()
    {
        // thin chicken
        if (hunger < 30)
        {
            type = 1;
            anim.SetInteger("type", 1);
            maxSpeed = 2.5f;
            eggTimer = 100;
            rb.mass = 35;
        }
        // normal chicken
        else if (hunger <= 70)
        {
            type = 0;
            anim.SetInteger("type", 0);
            maxSpeed = 3f;
            eggCooldown = 30;
            rb.mass = 50;
        }
        // thicc chicken
        else
        {
            type = 2;
            anim.SetInteger("type", 2);
            maxSpeed = 2f;
            eggCooldown = 20;
            rb.mass = 75;
        }
    }

    private void LayEgg()
    {
        if (young)
            return;

        if (type != 1)
        {
            eggTimer += Time.deltaTime;
            if (eggTimer > eggCooldown)
            {
                photonView.RPC("SpawnEgg", PhotonTargets.MasterClient, transform.position.x, transform.position.y);
                eggTimer = 0;
            }
        }
    }

    //moves the chicken
    private void UpdateMovingAnimation()
    {
        // animation updates
        if (Mathf.Abs(rb.velocity.magnitude) > 0.1)
        {
            anim.SetBool("isMoving", true);
            anim.SetBool("isEating", false);
        }
        else
        {
            anim.SetBool("isMoving", false);
        }
    }

    private void CheckStatus()
    {
        Vector3 forceDirection = moveDirection;

        if (status == "idle")
        {
            LayEgg();

        }
        else if (status == "move")
        {
            UpdateDirection();
            //if(rb.velocity.x == 0)
            //{
            //    moveDirection.x *= -1;
            //}
            //if (rb.velocity.y == 0)
            //{
            //    moveDirection.y *= -1;
            //}
            //rb.AddForce(forceDirection * accelerationForce * Time.deltaTime);
            rb.velocity = moveDirection * maxSpeed;
        }
        else if (status == "eat")
        {
            UpdateDirection();
            rb.velocity = moveDirection * maxSpeed;
        }
        else if (status == "run")
        {
            UpdateDirection();

            if (rb.velocity.x == 0 || rb.velocity.y == 0)
            {
                moveDirection = Random.onUnitSphere * maxSpeed;
                statusTimer = 0;
                maxTimer = 1f;
                status = "chaos";
            }

            //if (rb.velocity.x == 0)
            //{
            //    moveDirection.x *= -1;
            //}
            //if (rb.velocity.y == 0)
            //{
            //    moveDirection.y *= -1;
            //}

            //rb.AddForce(forceDirection * accelerationForce * Time.deltaTime * 1.2f);
            rb.velocity = moveDirection * maxSpeed * 1.2f;
        }
        else if (status == "chaos")
        {
            UpdateDirection();
            if (rb.velocity.x == 0 || rb.velocity.y == 0)
            {
                moveDirection = Random.onUnitSphere * maxSpeed;
                statusTimer = 0;
                maxTimer = 1f;
                status = "chaos";
            }

            rb.velocity = moveDirection * maxSpeed * 1.2f;
        }
        else if (status == "eating")
        {
            if (currentFood.gameObject == null)
            {
                photonView.RPC("RandomizeAction", PhotonTargets.MasterClient);
            }
            else
            {
                rb.velocity = Vector2.zero;
                eatTimer += Time.deltaTime;
                if (eatTimer >= 1f)
                {
                    if(young)
                    {
                        hunger += 1.25f;
                    }
                    else
                    {
                        hunger += 1f;
                    }
                    
                    if (hunger > 100)
                    {
                        hunger = 100;
                    }
                    currentFood.GetComponent<SeedScript>().photonView.RPC("Eat", PhotonTargets.AllBufferedViaServer);
                    eatTimer = 0f;
                }
            }
        }

        statusTimer += Time.deltaTime;

        if (statusTimer >= maxTimer)
        {
            photonView.RPC("RandomizeAction", PhotonTargets.MasterClient);
            //photonView.RPC("RandomizeAction", PhotonTargets.AllViaClients);
        }
    }

    private void UpdateDirection()
    {
        if (direction == 1 && rb.velocity.x < -0.2)
        {
            direction = 0;
            photonView.RPC("FlipTrue", PhotonTargets.AllBuffered);
        }
        else if (direction == 0 && rb.velocity.x > 0.2)
        {
            direction = 1;
            photonView.RPC("FlipFalse", PhotonTargets.AllBuffered);
        }
    }

    private void FixedUpdate()
    {
        if (!isDead)
        {
            CheckStatus();
        }

        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
    }

    [PunRPC]
    private void RandomizeAction()
    {
        maxTimer = Random.Range(1.0f, 7.5f);
        statusTimer = 0.0f;

        // randomizes what the chicken is going to do next
        int randomStatus = Random.Range(0, 2);

        if (randomStatus == 0)
        {
            status = "idle";
        }
        else if (randomStatus == 1)
        {
            moveDirection = Random.onUnitSphere;
            status = "move";
        }
    }

    public void DangerDetected(Vector2 dangerDir)
    {
        if(status != "chaos")
        {
            maxTimer = 1.0f;
            status = "run";
            statusTimer = 0.0f;
            moveDirection = -dangerDir;
        }
    }

    public void FoodDetected(GameObject food)
    {
        if (currentFood == null)
        {
            currentFood = food;
            currentFoodPos = food.transform.position;
        }

        if (status != "run" && status != "chaos")
        {
            if (Vector3.Distance(transform.position, currentFoodPos) > 0.5f)
            {
                status = "eat";
                maxTimer = 2f;
                statusTimer = 0.0f;
                moveDirection = (currentFoodPos - transform.position).normalized;
                eatTimer = 0f;
            }
            else
            {
                maxTimer = Random.Range(2f, 5f);
                statusTimer = 0.0f;
                status = "eating";
                anim.SetBool("isMoving", false);
                anim.SetBool("isEating", true);
            }
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            canButcher = true;
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            canButcher = false;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Add the GameObject collided with to the list.
        currentCollisions.Add(col);
    }

    void OnCollisionExit2D(Collision2D col)
    {

        // Remove the GameObject collided with from the list.
        currentCollisions.Remove(col);
    }

    [PunRPC]
    private void SpawnEgg(float x, float y)
    {
        PhotonNetwork.InstantiateSceneObject(egg.name, new Vector2(x, y + 0.001f), Quaternion.identity, 0, null);
    }

    [PunRPC]
    private void DropMeat(float x, float y)
    {
        PhotonNetwork.InstantiateSceneObject(raw.name, new Vector2(x, y), Quaternion.identity, 0, null);
    }

    [PunRPC]
    private void ToBones(float x, float y, int direction)
    {
        if(direction == 0)
        {
            PhotonNetwork.InstantiateSceneObject(bonesLeft.name, new Vector2(x, y), Quaternion.identity, 0, null);
        }
        else
        {
            PhotonNetwork.InstantiateSceneObject(bonesRight.name, new Vector2(x, y), Quaternion.identity, 0, null);
        }
    }

    [PunRPC]
    public void PreButcher()
    {
        butcherProcess = true;
    }

    [PunRPC]
    public void Spawn()
    {
        smokeEffect.gameObject.SetActive(true);
        smokeEffect.Play();
    }

    // fate
    [PunRPC]
    public void Butcher()
    {
        FindObjectOfType<AudioManager>().Play("axe hit");
        int randSound = Random.Range(0, 3);
        if (randSound == 0)
        {
            FindObjectOfType<AudioManager>().Play("butcher1");
        }
        else if (randSound == 1)
        {
            FindObjectOfType<AudioManager>().Play("butcher2");
        }
        else
        {
            FindObjectOfType<AudioManager>().Play("butcher3");
        }
        bloodEffect.gameObject.SetActive(true);
        smokeEffect.gameObject.SetActive(true);
        bloodEffect.Play();
        smokeEffect.Play();
        isDead = true;
        sr.enabled = false;
        collider.isTrigger = true;
    }

    // also fate
    [PunRPC]
    public void Starve()
    {
        smokeEffect.gameObject.SetActive(true);
        smokeEffect.Play();
        isDead = true;
        sr.enabled = false;
        collider.isTrigger = true;
    }
}
