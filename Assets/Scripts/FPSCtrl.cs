using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FPSCtrl : MonoBehaviour
{
    // 스피드 조정 변수
    [SerializeField]
    private float walkSpeed;
    [SerializeField]
    private float runSpeed;
    [SerializeField]
    private float crouchSpeed;

    private float applySpeed;




    [SerializeField]
    private float jumpForce;

    // 상태 변수
    private bool isWalk = false;
    private bool isRun = false;
    private bool isGround = true;
    private bool isCrouch = false;

    private CapsuleCollider capsuleCollider;

    // 움직임 체크 변수
    private Vector3 lastPos;

    // 앉았을 때 얼마나 앉을지 결정하는 함수.
    [SerializeField]
    private float crouchPosY;
    private float orighinPosY;
    private float applyCrouchPosY;

    // 민감도
    [SerializeField]
    private float lookSensitivity;

    //필요한 컴포넌트
    [SerializeField]
    private Camera theCamera;
    private Rigidbody myRigid;
    [SerializeField]
    private GunController theGunController;
    [SerializeField]
    private Crosshair theCrosshair;

    // 카메라 한계
    [SerializeField]
    private float cameraRotationLimit;
    private float currentCameraRotationX = 0;

    // Start is called before the first frame update
    void Start()
    {
        myRigid = GetComponent<Rigidbody>();
        // theCamera = FindObjectOfType<Camera>();
        applySpeed = walkSpeed;
        capsuleCollider = GetComponent<CapsuleCollider>();

        theGunController = FindObjectOfType<GunController>();
        theCrosshair = FindObjectOfType<Crosshair>();
        // 초기화
        orighinPosY = theCamera.transform.localPosition.y;
        applyCrouchPosY = orighinPosY;
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        CameraRotation();
        CharacterRotation();
        TryRun();
        IsGround();
        TryJump();
        TryCrouch();
        MoveCheck();
    }

    // 앉기 시도
    private void TryCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Crouch();
        }
    }
    // 앉는 동작
    private void Crouch()
    {
        isCrouch = !isCrouch;
        theCrosshair.CrouchingAnimation(isCrouch);
        if (isCrouch)
        {
            applySpeed = crouchSpeed;
            applyCrouchPosY = crouchPosY;
        }
        else
        {
            applySpeed = walkSpeed;
            applyCrouchPosY = orighinPosY;
        }

        StartCoroutine(CrouchCoroutine());
    }
    //부드러운 앉기 동작
    IEnumerator CrouchCoroutine()
    {
        
        float _posY = theCamera.transform.localPosition.y;
        int count = 0;

        while (_posY != applyCrouchPosY)
        {
            count++;
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.3f);
            theCamera.transform.localPosition = new Vector3(0, _posY, 0);
            if (count > 15)
                break;
            yield return null;
        }
        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0);
    }
   // 점프 시도
    private void TryJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGround)
        {
            Jump();
            Debug.Log("Jump?");
        }

    }
    // 점프
    private void Jump()
    {
        if (isCrouch)
        {  
            Crouch(); 
        }

        myRigid.velocity = transform.up * jumpForce;
        Debug.Log("Jump!");
    }
    // 땅에 붙어있는지 체크
    private void IsGround()
    {
        isGround = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f); // extents는 y값의 1/2이라고 알려준 것
        theCrosshair.RunningAnimation(!isGround);
        // Debug.Log("IsGround?");
    }
    // 뛰기 시도
    private void TryRun()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Running();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            RunningCancel();
        }
    }
    // 뛰기
    private void Running()
    {
        if (isCrouch)
        {
            Crouch();
        }
        theGunController.CancelFineSight();

        isRun = true;
        theCrosshair.RunningAnimation(isRun);
        applySpeed = runSpeed;
    }
    // 뛰기 취소
    private void RunningCancel()
    {
        isRun = false;
        theCrosshair.RunningAnimation(isRun);
        applySpeed = walkSpeed;
    }
    // 걷기
    private void Move()
    {
        float _moveDirX = Input.GetAxisRaw("Horizontal");
        // 1 -1 0 x축
        float _moveDirZ = Input.GetAxisRaw("Vertical");
        // y축

        Vector3 _moveHorizontal = transform.right * _moveDirX;
        // (1, 0, 0) * 1
        Vector3 _moveVertical = transform.forward * _moveDirZ;
        //(0, 0, 1) * 1

        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed; // 걷는 속도도 곱해줘야 얼마나 움직이는지 정해짐.
        // 위의 두 벡터 값을 합쳐주는 역할
        // (1, 0, 0) (0, 0, 1)
        // (1, 0, 1) = 2
        // (0.5, 0, 0.5) = 1 1초에 얼마나 이동시킬 것인가

        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime); // 시간당(1초) 얼마나 움직일 것인가

    }
    
    private void MoveCheck()
    {
        if (!isRun && !isCrouch && isGround)
        {
            if (Vector3.Distance(lastPos, transform.position) >= 0.01f)
                isWalk = true;
            else
                isWalk = false;

            theCrosshair.WalkingAnimation(isWalk);
            lastPos = transform.position;
        }
    }

    // 카메라 마우스로 돌리기
    private void CameraRotation()
    {
        float _xRotation = Input.GetAxisRaw("Mouse Y");
        float _cameraRotationX = _xRotation * lookSensitivity;
        currentCameraRotationX -= _cameraRotationX;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);

        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0);
    }
    private void CharacterRotation()
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;
        myRigid.MoveRotation(myRigid.rotation * Quaternion.Euler(_characterRotationY));
       // Debug.Log(myRigid.rotation);
       // Debug.Log(myRigid.rotation.eulerAngles);
    }
}

