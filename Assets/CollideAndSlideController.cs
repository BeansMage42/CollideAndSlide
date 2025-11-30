using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CollideAndSlideController : MonoBehaviour
{

    private PlayerMovementState currentMoveState;

    float timeStamp;
    
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private AnimationCurve deaccelerationCurve;
    [SerializeField] private float directionChangeSmoothing;
    private float currentSpeed;

    [SerializeField] private float maxSlopeAngle = 55f;
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float skinWidth = 0.015f;
    [SerializeField] private float rotSpeed;
    [SerializeField] private float minYAngle, maxYAngle;
    [SerializeField] private float maxSprintMod;
    [SerializeField] private float maxCrouchSpeed;
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float gravityScale = 1;
    
    float verticalVel;
    private float currentSprintMod = 1;
    private Vector2 look;

    [Header("Stair Climbing")]
    [SerializeField] private float maxStairheight;
    [SerializeField] private float stepSmoothing;
    [Header("Wallrunning")]
    [SerializeField] private float minHeight;
    private Vector3 wallNormal;
    private Vector3 wallRunDir;
    [SerializeField] private float wallRunMaxDist;
    [SerializeField] private float wallRunCoolDown;
    private WaitForSeconds coolDown;
    private Vector3 startPos;
    bool canWallRun = true;
    

    [Header("Components")]
    [SerializeField] private GameObject camRotPoint;
    private CapsuleCollider col;
    private Rigidbody rb;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collideLayer;
    
    private Vector3 moveAmount;
    private Vector3 p1, p2;

    private Vector3 inputDir = Vector3.zero;//direction vector from the input
    private Vector3 moveDir = Vector3.zero;//the direction the player moves in
  

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        rb.isKinematic = true;
        coolDown = new WaitForSeconds(wallRunCoolDown);
    }
    
    void Update()
    {
        SetLookRotations(look);
    }
    private void FixedUpdate()
    {

        // rb.MovePosition( transform.position + (transform.rotation * moveDir * moveSpeed * currentSprintMod));
        WallRunCheck();
        if (currentMoveState == PlayerMovementState.wallRunning)
        {
            WallRunMove();
        }
        else
        {
            Move();
        }
        if (IsGrounded() && verticalVel < 0) verticalVel = 0;

    }
    public void MoveDir(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if(context.performed) timeStamp = Time.time;
        if(input.sqrMagnitude == 0 ) timeStamp = Time.time;
        inputDir = new Vector3(input.x, 0, input.y);
        
    }
    /// <summary>
    /// Checks if you have met the conditions to start wall running
    /// </summary>
    private void WallRunCheck()
    {
        if (!IsGrounded() && currentSpeed > 0.1f && canWallRun)
        {

            Vector3 center = transform.position + transform.rotation * col.center;
            Vector3 origin = new Vector3(center.x, col.bounds.center.y, center.z);
            Vector3 upper45 = Quaternion.AngleAxis(45f, Vector3.up) * transform.forward;
            Vector3 lower45 = Quaternion.AngleAxis(-45f, Vector3.up) * transform.forward;
            RaycastHit hit;
            Debug.Log("starting wall checks");
            Debug.DrawRay(origin, transform.forward.normalized * (col.radius + 0.3f), Color.pink, 2f);
            if (Physics.Raycast(origin, transform.forward, out hit, col.radius + 0.3f, collideLayer) ||
                Physics.Raycast(origin, upper45, out hit, col.radius + 0.3f, collideLayer) ||
                Physics.Raycast(origin, lower45, out hit, col.radius + 0.3f, collideLayer))
            {
                if (currentMoveState != PlayerMovementState.wallRunning)
                {
                    Debug.Log("wallrun hit");
                    if (!Physics.Raycast(col.center, Vector3.down, minHeight + col.bounds.extents.y, collideLayer))
                    {
                        currentMoveState = PlayerMovementState.wallRunning;
                        canWallRun = false;
                        startPos = transform.position;
                        wallRunDir = transform.forward;
                        currentSpeed = maxSpeed * maxSprintMod;
                        verticalVel = 0;
                        wallNormal = hit.normal;
                    }
                }

            }

        }
    }
    /// <summary>
    /// WALL RUNNING
    /// 
    /// Calculates movement along the normal of the wall hit
    /// checks if you are going to hit another wall or if the wall you were on ended
    /// works best on flat planes, cant go around corners or curves
    /// </summary>
    private void WallRunMove()
    {
        Debug.Log("wall run");
        Vector3 dir = ProjectAndScale(new Vector3(wallRunDir.normalized.x, 0,wallRunDir.normalized.z), wallNormal);
        
       
        Vector3 center = transform.position + transform.rotation * col.center;
        Vector3 origin = new Vector3(center.x, col.bounds.center.y, center.z);
        if (Physics.Raycast(origin,-wallNormal,col.radius + 0.3f,collideLayer))
        {
            if (Vector3.Distance(startPos, transform.position) < wallRunMaxDist)
            {

                Debug.DrawRay(transform.position, dir * maxSprintMod * maxSpeed, Color.blue, 5f);
                Debug.Log(" clump " + transform.position + dir * 10);
                moveAmount = CollideAndSlide(dir *currentSpeed, transform.position, maxBounces-1,false, dir * maxSprintMod * maxSpeed);
                if(moveAmount.magnitude < 0.1f)
                {
                    Debug.Log("hit anotherwall");
                    StartCoroutine(WallRunCoolDown());
                }
                rb.MovePosition(transform.position + moveAmount);

            }
            else
            {
                Debug.Log("stop run distance");
                
                StartCoroutine(WallRunCoolDown());
            }
        }
        else
        {
            Debug.Log("run out of wall");
            
            StartCoroutine(WallRunCoolDown());

        }

    }
    private IEnumerator WallRunCoolDown()
    {
        currentMoveState = PlayerMovementState.walking;
        yield return coolDown;
        canWallRun = true;
    }

    //if there is input, lerp the move direction towards the input direction
    //if there is no input, lerp the move direction towards 0
    //this creates a smooth transition between the inputs and 
    //allows for realistic drifting after input is released
    private void CalculateMoveDir()
    {
        if (inputDir != Vector3.zero)
        {
            moveDir = Vector3.Lerp(moveDir, inputDir, Time.fixedDeltaTime * directionChangeSmoothing);
        }
        else if(moveDir != Vector3.zero)
        {
            moveDir = Vector3.Lerp(moveDir, Vector3.zero, Time.fixedDeltaTime * directionChangeSmoothing);
            if(moveDir.sqrMagnitude <= 0.1)
            {
                moveDir = Vector3.zero;
            }
        }
    }
    //calculates speed based on the acceleration and deacceleration curves
    private void CalculateSpeed()
    {
        if (inputDir != Vector3.zero)
        {
            currentSpeed += accelerationCurve.Evaluate(Time.time - timeStamp);
            if (currentSpeed > maxSpeed)
            {
                currentSpeed = maxSpeed * currentSprintMod;
            }
        }
        else
        {
            currentSpeed -= deaccelerationCurve.Evaluate(Time.time - timeStamp);
            if (currentSpeed < 0)
            {
                currentSpeed = 0;
            }
        }
    }
    private void Move()
    {
        // Horizontal movement

        CalculateMoveDir();//find the direction
        CalculateSpeed();//find the speed
        
        //multiply by rotation to ensure it is based on forward of the player
         moveAmount = transform.rotation * moveDir.normalized * currentSpeed;

       
        //adjusts vector to follow along the normal of a slope if any
        Vector3 foundNormal;
        if (OnSlope(out foundNormal))
        {
            moveAmount = ProjectAndScale(moveAmount, foundNormal);
        }
        
        //collide and slide baby
        moveAmount = CollideAndSlide(moveAmount, transform.position, 0, false, moveAmount);

        //cielings stop vertical momentum
        if (verticalVel > 0 && CielingCheck())
        {
            verticalVel = 0;
        }

        // Gravity
        verticalVel += (Physics.gravity.y * 2f * Time.fixedDeltaTime * gravityScale);
        Vector3 gravityMove = new Vector3(0, verticalVel * Time.fixedDeltaTime, 0);
        moveAmount += CollideAndSlide(gravityMove, transform.position + moveAmount, 0, true, gravityMove);

        //allows stair climbing without gravity interrupting
        //checks for slopes to prevent weird jittering when climbing
        //runs after gravity simulation because gravity was preventing the smooth motion
        
        if (moveDir.magnitude > 0) 
        {
            if (IsGrounded())
            {
                if (!OnSlope(out foundNormal) && CanClimbStep(col.bounds.min.y + 0.02f, maxStairheight)) moveAmount.y = stepSmoothing;
            }
        }

        // Apply movement

        rb.MovePosition(transform.position + moveAmount);
        
    }

    
    RaycastHit[] groundDetection = new RaycastHit[3];
    private bool IsGrounded()
    {
        Vector3 center = transform.position + transform.rotation * col.center;
        float radius = col.radius * 0.9f;
        Vector3 origin = new Vector3(center.x, col.bounds.min.y + radius + 0.01f, center.z);
        
        return (Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundDetection, 0.3f, collideLayer) > 0);
    }

    Collider[] cielingDetection = new Collider[3];
    private bool CielingCheck()
    {
        Vector3 center = transform.position + transform.rotation * col.center;
        float radius = col.radius;
        Vector3 origin = new Vector3(center.x, col.bounds.max.y-0.01f, center.z);
        //bool cieling = Physics.SphereCast(origin, radius, Vector3.up, out _, 0.3f, collideLayer);
        // Physics.over
        // Debug.Log(cieling);
        return (Physics.OverlapSphereNonAlloc(origin, radius, cielingDetection ,collideLayer) > 0);
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
            Vector3 snapToSurface = vel.normalized * (hit.distance-skinWidth);
            Vector3 leftOver = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            if (snapToSurface.magnitude <= skinWidth)
                snapToSurface = Vector3.zero;

            if (angle <= maxSlopeAngle)
            {
                if (gravityPass)
                {
                  //  Debug.Log("snap");
                    return snapToSurface;
                }

                leftOver = ProjectAndScale(leftOver, hit.normal);
            }
            else
            {
                //scale based on angle against wall
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
      //  Debug.Log("end of loop");
        return vel;
    }

    private Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
    {
        vec = Vector3.ProjectOnPlane(vec, normal);
        return vec;
    }

    private bool OnSlope(out Vector3 foundNormal)
    {
        
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, col.height + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            foundNormal = slopeHit.normal;
            return angle < maxSlopeAngle && angle != 0;
        }
        foundNormal = Vector3.zero;
        return false;
    }

    private bool CanClimbStep(float originY, float maxHeight)
    {
        Vector3 center = transform.position + transform.rotation * col.center;
        Vector3 origin = new Vector3(center.x, originY, center.z);
        Vector3 upper45 =  Quaternion.AngleAxis( 45f, Vector3.up) * transform.forward;
        Vector3 lower45 =  Quaternion.AngleAxis( -45f, Vector3.up) * transform.forward;

        if(DetectEdgeAndSurface(origin,transform.forward,maxHeight) ||  DetectEdgeAndSurface(origin,upper45, maxHeight) || DetectEdgeAndSurface(origin, lower45, maxHeight))
        {
            return true;
        }
        return false;
        
    }
    //checks if there is something blocking path, if there is check if its at the reach height
    private bool DetectEdgeAndSurface(Vector3 origin,  Vector3 direction, float height)
    {
        RaycastHit minHit;
        Debug.DrawRay(origin, direction * (0.2f + col.radius), Color.red, 0.1f);
        if (Physics.Raycast(origin, direction, out minHit, (0.2f + col.radius), collideLayer))
        {
            RaycastHit maxHit;
            Debug.DrawRay(origin + Vector3.up * height, direction * (0.3f + col.radius), Color.yellow, 0.1f);
            if (!Physics.Raycast(origin + Vector3.up * height, direction, out maxHit, (0.3f + col.radius), collideLayer))
            {
                return true;
            }
        }
        return false;
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
           // Camera.main.fieldOfView = 80;

        }
        if (context.canceled)
        {
            currentSprintMod = 1;
           // Camera.main.fieldOfView = 60;
        }

    }
    public void CrouchToggle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            currentSprintMod = maxCrouchSpeed;
            // Camera.main.fieldOfView = 80;

        }
        if (context.canceled)
        {
            currentSprintMod = 1;
            // Camera.main.fieldOfView = 60;
        }

    }
   
   
    public void Jump(InputAction.CallbackContext context)
    {
        if(context.performed && IsGrounded() && !CielingCheck())
        {
            verticalVel = jumpVelocity;
        }
        else if(context.performed && currentMoveState == PlayerMovementState.wallRunning)
        {
            StartCoroutine(WallRunCoolDown());
            Debug.Log("jump cancel");
            verticalVel = jumpVelocity;
            
        }
    }

}

public enum PlayerMovementState
{
    idle,
    walking,
    sprinting,
    crouching,
    climbing,
    vaulting,
    wallRunning
}