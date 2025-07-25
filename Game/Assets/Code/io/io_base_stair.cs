using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class io_base_stair : io_base
{ 
    protected override void Awake()
    {
        // Сначала вызываем базовый метод Awake()
        base.Awake();       
        
    }
}