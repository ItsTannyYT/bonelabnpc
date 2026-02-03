using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BonelabNpcMod;

public sealed class GunAdapter
{
    private static readonly string[] MuzzleNames =
    {
        "muzzle",
        "Muzzle",
        "muzzleTransform",
        "MuzzleTransform",
        "firePoint",
        "FirePoint",
        "barrelEnd",
        "BarrelEnd",
        "muzzleTip",
        "MuzzleTip",
    };

    private readonly MonoBehaviour _source;
    private readonly MethodInfo? _fireMethod;
    private readonly MethodInfo? _pullTriggerMethod;
    private readonly MethodInfo? _chargeMethod;
    private readonly PropertyInfo? _isHeldProperty;
    private readonly FieldInfo? _isHeldField;
    private readonly Transform? _aimTransform;
    private readonly Rigidbody? _rigidbody;

    private GunAdapter(MonoBehaviour source)
    {
        _source = source;

        var methods = source.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        _fireMethod = FindMethod(methods, "Fire")
            ?? FindMethod(methods, "TryFire")
            ?? FindMethod(methods, "OnFire");

        _pullTriggerMethod = FindMethod(methods, "PullTrigger")
            ?? FindMethod(methods, "TriggerPull");

        _chargeMethod = FindMethod(methods, "Charge");

        var properties = source.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _isHeldProperty = properties.FirstOrDefault(p =>
            string.Equals(p.Name, "IsHeld", StringComparison.OrdinalIgnoreCase) &&
            p.PropertyType == typeof(bool));

        var fields = source.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _isHeldField = fields.FirstOrDefault(f =>
            string.Equals(f.Name, "isHeld", StringComparison.OrdinalIgnoreCase) &&
            f.FieldType == typeof(bool));

        _aimTransform = ResolveMuzzleTransform(source);
        _rigidbody = source.GetComponent<Rigidbody>();
    }

    public Transform Transform => _source.transform;

    public bool IsValid => _source != null;

    public Transform? AimTransform => _aimTransform;

    public bool IsHeld
    {
        get
        {
            if (_isHeldProperty != null)
            {
                return (bool)(_isHeldProperty.GetValue(_source) ?? false);
            }

            if (_isHeldField != null)
            {
                return (bool)(_isHeldField.GetValue(_source) ?? false);
            }

            return false;
        }
    }

    public void Fire()
    {
        if (_chargeMethod != null)
        {
            _chargeMethod.Invoke(_source, Array.Empty<object>());
        }

        if (_fireMethod != null)
        {
            InvokeMethod(_fireMethod);
            return;
        }

        if (_pullTriggerMethod != null)
        {
            InvokeMethod(_pullTriggerMethod);
        }
    }

    public void AimAt(Vector3 position)
    {
        _source.transform.LookAt(position);
    }

    public void SetKinematic(bool value)
    {
        if (_rigidbody == null)
        {
            return;
        }

        _rigidbody.isKinematic = value;
        _rigidbody.detectCollisions = !value;
    }

    public void Drop()
    {
        Transform.SetParent(null, worldPositionStays: true);
        SetKinematic(false);
    }

    private void InvokeMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            method.Invoke(_source, Array.Empty<object>());
            return;
        }

        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool))
        {
            method.Invoke(_source, new object[] { true });
            return;
        }

        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
        {
            method.Invoke(_source, new object[] { 1f });
        }
    }

    public static bool TryCreate(MonoBehaviour source, out GunAdapter adapter)
    {
        adapter = new GunAdapter(source);
        if (adapter._fireMethod == null && adapter._pullTriggerMethod == null)
        {
            return false;
        }

        return true;
    }

    private static MethodInfo? FindMethod(MethodInfo[] methods, string name)
    {
        return methods.FirstOrDefault(method =>
            string.Equals(method.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static Transform? ResolveMuzzleTransform(MonoBehaviour source)
    {
        var type = source.GetType();
        foreach (var name in MuzzleNames)
        {
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                if (prop.PropertyType == typeof(Transform))
                {
                    return prop.GetValue(source) as Transform;
                }

                if (prop.PropertyType == typeof(GameObject))
                {
                    return (prop.GetValue(source) as GameObject)?.transform;
                }
            }

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                if (field.FieldType == typeof(Transform))
                {
                    return field.GetValue(source) as Transform;
                }

                if (field.FieldType == typeof(GameObject))
                {
                    return (field.GetValue(source) as GameObject)?.transform;
                }
            }
        }

        return null;
    }
}
