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
    using (var ds = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
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
}
