using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationAndMovementController : MonoBehaviour
{
    CharacterAndAnimationControll playerInput;
    CharacterController characterController;

    Animator animator;
    

    // variables to storre player input values
    Vector2 currentMovementInput;
    Vector3 currentMovement;

    Vector3 currentRunMovement;
    bool isMovementPressed;
    bool isRunPressed;
    //constraints
    float rotationFactorPerFrame = 15.0f;
    float runMultiplier = 7.0f;
    float jogMultiplier = 5.0f;
    int zero = 0;
    //
    // gravity 
     float groundedGravity = -0.05f;
     float gravity = -9.8f;
     //Jumping variables
     bool isJumpPressed = false;
     float initialJumpVelocity;
    float maxJumpHeight = 6.0f;
    float maxJumpTime = 0.75f * 1.2f;
    bool isJumping = false;
    float fallMultiplier;
    bool isJumpAnimating = false;
    int jumpCount = 0;

    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>();
    Coroutine currentJumpResetRoutine = null;


    


    

    // Start is called before the first frame update
    void Awake()
    {
        playerInput = new CharacterAndAnimationControll();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        playerInput.CharacterControls.Move.started += onMovementInput;
        playerInput.CharacterControls.Move.canceled += onMovementInput;
        playerInput.CharacterControls.Move.performed += onMovementInput;
        playerInput.CharacterControls.Run.started += onRun;
        playerInput.CharacterControls.Run.canceled += onRun;
        playerInput.CharacterControls.Jump.started += onJump;
        playerInput.CharacterControls.Jump.canceled += onJump;

        setupJumpVariable();
        }

        void setupJumpVariable(){
            float timeToApex = maxJumpTime / 2;
            gravity = (-2 * maxJumpHeight) /Mathf.Pow(timeToApex, 2);
            initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
            float secondJumpGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
            float secondJumpInitialVelocity = ( 2* (maxJumpHeight + 2)) / (timeToApex * 1.25f);
            float thirdJumpGravity = (-2 * (maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
            float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 4)) / (timeToApex * 1.5f);

            initialJumpVelocities.Add(1, initialJumpVelocity);
            initialJumpVelocities.Add(2, secondJumpInitialVelocity);
            initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

            jumpGravities.Add(0, gravity);
            jumpGravities.Add(1, gravity);
            jumpGravities.Add(2, secondJumpGravity);
            jumpGravities.Add(3, thirdJumpGravity);

        }

   

    void handleJump(){
            if (!isJumping && characterController.isGrounded && isJumpPressed){
                //set animator here
                animator.SetBool("isJumping",true);
                isJumpAnimating = true;
                isJumping = true;
                jumpCount += 1;
                currentMovement.y = initialJumpVelocities[jumpCount] * 0.5f;
                currentRunMovement.y = initialJumpVelocities[jumpCount] * 0.5f;
            }else if(!isJumpPressed && isJumping &&characterController.isGrounded){
              isJumping = false;  
            }
         }

         IEnumerable jumpResetRoutine(){
           yield return new WaitForSeconds(0.5f); 
           jumpCount = 0;
         }

            void onJump (InputAction.CallbackContext context)
            {
              isJumpPressed = context.ReadValueAsButton(); 
            }
            void onRun (InputAction.CallbackContext context)
            {
                isRunPressed = context.ReadValueAsButton();
            }


        void handleRotation(){
            Vector3 positionToLookAt;
            //the change in position our character should point to
            positionToLookAt.x = currentMovement.x;
            positionToLookAt.y = 0.0f;
            positionToLookAt.z = currentMovement.z;

            //the current rotation of our character
            Quaternion currentRotation = transform.rotation;

            if(isMovementPressed){
                // new rotation based on wgere you are
                Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
                transform.rotation = Quaternion.Slerp(currentRotation,targetRotation,rotationFactorPerFrame * Time.deltaTime);


            }
        }

        void onMovementInput(InputAction.CallbackContext ctx){
        currentMovementInput = ctx.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x * jogMultiplier;
        currentMovement.z = currentMovementInput.y * jogMultiplier;
        isMovementPressed = currentMovement.x != 0 || currentMovement.z != 0;
        //isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
        currentRunMovement.x = currentMovementInput.x * runMultiplier;
        currentRunMovement.z = currentMovementInput.y * runMultiplier;
        
        
        }

        void handleAnimation(){
            bool isJogging = animator.GetBool("isJogging");
            bool isRunning = animator.GetBool("isRunning");

            if (isMovementPressed && !isJogging){
                animator.SetBool("isJogging", true);
            }

            else if (!isJogging && !isMovementPressed){
                animator.SetBool("isJogging", false);
            }
            else if (isJogging && !isMovementPressed){
                animator.SetBool("isJogging", false);
            }

            if(isMovementPressed && isRunPressed && !isRunning)
            {
                animator.SetBool("isRunning",true);
            }
            else if(!isMovementPressed || isRunPressed && isRunning){
                animator.SetBool("isRunning",false);
            }
        }

            void handleGravity()
            {   
                bool isFalling = currentMovement.y <= 0.0f || isJumpPressed;
                fallMultiplier = 2.0f;

                if(characterController.isGrounded){
                    if(isJumpAnimating){
                       animator.SetBool("isJumping", false);
                       isJumpAnimating = false;
                    
                    }
                    currentMovement.y = groundedGravity; 
                    currentRunMovement.y = groundedGravity;
                }
                else if(isFalling){
                    float previousYVelocity = currentMovement.y;
                    float newYVelocity = currentMovement.y + (jumpGravities[jumpCount] * fallMultiplier * Time.deltaTime);
                    float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * 0.5f, -20.0f);
                    currentMovement.y += newYVelocity;
                    currentRunMovement.y += nextYVelocity; 
                }else {
                    float previousYVelocity = currentMovement.y;
                    float newYVelocity = currentMovement.y + (gravity * Time.deltaTime);
                    float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
                    currentMovement.y = nextYVelocity;
                    currentRunMovement.y = nextYVelocity;
                }
            }


    
        

    // Update is called once per frame
    void Update()
    {
       
        handleRotation();
        handleAnimation();
        
         
        if(isRunPressed){
            characterController.Move(currentRunMovement * Time.deltaTime);
        }
        else{
           characterController.Move(currentMovement * Time.deltaTime); 
        }
         handleGravity(); 
        handleJump();  
    
    }
    void OnEnable(){
        playerInput.CharacterControls.Enable();
    }

    void OnDisable(){
        playerInput.CharacterControls.Disable();
    }
    }

    