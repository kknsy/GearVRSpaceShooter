﻿using System;
using UnityEngine;

public class MovementControlComponent : MonoBehaviour
{
    // Constants

    public static readonly float DefaultBaseMovementSpeed = 60;
    public static readonly float DefaultRotationSpeed = 1.5f;
    public static readonly float DefaultAcceleration = 0.015f;
    
    // Unity fields

    [Header("Speed")]
    [Tooltip("")]
    public float BaseMovementSpeed = DefaultBaseMovementSpeed;
    [Tooltip("")]
    public float Acceleration = DefaultAcceleration;
    [Tooltip("")]
    public float YawRotationSpeed = DefaultRotationSpeed;
    [Tooltip("")]
    public float PitchRotationSpeed = DefaultRotationSpeed;
    [Tooltip("")]
    public float RollRotationSpeed = DefaultRotationSpeed;

    [Header("Speed Steps")]
    [Tooltip("")]
    public int ForwardSpeedSteps = 8;
    [Tooltip("")]
    public int BackwardSpeedSteps = 4;

    [Header("Deadzone")]
    [Range(0f, 0.99f)]
    [Tooltip("")]
    public float InnerDeadzoneValue = 0.01f;
    [Range(0.01f, 1f)]
    [Tooltip("")]
    public float OuterDeadzoneValue = 0.25f;
    
    [Header("Slider properties")]

    [Tooltip("")]
    public Transform XUpTransform;
    [Tooltip("")]
    public Transform XDownTransform;

    [Tooltip("")]
    public Transform YUpTransform;
    [Tooltip("")]
    public Transform YDownTransform;

    [Tooltip("")]
    public Transform ZUpTransform;
    [Tooltip("")]
    public Transform ZDownTransform;

    [Tooltip("")]
    public Transform CurrentSpeedTransform;
    [Tooltip("")]
    public Transform TargetSpeedTransform;
    [Tooltip("")]
    public float ScaleValuePerSpeedStep;

    [Tooltip("")]
    public Transform MiniShipTransform;

    [Header("Misc")]
    [Tooltip("")]
    public bool InvertY;

    // Fields
    
    private bool _movementEnabled = true;
    private float _currentSpeedFactor = 0;
    private float _targetSpeedFactor = 0;

    // Methods

    void FixedUpdate () {
        OVRInput.FixedUpdate();

        HandleRotation();
        HandleSpeed();   
    }

    private void HandleRotation()
    {
        // movement can be disabled to relax hand and give the user the option of letting the controller go w/o affecting flight
        if (_movementEnabled)
        {
            var rotation = GetRotationQuaternion();

            var yawFactor = CalculateYawFactor(rotation);
            var pitchFactor = CalculatePitchFactor(rotation);
            var rollFactor = CalculateRollFactor(rotation);

            // do rotate
            transform.Rotate(yawFactor * YawRotationSpeed, pitchFactor * PitchRotationSpeed, rollFactor * RollRotationSpeed);

            // update rotation sliders
            if (YUpTransform != null && YDownTransform != null)
            {
                if (yawFactor > 0)
                {
                    YUpTransform.localScale = new Vector3(1, yawFactor * 2, 1); //TODO: weird values
                    YDownTransform.localScale = new Vector3(1, 0, 1);
                }
                else
                {
                    YUpTransform.localScale = new Vector3(1, 0, 1);
                    YDownTransform.localScale = new Vector3(1, yawFactor * 2, 1);
                }
            }

            if (XUpTransform != null && XDownTransform != null)
            {
                if (pitchFactor > 0)
                {
                    XUpTransform.localScale = new Vector3(pitchFactor * 2, 1, 1);
                    XDownTransform.localScale = new Vector3(0, 1, 1);
                }
                else
                {
                    XUpTransform.localScale = new Vector3(0, 1, 1);
                    XDownTransform.localScale = new Vector3(pitchFactor * 2, 1, 1);
                }
            }

            if (ZUpTransform != null && ZDownTransform != null)
            {
                if (rollFactor > 0)
                {
                    ZUpTransform.localScale = new Vector3(rollFactor * -2, 1, 1);
                    ZDownTransform.localScale = new Vector3(0, 1, 1);
                }
                else
                {
                    ZUpTransform.localScale = new Vector3(0, 1, 1);
                    ZDownTransform.localScale = new Vector3(rollFactor * -2, 1, 1);
                }
            }

            if (MiniShipTransform != null)
            {
                //var currentRotation = MiniShipTransform.localRotation;
                //var wantedRotation = currentRotation * Quaternion.AngleAxis(rollFactor * 100, Vector3.forward);
                //MiniShipTransform.rotation = Quaternion.Slerp(currentRotation, wantedRotation, Time.deltaTime * 5);
                MiniShipTransform.localRotation = new Quaternion(yawFactor, pitchFactor, rollFactor, 1);
            }

            if (OVRInput.GetUp(OVRInput.Button.PrimaryTouchpad))
            {
                _movementEnabled = false;
            }
        }
        else
        {
            if (OVRInput.GetUp(OVRInput.Button.PrimaryTouchpad))
            {
                _movementEnabled = true;
            }
        }
    }

    private void HandleSpeed()
    {
        if (OVRInput.Get(OVRInput.Touch.PrimaryTouchpad))
        {
            int stepSum = ForwardSpeedSteps + BackwardSpeedSteps;
            var speedTouchPosition = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad);
            //targetSpeedFactor = speedTouchPosition.x < 0.4f ? speedTouchPosition.y : 0;
            // + 1 to only have positive values, divided by 2 to have range 0 to 1
            _targetSpeedFactor = (speedTouchPosition.y + 1) / 2;
            _targetSpeedFactor = _targetSpeedFactor * stepSum;
            _targetSpeedFactor -= BackwardSpeedSteps;
        }

        _currentSpeedFactor = UpdateSpeedFactor(_currentSpeedFactor, _targetSpeedFactor);

        if (CurrentSpeedTransform != null)
        {
            CurrentSpeedTransform.localScale = new Vector3(1, _currentSpeedFactor * ScaleValuePerSpeedStep, 1);
        }
        if (TargetSpeedTransform != null)
        {
            TargetSpeedTransform.localScale = new Vector3(1, _targetSpeedFactor * ScaleValuePerSpeedStep, 1);
        }

        ApplyMovementTranslation(transform, BaseMovementSpeed);
    }

    private Quaternion GetRotationQuaternion()
    {
        // get active controller, either left- or right-hand based
        //var activeController = OVRInput.GetActiveController();

        // get gyroscope instead of accelerometer values because more precise AND based on orientation, not movement.
        // This is important because values have origin in calibrated null value (holding position?)
        //return OVRInput.GetLocalControllerRotation(activeController);
        //var rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote);
        return OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote);
    }

    private float CalculateAdjustedRotationFactor(float factor)
    {
        // factor in inner and outer deadzones, inner to correct for "jitter" and outer to handle out-of-bounds input ranges

        if (factor > -InnerDeadzoneValue && factor < InnerDeadzoneValue)
            return 0;
        if (factor < -OuterDeadzoneValue)
            return -OuterDeadzoneValue;
        if (factor > OuterDeadzoneValue)
            return OuterDeadzoneValue;

        return factor;
    }

    private float CalculateYawFactor(Quaternion rotation)
    {
        return CalculateAdjustedRotationFactor(rotation.x);
    }

    private float CalculatePitchFactor(Quaternion rotation)
    {
        return CalculateAdjustedRotationFactor(rotation.y);
    }

    private float CalculateRollFactor(Quaternion rotation)
    {
        return CalculateAdjustedRotationFactor(rotation.z);
    }

    private float UpdateSpeedFactor(float previousSpeed, float targetSpeed)
    {
        if (targetSpeed > previousSpeed)
        {
            return previousSpeed + Acceleration;
        }
        if (targetSpeed < previousSpeed)
        {
            return previousSpeed - Acceleration;
        }
        return previousSpeed;
    }

    private void ApplyMovementTranslation(Transform transform, float baseMovementSpeed)
    {
        transform.position += transform.forward * _currentSpeedFactor * baseMovementSpeed * Time.deltaTime;
    }
}