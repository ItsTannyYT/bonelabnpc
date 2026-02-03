using System.IO;
using MelonLoader;
using UnityEngine;

namespace BonelabNpcMod;

public static class NpcAssetSpawner
{
    private const string BundleFileName = "custom-npc.bundle";
    private const string PrefabName = "CustomNpc";
    private static bool _spawned;

    public static void TrySpawn()
    {
        if (_spawned)
        {
            return;
        }

        var bundlePath = Path.Combine(MelonEnvironment.ModsDirectory, "BonelabNpcMod", BundleFileName);
        if (!File.Exists(bundlePath))
        {
            return;
        }

        var bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            return;
        }

        var prefab = bundle.LoadAsset<GameObject>(PrefabName);
        if (prefab == null)
        {
            bundle.Unload(false);
            return;
        }

        var camera = Camera.main;
        var spawnPosition = camera != null
            ? camera.transform.position + camera.transform.forward * 3f
            : Vector3.zero;

        Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
        bundle.Unload(false);
        _spawned = true;
    }
}
