using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARGravityGun : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    public Camera ARCamera;
    public GameObject OnButton;
    public GameObject GravityGun;
    public Transform HoldPosition;
    public GameObject LeverArm;

    [Header("Cube prefab (you can assign to either field)")]
    public GameObject Cube;       // backward-compatible field
    public GameObject CubePrefab; // new field

    [Header("Settings")]
    public float attractSpeed = 6f;
    public float throwForce = 12f;
    public float grabLiftOffset = 0.12f;

    private GameObject grabbedObject;
    private Rigidbody grabbedRB;
    private Renderer buttonRenderer;

    private bool buttonPressed = false;  // текущее состояние кнопки
    private bool isHolding = false;      // держим ли объект
    private bool isAttracting = false;   // тянем ли объект сейчас

    void Start()
    {
        if (ARCamera == null) ARCamera = Camera.main;

        if (CubePrefab == null && Cube != null)
        {
            CubePrefab = Cube;
        }

        if (OnButton != null)
        {
            buttonRenderer = OnButton.GetComponent<Renderer>();
            if (buttonRenderer != null)
                buttonRenderer.material.color = Color.red;
        }

        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 12;
        Physics.defaultContactOffset = 0.005f;
    }

    void Update()
    {
#if UNITY_EDITOR
        bool mouseDown = Input.GetMouseButtonDown(0);
        bool mouseUp = Input.GetMouseButtonUp(0);
        bool mouseHeld = Input.GetMouseButton(0);
        Vector2 touchPos = Input.mousePosition;
#else
        bool mouseDown = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        bool mouseUp = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
        bool mouseHeld = Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary);
        Vector2 touchPos = Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
#endif

        if (ARCamera == null) return;

        Ray ray = ARCamera.ScreenPointToRay(touchPos);
        RaycastHit hit;

        // Обработка нажатия на кнопку OnButton
        if (mouseDown && OnButton != null && Physics.Raycast(ray, out hit) && hit.transform == OnButton.transform)
        {
            if (!isHolding)
            {
                // Если не держим объект — пытаемся захватить (начать притягивать)
                buttonPressed = true;
                isAttracting = true;
                if (buttonRenderer != null) buttonRenderer.material.color = Color.green;
                TryGrabObject();
            }
            else
            {
                // Если уже держим объект — это команда на выстрел
                ThrowObject();
                buttonPressed = false;
                isAttracting = false;
                if (buttonRenderer != null) buttonRenderer.material.color = Color.red;
            }
        }

        // Если сейчас притягиваем и есть объект, тянем его к HoldPosition
        if (isAttracting && isHolding && grabbedObject != null)
        {
            MoveToHoldPoint();
        }

        // Если кнопку отпустили — перестаём тянуть, но не отпускаем куб
        if (mouseUp && buttonPressed)
        {
            isAttracting = false;
            // кнопка остаётся "включена" пока мы держим объект
            if (buttonRenderer != null) buttonRenderer.material.color = Color.yellow; // например, желтый — удержание без притяжения
        }
    }

    private void TryGrabObject()
    {
        if (GravityGun == null) return;

        RaycastHit hit;
        if (Physics.Raycast(GravityGun.transform.position, GravityGun.transform.forward, out hit, 3f))
        {
            if (hit.collider != null && hit.collider.CompareTag("GameObject"))
            {
                grabbedObject = hit.collider.gameObject;
                grabbedRB = grabbedObject.GetComponent<Rigidbody>();
                if (grabbedRB == null)
                {
                    Debug.LogWarning("[ARGravityGun] Объект без Rigidbody.");
                    grabbedObject = null;
                    return;
                }

                grabbedRB.useGravity = false;
                grabbedRB.constraints = RigidbodyConstraints.FreezeRotation;
                grabbedObject.transform.position += Vector3.up * grabLiftOffset;

                isHolding = true;
            }
        }
    }

    private void MoveToHoldPoint()
    {
        if (grabbedRB == null || HoldPosition == null || grabbedObject == null) return;

        float step = attractSpeed;
        grabbedRB.MovePosition(Vector3.MoveTowards(grabbedObject.transform.position, HoldPosition.position, step));
    }

    private void ThrowObject()
    {
        if (grabbedRB == null || grabbedObject == null || ARCamera == null) return;

        grabbedRB.constraints = RigidbodyConstraints.None;
        grabbedRB.useGravity = true;

        grabbedRB.AddForce(ARCamera.transform.forward * throwForce, ForceMode.Impulse);

        grabbedObject = null;
        grabbedRB = null;
        isHolding = false;
    }

    public void SpawnBox()
    {
        if (CubePrefab == null)
        {
            Debug.LogWarning("[ARGravityGun] CubePrefab не назначен.");
            return;
        }

        if (ARCamera == null)
        {
            Debug.LogWarning("[ARGravityGun] ARCamera не назначена.");
            return;
        }

        Vector3 spawnPos = ARCamera.transform.position + ARCamera.transform.forward * 0.8f + Vector3.down * 0.2f;
        Instantiate(CubePrefab, spawnPos, Quaternion.identity);
    }
}
