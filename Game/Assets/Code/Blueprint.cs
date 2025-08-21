using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression; // пусть остаётся
using System.Text;
using UnityEngine;

[Serializable] public class MachineBlueprint { public List<CellDataNet> cells = new(); }
[Serializable] public class CellDataNet { public int prefabIndex; public Vector3 position; public Quaternion rotation; public string name; }

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
