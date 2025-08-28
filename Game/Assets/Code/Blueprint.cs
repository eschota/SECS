using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression; // пусть остаётся
using System.Text;
using UnityEngine;

[Serializable] public class MachineBlueprint { public List<io_base_serialized> cells = new(); }

[Serializable]
public class io_base_serialized
{
    public string _prefab_name; // Имя префаба для новой системы
    public Vector3 _target_world_position;
    public Quaternion _target_world_rotation;
    public int _yaw_steps;
    public int _status;
    public string name;
    public string _cell_type; // Тип клетки для наследования
}

[Serializable]
public class io_engine_serialized : io_base_serialized
{
    public float force_power;
    public int force_type; // ForceMode как int (Force, Acceleration, Impulse, VelocityChange)
    public Vector3 force_vector_local;
    public float fuel_per_second;
    public float electricity_per_second;
}

public static class BlueprintCodec {
  public static byte[] ToBytes(MachineBlueprint bp) {
    var json = JsonUtility.ToJson(bp, false);
    var raw  = Encoding.UTF8.GetBytes(json);
    using var ms = new MemoryStream();
    using (var ds = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Optimal, true))
      ds.Write(raw, 0, raw.Length);
    return ms.ToArray();
  }

  public static MachineBlueprint FromBytes(byte[] data) {
    using var ms = new MemoryStream(data);
    using var ds = new DeflateStream(ms, CompressionMode.Decompress);
    using var outMs = new MemoryStream();
    ds.CopyTo(outMs);
    var json = Encoding.UTF8.GetString(outMs.ToArray());
    return JsonUtility.FromJson<MachineBlueprint>(json);
  }
  
  /// <summary>
  /// Оптимизированная сериализация для RPC (убирает лишние данные)
  /// </summary>
  public static byte[] ToBytesOptimized(MachineBlueprint bp) {
    // Создаем упрощенную версию blueprint только с необходимыми данными
    var optimizedBp = new MachineBlueprint();
    optimizedBp.cells = new List<io_base_serialized>();
    
    foreach (var cell in bp.cells)
    {
      var optimizedCell = new io_base_serialized
      {
        _prefab_name = cell._prefab_name,
        _target_world_position = cell._target_world_position,
        _target_world_rotation = cell._target_world_rotation,
        _yaw_steps = cell._yaw_steps,
        _status = cell._status,
        _cell_type = cell._cell_type
        // Убираем name для экономии места
      };
      
      // Добавляем специфичные данные для двигателей
      if (cell is io_engine_serialized engineCell)
      {
        var optimizedEngine = new io_engine_serialized
        {
          _prefab_name = engineCell._prefab_name,
          _target_world_position = engineCell._target_world_position,
          _target_world_rotation = engineCell._target_world_rotation,
          _yaw_steps = engineCell._yaw_steps,
          _status = engineCell._status,
          _cell_type = engineCell._cell_type,
          force_power = engineCell.force_power,
          force_type = engineCell.force_type,
          force_vector_local = engineCell.force_vector_local,
          fuel_per_second = engineCell.fuel_per_second,
          electricity_per_second = engineCell.electricity_per_second
        };
        optimizedBp.cells.Add(optimizedEngine);
      }
      else
      {
        optimizedBp.cells.Add(optimizedCell);
      }
    }
    
    var json = JsonUtility.ToJson(optimizedBp, false);
    var raw = Encoding.UTF8.GetBytes(json);
    
    // Используем максимальное сжатие
    using var ms = new MemoryStream();
    using (var ds = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Optimal, true))
      ds.Write(raw, 0, raw.Length);
    
    var compressed = ms.ToArray();
    Debug.Log($"Blueprint optimization: {raw.Length} -> {compressed.Length} bytes ({100f * compressed.Length / raw.Length:F1}%)");
    
    return compressed;
  }
  
  /// <summary>
  /// Разбивает большой blueprint на части для передачи через RPC
  /// </summary>
  public static List<byte[]> SplitBlueprintForRPC(MachineBlueprint bp, int maxChunkSize = 300)
  {
    // Сначала создаем полный оптимизированный blueprint
    var fullOptimizedBp = new MachineBlueprint();
    fullOptimizedBp.cells = new List<io_base_serialized>();
    
    foreach (var cell in bp.cells)
    {
      var optimizedCell = new io_base_serialized
      {
        _prefab_name = cell._prefab_name,
        _target_world_position = cell._target_world_position,
        _target_world_rotation = cell._target_world_rotation,
        _yaw_steps = cell._yaw_steps,
        _status = cell._status,
        _cell_type = cell._cell_type
      };
      
      // Добавляем специфичные данные для двигателей
      if (cell is io_engine_serialized engineCell)
      {
        var optimizedEngine = new io_engine_serialized
        {
          _prefab_name = engineCell._prefab_name,
          _target_world_position = engineCell._target_world_position,
          _target_world_rotation = engineCell._target_world_rotation,
          _yaw_steps = engineCell._yaw_steps,
          _status = engineCell._status,
          _cell_type = engineCell._cell_type,
          force_power = engineCell.force_power,
          force_type = engineCell.force_type,
          force_vector_local = engineCell.force_vector_local,
          fuel_per_second = engineCell.fuel_per_second,
          electricity_per_second = engineCell.electricity_per_second
        };
        fullOptimizedBp.cells.Add(optimizedEngine);
      }
      else
      {
        fullOptimizedBp.cells.Add(optimizedCell);
      }
    }
    
    // Сериализуем полный blueprint
    var fullJson = JsonUtility.ToJson(fullOptimizedBp, false);
    var fullRaw = Encoding.UTF8.GetBytes(fullJson);
    
    // Сжимаем полный blueprint
    using var fullMs = new MemoryStream();
    using (var fullDs = new DeflateStream(fullMs, System.IO.Compression.CompressionLevel.Optimal, true))
      fullDs.Write(fullRaw, 0, fullRaw.Length);
    
    var fullCompressed = fullMs.ToArray();
    
    // Разбиваем сжатые данные на чанки
    var chunks = new List<byte[]>();
    int offset = 0;
    
    while (offset < fullCompressed.Length)
    {
      int chunkSize = Math.Min(maxChunkSize, fullCompressed.Length - offset);
      var chunk = new byte[chunkSize];
      Array.Copy(fullCompressed, offset, chunk, 0, chunkSize);
      chunks.Add(chunk);
      offset += chunkSize;
    }
    
    Debug.Log($"Blueprint split into {chunks.Count} chunks for RPC transmission (total: {fullCompressed.Length} bytes)");
    
    // Проверяем, что все чанки в пределах лимита
    for (int i = 0; i < chunks.Count; i++)
    {
        if (chunks[i].Length > 512)
        {
            Debug.LogError($"Chunk {i} exceeds RPC limit: {chunks[i].Length} bytes > 512 bytes");
        }
    }
    
    return chunks;
  }
}
