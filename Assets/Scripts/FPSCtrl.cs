using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FPSCtrl : MonoBehaviour
{
    // ���ǵ� ���� ����
    [SerializeField]
    private float walkSpeed;
    [SerializeField]
    private float runSpeed;
    [SerializeField]
    private float crouchSpeed;

    private float applySpeed;




    [SerializeField]
    private float jumpForce;

    // ���� ����
    private bool isWalk = false;
    private bool isRun = false;
    private bool isGround = true;
    private bool isCrouch = false;

    private CapsuleCollider capsuleCollider;

    // ������ üũ ����
    private Vector3 lastPos;

    // �ɾ��� �� �󸶳� ������ �����ϴ� �Լ�.
    [SerializeField]
    private float crouchPosY;
    private float orighinPosY;
    private float applyCrouchPosY;

    // �ΰ���
    [SerializeField]
    private float lookSensitivity;

    //�ʿ��� ������Ʈ
    [SerializeField]
    private Camera theCamera;
    private Rigidbody myRigid;
    [SerializeField]
    private GunController theGunController;
    [SerializeField]
    private Crosshair theCrosshair;

    // ī�޶� �Ѱ�
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
        // �ʱ�ȭ
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

    // �ɱ� �õ�
    private void TryCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Crouch();
        }
    }
    // �ɴ� ����
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
    //�ε巯�� �ɱ� ����
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
   // ���� �õ�
    private void TryJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGround)
        {
            Jump();
            Debug.Log("Jump?");
        }

    }
    // ����
    private void Jump()
    {
        if (isCrouch)
        {  
            Crouch(); 
        }

        myRigid.velocity = transform.up * jumpForce;
        Debug.Log("Jump!");
    }
    // ���� �پ��ִ��� üũ
    private void IsGround()
    {
        isGround = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f); // extents�� y���� 1/2�̶�� �˷��� ��
        theCrosshair.RunningAnimation(!isGround);
        // Debug.Log("IsGround?");
    }
    // �ٱ� �õ�
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
    // �ٱ�
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
    // �ٱ� ���
    private void RunningCancel()
    {
        isRun = false;
        theCrosshair.RunningAnimation(isRun);
        applySpeed = walkSpeed;
    }
    // �ȱ�
    private void Move()
    {
        float _moveDirX = Input.GetAxisRaw("Horizontal");
        // 1 -1 0 x��
        float _moveDirZ = Input.GetAxisRaw("Vertical");
        // y��

        Vector3 _moveHorizontal = transform.right * _moveDirX;
        // (1, 0, 0) * 1
        Vector3 _moveVertical = transform.forward * _moveDirZ;
        //(0, 0, 1) * 1

        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed; // �ȴ� �ӵ��� ������� �󸶳� �����̴��� ������.
        // ���� �� ���� ���� �����ִ� ����
        // (1, 0, 0) (0, 0, 1)
        // (1, 0, 1) = 2
        // (0.5, 0, 0.5) = 1 1�ʿ� �󸶳� �̵���ų ���ΰ�

        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime); // �ð���(1��) �󸶳� ������ ���ΰ�

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

    // ī�޶� ���콺�� ������
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

