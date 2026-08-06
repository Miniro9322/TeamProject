using System.Collections.Generic;
using UnityEngine;

public class HeroCreateManager : MonoBehaviour
{
    private List<List<int>> probabilities = new List<List<int>>()
    {
        new List<int>() { 99, 1, 0, 0 }, 
        new List<int>() { 90, 9, 1, 0 }, 
        new List<int>() { 80, 15, 5, 0 },
        new List<int>() { 70, 20, 9, 1 },
    };
}
