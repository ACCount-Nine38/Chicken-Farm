﻿using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public GameObject WorldItem;
    public string itemName;
    public bool stackable, canObserve;
    public int maxStack, currentStack;
    public int sellPrice;

    [HideInInspector]
    public float cookedMagnitude;
}
