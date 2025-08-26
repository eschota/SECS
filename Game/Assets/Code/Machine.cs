using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
// БЫЛО: [RequireComponent(typeof(NetworkTransform))] // конфликтует с физикой
// СТАЛО: используем сетевую физику
// или NetworkRigidbody, если у вас так называется 
public class Machine : NetworkBehaviour
{
    [Header("Build Target")]
    [SerializeField] private Transform visualRoot;   // сюда инстансим клетки (если пусто — возьмём transform)

    [Tooltip("Фолбэк-радиус до расчёта баундов (метры)")]
    public float safeRadius = 1.5f;

    // === Расчитанные параметры ===
    public Bounds WorldBounds { get; private set; } // AABB в мире
    public Vector3 LocalCenter { get; private set; } // центр баундов в локале корня
    public float BoundsMaxExtent { get; private set; } // max(extents)
    public Transform CenterOfMass { get; private set; } // «истинный» центр (центроид)

    public static event Action<Machine> OnLocalMachineReady;

    private const string TAG = "<color=#4DA3FF>[Machine]</color>";
    private int _machineLayer = -1;
    private Dictionary<string, io_base> prefabLookup = new Dictionary<string, io_base>();
    private byte[] savedBlueprintData; // Сохраняем blueprint для отправки новым игрокам

    public override void Spawned()
    {
        if (!visualRoot) visualRoot = transform;
        EnsureCenterOfMass();

        // Настройки корневого Rigidbody под сетевую физику
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.None; // интерполяция будет на стороне Fusion
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Debug.Log($"{TAG} Spawned  NO={Object.Id}  StateAuth={Object.HasStateAuthority}  InputAuth={Object.HasInputAuthority}");
    }

    private void EnsureCenterOfMass()
    {
        if (CenterOfMass == null)
        {
            var go = new GameObject("CenterOfMass");
            go.hideFlags = HideFlags.DontSave;
            CenterOfMass = go.transform;
            CenterOfMass.SetParent(transform, false);
            CenterOfMass.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// StateAuthority рассылает blueprint всем клиентам после спавна.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_SetBlueprint(byte[] data)
    {
        EnsureCenterOfMass();

        var bp = BlueprintCodec.FromBytes(data);
        Debug.Log($"{TAG} Получен blueprint ({bp.cells.Count} клеток) → сборка...");

        BuildFromBlueprint(bp);
        RecalculateBounds();

        // запас: берём 2 * наибольший half-extent
        safeRadius = Mathf.Max(2f * BoundsMaxExtent, 1.0f);

        if (Object.HasInputAuthority)
            OnLocalMachineReady?.Invoke(this);

        Debug.Log($"{TAG} Сборка завершена. Extent={BoundsMaxExtent:F2}, safeRadius={safeRadius:F2}");

        // Сохраняем blueprint данные для отправки новым игрокам
        savedBlueprintData = data;
    }

    /// <summary>
    /// Отправляем информацию о существующей машине новому игроку
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_SendMachineToNewPlayer(PlayerRef newPlayer)
    {
        if (Runner.LocalPlayer == newPlayer && Object.InputAuthority != newPlayer)
        {
            if (savedBlueprintData != null)
            {
                Debug.Log($"{TAG} Отправляем существующую машину новому игроку {newPlayer}");
                RPC_SetBlueprint(savedBlueprintData);
            }
            else
            {
                Debug.LogWarning($"{TAG} Нет сохраненного blueprint для отправки новому игроку");
            }
        }
    }

    // ------------------ Сборка из blueprint ------------------

    private void BuildFromBlueprint(MachineBlueprint bp)
    {
        // очистим предыдущую сборку
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
            Destroy(visualRoot.GetChild(i).gameObject);

        var creator = Creator.instance ?? FindFirstObjectByType<Creator>();
        if (creator == null || creator.prefabs == null || creator.prefabs.Count == 0)
        {
            Debug.LogError($"{TAG} Creator.prefabs пуст — не из чего строить.");
            return;
        }

        if (creator.prefabLookup == null || creator.prefabLookup.Count == 0)
        {
            Debug.LogError($"{TAG} Creator.prefabLookup пуст! Префабы должны быть сериализованы в сцене.");
            return;
        }

        // 1) Считаем центроид
        Vector3 centroid = Vector3.zero;
        int count = 0;
        foreach (var cd in bp.cells) { centroid += cd._target_world_position; count++; }
        if (count > 0) centroid /= count;

        // Инстансим клетки как чистый компаунд (коллайдеры + рендеры)
        foreach (var cd in bp.cells)
        {
            io_base prefab;
            if (!string.IsNullOrEmpty(cd._prefab_name) && creator.prefabLookup.TryGetValue(cd._prefab_name, out prefab))
            {
                var go = Instantiate(prefab.gameObject, visualRoot);
                go.name = string.IsNullOrEmpty(cd.name) ? $"Cell_{cd._prefab_name}" : cd.name;

                var cellComponent = go.GetComponent<io_base>();
                if (cellComponent != null)
                    cellComponent.DeserializeFromData(cd);

                Vector3 local = cd._target_world_position - centroid;
                go.transform.localPosition = local;
                go.transform.localRotation = cd._target_world_rotation;

                PrepareChildForCompound(go);
            }
            else
            {
                Debug.LogWarning($"{TAG} Prefab not found: {cd._prefab_name}, skip");
            }
        }

        // корневой Rigidbody — единственный
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
    }

    /// <summary>
    /// Оставляем только визуал/коллайдеры; убираем сети/риги у детей.
    /// </summary>
    private void PrepareChildForCompound(GameObject go)
    {
        foreach (var no in go.GetComponentsInChildren<NetworkObject>(true)) Destroy(no);
        foreach (var nb in go.GetComponentsInChildren<NetworkBehaviour>(true)) Destroy(nb);
        foreach (var childRb in go.GetComponentsInChildren<Rigidbody>(true)) Destroy(childRb);
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
        // Рендеры/Коллайдеры остаются включёнными — образуют compound для корневого Rigidbody.
    }

    private void RecalculateBounds()
    {
        bool has = false;
        var b = new Bounds(transform.position, Vector3.zero);

        var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }

        var colliders = visualRoot.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            if (!has) { b = c.bounds; has = true; }
            else b.Encapsulate(c.bounds);
        }

        if (!has) b = new Bounds(transform.position, Vector3.one);

        WorldBounds = b;
        LocalCenter = transform.InverseTransformPoint(b.center);
        BoundsMaxExtent = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);

        EnsureCenterOfMass();
        CenterOfMass.localPosition = LocalCenter;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (WorldBounds.size != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size);
        }
        if (CenterOfMass != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(CenterOfMass.position, 0.2f);
        }
    }
#endif
}
