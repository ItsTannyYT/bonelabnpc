#if BONELAB_STUBS
using System;
using System.Collections.Generic;

namespace UnityEngine;

public class Object
{
    public static void DontDestroyOnLoad(object target) { }

    public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : class => original;

    public static Array FindObjectsOfType(Type type) => Array.Empty<object>();
}

public class GameObject
{
    public string name;
    public Transform transform { get; } = new();
    public HideFlags hideFlags;

    public GameObject(string name) => this.name = name;

    public T AddComponent<T>() where T : new() => new();

    public T? GetComponent<T>() where T : class => null;

    public T? GetComponentInChildren<T>() where T : class => null;

    public T[] GetComponentsInChildren<T>() where T : class => Array.Empty<T>();
}

public class AssetBundle
{
    public static AssetBundle? LoadFromFile(string path) => null;

    public T? LoadAsset<T>(string name) where T : class => null;

    public void Unload(bool unloadAllLoadedObjects) { }
}

public class MonoBehaviour
{
    public GameObject gameObject { get; } = new("Stub");
    public Transform transform => gameObject.transform;

    public T? GetComponent<T>() where T : class => gameObject.GetComponent<T>();

    public int GetInstanceID() => 0;
}

public class Transform
{
    public string name = string.Empty;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Transform? parent;
    private readonly List<Transform> _children = new();

    public Vector3 forward => new(0f, 0f, 1f);
    public int childCount => _children.Count;

    public Transform GetChild(int index) => _children[index];

    public void SetParent(Transform? parent, bool worldPositionStays) => this.parent = parent;

    public bool IsChildOf(Transform parent) => false;

    public void LookAt(Vector3 position) { }
}

public struct Vector3
{
    public float x;
    public float y;
    public float z;

    public Vector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static Vector3 zero => new(0f, 0f, 0f);

    public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);

    public Vector3 normalized
    {
        get
        {
            var mag = magnitude;
            return mag > 0f ? new Vector3(x / mag, y / mag, z / mag) : zero;
        }
    }

    public static Vector3 operator -(Vector3 left, Vector3 right) => new(left.x - right.x, left.y - right.y, left.z - right.z);

    public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.x + right.x, left.y + right.y, left.z + right.z);

    public static Vector3 operator *(Vector3 left, float scalar) => new(left.x * scalar, left.y * scalar, left.z * scalar);

    public static float Dot(Vector3 lhs, Vector3 rhs) => lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;

    public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
}

public struct Quaternion
{
    public static Quaternion identity => new();
}

public static class Time
{
    public static float time => 0f;
    public static float deltaTime => 0.02f;
}

public class Camera
{
    public static Camera? main => null;
    public Transform transform { get; } = new();
}

public static class Physics
{
    public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance)
    {
        hit = new RaycastHit();
        return false;
    }
}

public struct RaycastHit
{
    public Collider? collider;
}

public class Collider
{
    public string name = string.Empty;
    public Transform transform { get; } = new();
    public Rigidbody? attachedRigidbody;
    public bool enabled = true;

    public bool CompareTag(string tag) => false;
}

public class Rigidbody
{
    public bool isKinematic;
    public bool detectCollisions;
    public Vector3 velocity;
}

public class Collision
{
    public Collider collider { get; } = new();
    public Vector3 relativeVelocity => Vector3.zero;
}

public class Animator
{
    public bool enabled;
}

public enum HideFlags
{
    HideAndDontSave,
}

public static class Mathf
{
    public static float Max(float a, float b) => Math.Max(a, b);

    public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
}
#endif
