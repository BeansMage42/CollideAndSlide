using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;

public class CollideAndSlideController : MonoBehaviour
{
   
    
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float maxSlopeAngle = 55f;
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float skinWidth = 0.015f;
    [SerializeField] private float rotSpeed;
    [SerializeField] private float minYAngle, maxYAngle;
    [SerializeField] private float maxSprintMod;
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float gravityScale = 1;
    float verticalVel;
    private float currentSprintMod = 1;
    private Vector2 look;
    //COMPONENTS
   
    
    [Header("Components")]
    [SerializeField] private GameObject camRotPoint;
    private CapsuleCollider col;
    private Rigidbody rb;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collideLayer;
    private Vector3 moveDir = Vector3.zero;
    private Vector3 moveAmount;
    private Vector3 p1, p2;

  

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        rb.isKinematic = true;
    }
    
    void Update()
    {
        SetLookRotations(look);
    }
    private void FixedUpdate()
    {

        // rb.MovePosition( transform.position + (transform.rotation * moveDir * moveSpeed * currentSprintMod));
       
        Move(moveDir.normalized);
        if (IsGrounded() && verticalVel < 0) verticalVel = 0;

    }
    public void MoveDir(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>().normalized;
        moveDir = new Vector3(input.x, 0, input.y);
    }

    private void Move(Vector3 dir)
    {
        // Horizontal movement
        //multiply by rotation to ensure it is based on forward of the player
        moveAmount = transform.rotation * dir * speed * currentSprintMod;
        moveAmount = CollideAndSlide(moveAmount, transform.position, 0, false, moveAmount);

        // Gravity
        verticalVel += (Physics.gravity.y * Time.fixedDeltaTime * gravityScale);
        
        

        Vector3 gravityMove = new Vector3(0, verticalVel, 0);
        moveAmount += CollideAndSlide(gravityMove, transform.position + moveAmount, 0, true, gravityMove);
       



        // Apply movement
        // Debug.Log(moveAmount);
        rb.MovePosition(transform.position + moveAmount);
        
    }

    private bool IsGrounded()
    {
        Vector3 center = transform.position + transform.rotation * col.center;
        float radius = col.radius * 0.9f;
        Vector3 origin = new Vector3(center.x, col.bounds.min.y + radius + 0.01f, center.z);
        return Physics.SphereCast(origin, radius, Vector3.down, out _, 0.3f, collideLayer);
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 startPos, int depth, bool gravityPass, Vector3 velInit)
    {
       // Debug.Log("collide and slide");
        Vector3 center = startPos;
        float height = Mathf.Max(col.height, col.radius * 2);
        float halfHeight = (height / 2f) - col.radius;
        Vector3 up = transform.up;

        p1 = center + up * halfHeight;
        p2 = center - up * halfHeight;

        if (depth >= maxBounces)
            return Vector3.zero;

        float dist = vel.magnitude + skinWidth;
        if (Physics.CapsuleCast(p1, p2, col.radius, vel.normalized, out RaycastHit hit, dist, collideLayer))
        {
            Debug.Log("hit " + hit.collider.name);
            //Vector3 snapToSurface = vel.normalized * (hit.distance + skinWidth);
            Vector3 snapToSurface = vel.normalized * (hit.distance-skinWidth)/*Mathf.Max(hit.distance - skinWidth, 0)*/;
            Vector3 leftOver = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            if (snapToSurface.magnitude <= skinWidth)
                snapToSurface = Vector3.zero;

            if (angle <= maxSlopeAngle)
            {
                if (gravityPass)
                {
                    Debug.Log("snap");
                    return snapToSurface;
                }

                leftOver = ProjectAndScale(leftOver, hit.normal);
            }
            else
            {
                float scale = 1-Vector3.Dot(new Vector3(hit.normal.x,0,hit.normal.z).normalized, - new Vector3(velInit.x,0,velInit.z).normalized);
                // Stop against vertical walls
                if (IsGrounded() && !gravityPass)
                {
                    leftOver = ProjectAndScale(new Vector3(leftOver.x,0,leftOver.z),new Vector3(hit.normal.x,0,hit.normal.z).normalized) ;
                    leftOver *= scale ;
                }
                else
                {
                    leftOver = ProjectAndScale(leftOver, hit.normal)*scale;
                }
            }

            return snapToSurface + CollideAndSlide(leftOver, startPos + snapToSurface, depth + 1, gravityPass, velInit);
        }
        Debug.Log("end of loop");
        return vel;
    }

    private Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
    {
        vec = Vector3.ProjectOnPlane(vec, normal);
        return vec;
    }

    /*
     * The look function takes the players mouse delta and translates it to camera rotation
     * 
     * this function is ignored if the player is interacting with an NPC or is in a dialogue event, preventing them from moving the camera around while reading or pressing buttons
     * 
     * The camera is a child of the camRotPoint object, and that object is a child of the players global transform object. This allows the up/down movements to be independent of the body movements
     * this is useful for implimenting animations and proper models as it would allow the player to look up without their whole body tilting upwards
     * 
     */
    public void Look(InputAction.CallbackContext context)
    {
        
         look = context.ReadValue<Vector2>() * rotSpeed;
    }


    private void SetLookRotations(Vector2 mouseDir)
    {
        Quaternion yMove = Quaternion.Euler(-mouseDir.y, 0, 0);// the y has to be inverted because up is actually negative on the x axis, which is the axis of rotation but mouse delta treats up as positive and down as negative as it is tracking position on the cartesean plane
        Quaternion zMove = Quaternion.Euler(0, mouseDir.x, 0);

        camRotPoint.transform.rotation = camRotPoint.transform.rotation * yMove;


        //this section performs the clamping
        float Angle = camRotPoint.transform.eulerAngles.x;
        Angle = LockRotation(Angle, minYAngle, maxYAngle);
        //factors in parents rotation to convert the rotation from local to global space to prevent errors
        camRotPoint.transform.rotation = transform.rotation * Quaternion.Euler(Angle, 0, 0);

        rb.rotation = rb.rotation * zMove;//rotates the full body
    }
    /*
     * Angles are weird and cant be clamped using the usual methods
     * 
     * first this function has to scale all the angles so that they are all within the same number of rotations otherwise the clamping wont work
     * then it clamps it with the newly scaled angles
     * 
     */
    private float LockRotation(float Angle, float min, float max)
    {
        if (Angle < 90 || Angle > 270)
        {
            if (Angle > 180) Angle -= 360;
            if (max > 180) max -= 360;
            if (min > 180) min -= 360;
        }
        Angle = Mathf.Clamp(Angle, min, max);
        if (Angle < 0) Angle += 360;
        return Angle;
    }
    
    public void SprintToggle(InputAction.CallbackContext context)
    {
        if (context.started)
        { 
            currentSprintMod = maxSprintMod;
            Camera.main.fieldOfView = 80;

        }
        if (context.canceled)
        {
            currentSprintMod = 1;
            Camera.main.fieldOfView = 60;
        }

    }
   
   
    public void Jump(InputAction.CallbackContext context)
    {
        if(context.performed && IsGrounded())
        {
            verticalVel = jumpVelocity;
        }
    }

}
