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
public partial class Machine : NetworkBehaviour
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
    
    // Переменные для передачи blueprint по частям
    private List<byte[]> receivedChunks = new List<byte[]>();
    private int expectedChunks = 0;
    private int receivedChunksCount = 0;
private const string G = "<color=#00FF00>";
    private const string GE = "</color>";

    // Удаление «клетки» по мировой позиции (округляем до сетки 1х1х1) с подсказкой по имени
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_DestroyCellAt(Vector3 worldPos, string nameHint)
    {
        Vector3Int key = RoundToInt(worldPos);

        var cells = GetComponentsInChildren<io_base>(true);

        io_base target = cells.FirstOrDefault(c =>
        {
            // приоритет — точное совпадение позиции по сетке
            Vector3Int cpos = RoundToInt(c.target_world_position != Vector3.zero ? c.target_world_position : c.transform.position);
            if (cpos == key) return true;
            // запасной вариант — совпадение имени
            return !string.IsNullOrEmpty(nameHint) && c.gameObject.name == nameHint;
        });

        if (target)
        {
            Debug.Log($"{G}[Machine/Damage] Destroy cell '{target.name}' at ~{key}{GE}");
            Destroy(target.gameObject);
            // при необходимости можно пересчитать габариты/массу здесь
        }
        else
        {
            Debug.Log($"{G}[Machine/Damage] Cell not found at ~{key} (hint='{nameHint}'){GE}");
        }
    }

    private static Vector3Int RoundToInt(Vector3 p) =>
        new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));

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
    /// Начинаем передачу blueprint по частям
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_StartBlueprintChunked(int totalChunks)
    {
        Debug.Log($"{TAG} Начинаем получение blueprint по частям: {totalChunks} чанков");
        
        // Сбрасываем состояние
        receivedChunks.Clear();
        expectedChunks = totalChunks;
        receivedChunksCount = 0;
        
        // Подготавливаем список для чанков
        for (int i = 0; i < totalChunks; i++)
        {
            receivedChunks.Add(null);
        }
    }

    /// <summary>
    /// Получаем один чанк blueprint
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_ReceiveBlueprintChunk(int chunkIndex, byte[] chunkData)
    {
        Debug.Log($"{TAG} Получен чанк {chunkIndex + 1}/{expectedChunks}");
        
        if (chunkIndex >= 0 && chunkIndex < receivedChunks.Count)
        {
            receivedChunks[chunkIndex] = chunkData;
            receivedChunksCount++;
            
            // Проверяем, получили ли все чанки
            if (receivedChunksCount >= expectedChunks)
            {
                AssembleBlueprintFromChunks();
            }
        }
        else
        {
            Debug.LogError($"{TAG} Неверный индекс чанка: {chunkIndex}");
        }
    }

    /// <summary>
    /// Собираем blueprint из полученных чанков
    /// </summary>
    private void AssembleBlueprintFromChunks()
    {
        Debug.Log($"{TAG} Собираем blueprint из {receivedChunksCount} чанков");
        
        try
        {
            // Объединяем все чанки
            var combinedData = new List<byte>();
            foreach (var chunk in receivedChunks)
            {
                if (chunk != null)
                {
                    combinedData.AddRange(chunk);
                }
            }
            
            var assembledData = combinedData.ToArray();
            Debug.Log($"{TAG} Собран blueprint размером {assembledData.Length} байт");
            
            // Обрабатываем собранный blueprint
            var bp = BlueprintCodec.FromBytes(assembledData);
            Debug.Log($"{TAG} Получен blueprint ({bp.cells.Count} клеток) → сборка...");

            BuildFromBlueprint(bp);
            RecalculateBounds();

            // запас: берём 2 * наибольший half-extent
            safeRadius = Mathf.Max(2f * BoundsMaxExtent, 1.0f);

            if (Object.HasInputAuthority)
                OnLocalMachineReady?.Invoke(this);

            Debug.Log($"{TAG} Сборка завершена. Extent={BoundsMaxExtent:F2}, safeRadius={safeRadius:F2}");

            // Сохраняем blueprint данные для отправки новым игрокам
            savedBlueprintData = assembledData;
        }
        catch (Exception e)
        {
            Debug.LogError($"{TAG} Ошибка при сборке blueprint из чанков: {e.Message}");
        }
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
                
                // Проверяем размер данных и отправляем соответствующим способом
                Debug.Log($"{TAG} Sending to new player: blueprint size {savedBlueprintData.Length} bytes");
                if (savedBlueprintData.Length > 512)
                {
                    Debug.LogWarning($"{TAG} Blueprint data size ({savedBlueprintData.Length} bytes) exceeds RPC limit. Using chunked transmission for new player.");
                    var bp = BlueprintCodec.FromBytes(savedBlueprintData);
                    var chunks = BlueprintCodec.SplitBlueprintForRPC(bp);
                    SendBlueprintChunkedToNewPlayer(newPlayer, chunks);
                }
                else
                {
                    RPC_SetBlueprint(savedBlueprintData);
                }
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

    /// <summary>
    /// Отправляет blueprint по частям новому игроку
    /// </summary>
    private void SendBlueprintChunkedToNewPlayer(PlayerRef newPlayer, List<byte[]> chunks)
    {
        Debug.Log($"{TAG} Отправляем blueprint по частям новому игроку {newPlayer}: {chunks.Count} чанков");
        
        // Начинаем передачу
        RPC_StartBlueprintChunked(chunks.Count);
        
        // Отправляем каждый чанк
        for (int i = 0; i < chunks.Count; i++)
        {
            RPC_ReceiveBlueprintChunk(i, chunks[i]);
        }
        
        Debug.Log($"{TAG} Blueprint отправлен новому игроку по частям успешно");
    }
} 

public partial class Machine : NetworkBehaviour
{
    [SerializeField] private string _hitVfxResourcePath = "VFX/Hit_Default";
 

    /// <summary>
    /// Клиент стрелявшего вызывает это на ЧУЖОЙ машине.
    /// Дойдёт на владельца (StateAuthority) и там выполнится.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestDamage(Vector3 worldPos, string nameHint, Vector3 hitPoint, Vector3 hitNormal)
    {
        Debug.Log($"{G}[Machine/Damage] RPC_RequestDamage from client: pos~{Round(worldPos)} hint='{nameHint}'{GE}");

        // На владельце применяем урон и рассылаем "всем"
        RPC_ApplyDamage(worldPos, nameHint, hitPoint, hitNormal);
    }

    /// <summary>
    /// Выполняется на всех: спавнит VFX и удаляет клетку локально.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_ApplyDamage(Vector3 worldPos, string nameHint, Vector3 hitPoint, Vector3 hitNormal)
    {
        // VFX
        var prefab = Resources.Load<GameObject>(_hitVfxResourcePath);
        if (prefab)
        {
            var rot = Quaternion.LookRotation(hitNormal.sqrMagnitude > 1e-6f ? hitNormal.normalized : Vector3.up);
            var fx  = GameObject.Instantiate(prefab, hitPoint, rot);
            GameObject.Destroy(fx, 3f);
            Debug.Log($"{G}[Machine/VFX] Spawn '{prefab.name}' at {Round(hitPoint)}{GE}");
        }
        else
        {
            Debug.Log($"{G}[Machine/VFX] Prefab not found Resources/{_hitVfxResourcePath}{GE}");
        }

        // Удаление клетки на каждом клиенте
        LocalDestroyCellAt(worldPos, nameHint);
    }

    /// <summary>Локально у каждого клиента удаляет одну «клетку».</summary>
    public void LocalDestroyCellAt(Vector3 worldPos, string nameHint)
    {
        Vector3Int key = new Vector3Int(
            Mathf.RoundToInt(worldPos.x),
            Mathf.RoundToInt(worldPos.y),
            Mathf.RoundToInt(worldPos.z)
        );

        var cells = GetComponentsInChildren<io_base>(true);

        io_base target = cells.FirstOrDefault(c =>
        {
            Vector3 p = (c.target_world_position != Vector3.zero)
                        ? c.target_world_position
                        : c.transform.position;

            Vector3Int cp = new Vector3Int(
                Mathf.RoundToInt(p.x),
                Mathf.RoundToInt(p.y),
                Mathf.RoundToInt(p.z)
            );

            if (cp == key) return true;
            return !string.IsNullOrEmpty(nameHint) && c.gameObject.name == nameHint;
        });

        if (target)
        {
            Debug.Log($"{G}[Machine/Damage] Destroy cell '{target.name}' at ~{key}{GE}");
            Destroy(target.gameObject);
            // TODO: пересчитать массу/центр/баунды/радиус при необходимости
        }
        else
        {
            Debug.Log($"{G}[Machine/Damage] Cell not found at ~{key} (hint='{nameHint}'){GE}");
        }
    }

    private static string Round(Vector3 v) =>
        $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
}
