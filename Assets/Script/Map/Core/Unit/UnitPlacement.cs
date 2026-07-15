// using UnityEngine;
// using System.Collections.Generic;
// using System;

// public class UnitPlacement : MonoBehaviour
// {
//     private class PlaceedUnit
//     {
//         public Tile tile;
//         public OccupantKind kind;
//         public int AttackRange;
//         public Action BreakHandler;

//     }

//     [SerializeField] private MapBoard _board;
//     [SerializeField] private float _placeYOffset = 0f;

//     private readonly Dictionary<GameObject, PlaceedUnit> _placedUnits = new();

//     public event Action<Tile> UnitPlaced;
//     public event Action<Tile> UnitRemoved;

//     public bool CanPlaceUnit(Tile tile, OccupantKind kind)
//     {
//         if (tile == null || !tile.IsEmpty)
//             return false;

//         // 추가적인 배치 조건을 여기에 구현할 수 있습니다.

//         TilePlacementRule.Result rule = TilePlacementRule.CanPlace(tile.State, kind);
//         return rule.Allowed;
//     }

//     public bool TryPlaceUnit(Tile tile, GameObject unitPrefab, OccupantKind kind, int attackRange, out string reason)
//     {
//         if(!CanPlaceUnit(tile, kind)) 
//         {
//             reason = "유닛을 배치할 수 없습니다.";
//             return false;
//         }


//         if(unitPrefab == null)
//         {
//             reason = "유닛 프리팹이 null입니다.";
//             return false;
//         }

//         GameObject unitInstance = Instantiate(unitPrefab, tile.transform.position + Vector3.up * _placeYOffset, Quaternion.identity);
//         unitInstance.transform.position = tile.WorldTop + Vector3.up * _placeYOffset;

//         BindBoard(unitInstance);
//         tile.PlaceUnit(unitInstance, kind, attackRange);
//         reason = null;
//         return true;
//     }

//     public void RemoveUnit(Tile tile)
//     {
//           if (tile == null || !tile.HasUnit)
//         {
//             return;
//         }

//         GameObject unit = tile.RemoveUnit(); // 유닛 정보를 타일에서 없앤다
//         _board.ClearRangeCover(unit);
//         UnbindDeath(unit);
//         _placedUnits.Remove(unit);

//         UnitRemoved?.Invoke(tile);
//         Destroy(unit);
//     }

//     public void RemoveAllUnits()
//     {
//          List<Tile> tiles = new();
//         foreach (UnitInfo info in _units.Values)
//         {
//             tiles.Add(info.Tile);
//         }

//         foreach (Tile tile in tiles)
//         {
//             RemoveUnit(tile);
//         }
//     }

//     public int GetAttackRange(GameObject unit)
//     {
//         if (_placedUnits.TryGetValue(unit, out PlaceedUnit placedUnit))
//         {
//             return placedUnit.AttackRange;
//         }
//         return 0; // 기본 공격 범위
//     }

//     private void OnUnitDied(GameObject unit)
//     {
//         if(_placedUnits.TryGetValue(unit,out PlaceedUnit placedUnit))
//         {
//             placedUnit.BreakHandler?.Invoke();
//             RemoveUnit(placedUnit.tile);
//         }
//     }

//     private void BindBoard(GameObject unit)
//     {
//         IPlaceAble placeable = unit.GetComponent<IPlaceAble>();
//         if (placeable != null)
//         {
//             placeable.SetBoard(_board);
//         }
//     }
//     private static int GetBlockMax(GameObject unit, OccupantKind kind)
//     {
//         if (kind != OccupantKind.MeleeHero)
//         {
//             return 0;
//         }

//         if (unit.GetComponent<Hero>() is Hero hero)
//         {
//             return hero.BlockCount;
//         }

//         return 0;
//     }

//     private void SetRangeCover(GameObject unit, Tile tile, OccupantKind kind, int attackRange)
//     {
//         if(kind == OccupantKind.Building) return;
//         _board.SetRangeCover(unit, tile.Coord, Mathf.Max(0, attackRange));
//     }

//     private void BindDeath(GameObject unit, UnitInfo info)
//     {
//         if(unit.GetComponent<IPlaceAble>() is IPlaceAble placeable)
//         {
//             placeable.OnBreak += () => OnUnitDied(unit);
//             placeable.OnResur += () => OnUnitResurrected(unit, info);
//         }
//     }

//     private void OnUnitResurrected(GameObject unit, UnitInfo info)
//     {
//         if(_placedUnits.TryGetValue(unit, out PlaceedUnit placedUnit))
//         {
//             SetRangeCover(unit, placedUnit.tile, placedUnit.kind, placedUnit.AttackRange);
//         }
//     }

//     private void UnbindDeath(GameObject unit)
//     {
//         if(unit.GetComponent<IPlaceAble>() is IPlaceAble placeable)
//         {
//             placeable.OnBreak -= () => OnUnitDied(unit);
//             placeable.OnResur -= () => OnUnitResurrected(unit, null);
//         }
//     }

 



    
// }
