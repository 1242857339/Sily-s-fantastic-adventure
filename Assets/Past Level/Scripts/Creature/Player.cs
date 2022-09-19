using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
#pragma warning disable CS0108

public class Player : MonoBehaviour
{
    int jumpCnt = 0;
    bool isJump = false;
    bool isCrouching = false;
    float h, v;
    float speed = 6;
    float jumpHeight = 60.0f;
    static public int lv;
    static public int jumpTime = 1;
    Vector2 footPos;
    Vector2 collider_offset;
    Vector2 collider_size;
    static public Vector2 move;
    static public bool isUp = false;
    static public bool isGround = false;
    static public bool isDead = false;
    static public bool onLadder = false;
    static public bool inLadder = false;
    static public bool isClimb = false;
    Animator animator;
    AudioSource audio;
    Rigidbody2D rigidbody2D;
    CapsuleCollider2D collider;
    Controls controls;
    public AudioClip dead;
    public AudioClip enemy_dead;
    public AudioClip cherry;
    public AudioClip gem;
    public AudioClip jump;
    

    IEnumerator Dead()
    {
        audio.clip = dead; audio.Play();
        Destroy(collider); rigidbody2D.velocity = new Vector2(0, 10);
        isDead = true; animator.SetBool("IsDead", true);
        yield return new WaitForSecondsRealtime(3f);
        isDead = false;lv = Save.lv; jumpTime = Save.jumpTime; SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator Mushroom()
    {
        isDead = true;
        for (float i = 1; i <= 15; i += 0.1f) 
        { 
            if(transform.localScale.x>0) transform.localScale = new Vector3(i, i, i);
            else transform.localScale = new Vector3(-i, i, i);
            yield return new WaitForSecondsRealtime(0.015f); 
        }
        animator.SetBool("IsDead", true); yield return new WaitForSecondsRealtime(1.5f);
        isDead = false; lv = Save.lv; jumpTime = Save.jumpTime; SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        audio = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        collider = GetComponent<CapsuleCollider2D>();
        controls = new Controls();
        controls.GamePlay.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.GamePlay.Move.canceled += ctx => move = Vector2.zero;
        controls.GamePlay.Jump.started += ctx => isJump = true;
        controls.GamePlay.Jump.canceled += ctx => isJump = false;
        controls.GamePlay.Rouch.started += ctx => isCrouching = true;
        controls.GamePlay.Rouch.canceled += ctx => isCrouching = false;
        controls.GamePlay.Enable();
    }

    void Update()
    {
        if (isDead) return;
        //Movment
        h = move.x; v = rigidbody2D.velocity.y;
        transform.position += new Vector3(h * (speed+0.1f*lv) * Time.deltaTime, 0, 0);
        if (h < 0) transform.localScale = new Vector3(-1, 1, 1);else if (h > 0) transform.localScale = new Vector3(1, 1, 1);
        //Jump&Fall
        if (isGround || onLadder) jumpCnt = 0;
        if (isJump && jumpCnt < jumpTime) 
        {
            audio.clip = jump; audio.Play();
            jumpCnt++;isJump = isGround = false;
            if (rigidbody2D.velocity.y < 0) rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, rigidbody2D.velocity.y / 2);
            rigidbody2D.AddForce(new Vector2(0, jumpHeight)); 
        }
        if (h == 0) animator.SetBool("IsRunning", false); else animator.SetBool("IsRunning", true);
        if (v > 0&&!isClimb) { animator.SetBool("IsJumping", true); animator.SetBool("IsFalling", false); animator.SetBool("IsGround", false); }
        else if (v < 0 && !isClimb) { animator.SetBool("IsJumping", false); animator.SetBool("IsFalling", true); animator.SetBool("IsGround", false); }
        if (isUp) animator.SetBool("IsUp", true); else animator.SetBool("IsUp", false);
        if (isGround||onLadder) { animator.SetBool("IsJumping", false); animator.SetBool("IsFalling", false);animator.SetBool("IsGround", true); }
        //Climb
        footPos = new Vector2(collider.bounds.center.x, collider.bounds.min.y);
        if (Physics2D.Raycast(footPos, Vector2.up, 0.3f, 1 << 3).collider != null) inLadder = true;
        else inLadder = false;
        if(isClimb) transform.position += new Vector3(0, move.y * (speed+0.1f*lv) * Time.deltaTime, 0);
        if ((onLadder && move.y < 0 && !isClimb) || (inLadder && move.y != 0 && !isClimb))
        {
            animator.SetBool("IsClimbing", true);isClimb = true;
            GameObject.FindGameObjectWithTag("Ladder").GetComponent<Collider2D>().isTrigger = true;
            rigidbody2D.velocity = new Vector2(0, 0);rigidbody2D.gravityScale = 0;
            if (onLadder) { onLadder = false;inLadder = true; transform.position -= new Vector3(0, 0.1f, 0); }
        }
        else if((onLadder&&isClimb)||(!inLadder&&isClimb))
        {
            animator.SetBool("IsClimbing", false);isClimb = false;
            GameObject.FindGameObjectWithTag("Ladder").GetComponent<Collider2D>().isTrigger = false;
            rigidbody2D.velocity = new Vector2(0, 0); rigidbody2D.gravityScale = 3;
        }
        //Croch
        if (move.y < 0 && isGround && !isDead && !onLadder && !inLadder) 
        { 
            speed = 2; animator.SetBool("IsCrouching", true);
            collider.offset = new Vector2(0.1049123f, -0.5136018f);
            collider.size = new Vector2(0.9388857f, 0.9388857f);
        }
        else if(isGround && !isDead && !isUp && !onLadder && !inLadder)
        { 
            speed = 6; animator.SetBool("IsCrouching", false);
            collider.offset = new Vector2(0.01889324f, -0.318954f);
            collider.size = new Vector2(0.901413f, 1.319401f);
        }
        //Dead
        if (!isDead && transform.position.y < -10) { isDead = true; StartCoroutine("Dead"); }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Cherry"))
        {
            audio.clip = cherry; audio.Play();
            jumpTime++; Destroy(other.gameObject);
        }
        else if(other.gameObject.CompareTag("Gem"))
        {
            audio.clip = gem; audio.Play();
            if (jumpTime < ++Save.jumpTime) jumpTime = Save.jumpTime;
            Destroy(other.gameObject);
        }
        else if (other.gameObject.CompareTag("Trap")) 
        {
            if (!isDead) StartCoroutine("Dead"); 
        }
        else if (other.gameObject.CompareTag("Mushroom"))
        {
            if (!isDead) StartCoroutine("Mushroom");
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Enemy")&& !other.gameObject.GetComponent<Animator>().GetBool("IsDead")) 
        {
            if (!isDead && other.GetContact(0).normal.y < 0.9f) { isDead = true; StartCoroutine("Dead"); }
            else
            {
                audio.clip = enemy_dead; audio.Play();
                other.gameObject.GetComponent<Animator>().SetBool("IsDead", true);lv++;
                jumpCnt = 0; isGround = onLadder = false; rigidbody2D.velocity = new Vector2(0, 5);
            }
        }
        else if(other.gameObject.CompareTag("Trap")) 
        {
            if (!isDead) StartCoroutine("Dead"); 
        }
        else if (other.gameObject.CompareTag("Mushroom"))
        {
            audio.clip = gem; audio.Play(); Destroy(other.gameObject);
            if (!isDead) StartCoroutine("Mushroom");
        }
    }
}
