using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ARGravityGun : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    public Camera ARCamera;
    public GameObject OnButton;      // кнопка гравипушки
    public GameObject GravityGun;
    public Transform HoldPosition;

    [Header("Cube prefab (assign in Inspector)")]
    public GameObject CubePrefab;

    [Header("Settings")]
    public float attractSpeed = 6f;
    public float throwForce = 12f;
    public float grabLiftOffset = 0.12f;

    private GameObject grabbedObject;
    private Rigidbody grabbedRB;
    private Renderer buttonRenderer;

    private bool isHolding = false;     
    private bool isAttracting = false;  

    void Start()
    {
        if (ARCamera == null) ARCamera = Camera.main;

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
        if (ARCamera == null) return;

        Vector2 pointerPos = GetPointerPosition();
        bool pointerDown = IsPointerDown();
        bool pointerUp = IsPointerUp();

        Ray ray = ARCamera.ScreenPointToRay(pointerPos);
        RaycastHit hit;

        // --- Гравипушка: захват / бросок
        if (pointerDown && OnButton != null && Physics.Raycast(ray, out hit) && hit.collider.gameObject == OnButton)
        {
            if (!isHolding)
            {
                TryGrabObject();
            }
            else
            {
                ThrowObject();
            }
        }

        // --- Притягивание к HoldPosition
        if (isAttracting && isHolding && grabbedObject != null)
        {
            MoveToHoldPoint();
        }

        if (pointerUp)
        {
            isAttracting = false;
        }
    }

    // --------------------------
    // Input System helpers
    // --------------------------
    private Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        if (Mouse.current != null) 
            return Mouse.current.position.ReadValue();
        return Vector2.zero;
    }

    private bool IsPointerDown()
    {
        bool down = false;
        if (Touchscreen.current != null)
            down |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        if (Mouse.current != null)
            down |= Mouse.current.leftButton.wasPressedThisFrame;
        return down;
    }

    private bool IsPointerUp()
    {
        bool up = false;
        if (Touchscreen.current != null)
            up |= Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
        if (Mouse.current != null)
            up |= Mouse.current.leftButton.wasReleasedThisFrame;
        return up;
    }

    // --------------------------
    // Захват объекта гравипушкой
    // --------------------------
    private void TryGrabObject()
    {
        if (GravityGun == null) return;

        if (Physics.Raycast(GravityGun.transform.position, GravityGun.transform.forward, out RaycastHit hit, 3f))
        {
            if (hit.collider != null && hit.collider.CompareTag("GameObject"))
            {
                grabbedObject = hit.collider.gameObject;
                grabbedRB = grabbedObject.GetComponent<Rigidbody>();
                if (grabbedRB == null) grabbedRB = grabbedObject.AddComponent<Rigidbody>();

                grabbedRB.useGravity = false;
                grabbedRB.constraints = RigidbodyConstraints.FreezeRotation;
                grabbedObject.transform.position += Vector3.up * grabLiftOffset;

                isHolding = true;
                isAttracting = true;

                if (buttonRenderer != null) buttonRenderer.material.color = Color.green;
            }
        }
    }

    // --------------------------
    // Перемещение удерживаемого объекта к HoldPosition
    // --------------------------
    private void MoveToHoldPoint()
    {
        if (grabbedRB == null || HoldPosition == null || grabbedObject == null) return;

        float step = attractSpeed * Time.deltaTime;
        grabbedRB.MovePosition(Vector3.MoveTowards(grabbedObject.transform.position, HoldPosition.position, step));
    }

    // --------------------------
    // Бросок объекта
    // --------------------------
    private void ThrowObject()
    {
        if (grabbedRB == null || grabbedObject == null || ARCamera == null) return;

        grabbedRB.constraints = RigidbodyConstraints.None;
        grabbedRB.useGravity = true;
        grabbedRB.AddForce(ARCamera.transform.forward * throwForce, ForceMode.Impulse);

        grabbedObject = null;
        grabbedRB = null;
        isHolding = false;

        if (buttonRenderer != null) buttonRenderer.material.color = Color.red;
    }

    // --------------------------
    // Спавн куба (оставляем без изменений)
    // --------------------------
    public void SpawnBox()
    {
        if (CubePrefab == null || ARCamera == null) return;

        Vector3 spawnPos = ARCamera.transform.position + ARCamera.transform.forward * 0.8f + Vector3.down * 0.2f;
        GameObject cube = Instantiate(CubePrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();

        if (!cube.CompareTag("GameObject")) cube.tag = "GameObject";
    }
}
