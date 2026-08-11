using System;
using System.Collections.Generic;
using UnityEngine;

// 낮 동안 바람이 들어오는 외곽 면 전체에 안내 화살표를 표시합니다.
public class WindPreview : IDisposable
{
    private const int TextureSize = 32;

    private readonly MapBoard board;
    private readonly float arrowSize;
    private readonly float arrowHeight;
    private readonly Color arrowColor;
    private readonly Dictionary<Vector2Int, List<Tile>> faceByWind = new();
    private readonly List<GameObject> arrows = new();

    private GameObject arrowRoot;
    private Texture2D arrowTexture;
    private Sprite arrowSprite;

    public int VisibleCount { get; private set; }
    public int ArrowCount => arrows.Count;

    // 사막 보드의 네 외곽 면과 재사용할 화살표를 한 번 준비합니다.
    public WindPreview(MapBoard board, Transform parent, float size, float height, Color color)
    {
        this.board = board;
        arrowSize = size;
        arrowHeight = height;
        arrowColor = color;
        BuildFaces();
        BuildArrows(parent);
    }

    // 현재 바람이 들어오는 외곽 면 전체에 화살표를 표시합니다.
    public void Show(Vector2Int wind)
    {
        if (!faceByWind.TryGetValue(wind, out List<Tile> face))
        {
            throw new ArgumentOutOfRangeException(nameof(wind));
        }

        Hide();
        Vector3 worldDirection = ReadDirection(wind);
        Quaternion rotation = Quaternion.LookRotation(Vector3.up, worldDirection);
        for (int index = 0; index < face.Count; index++)
        {
            GameObject arrow = arrows[index];
            Tile tile = face[index];
            arrow.transform.SetPositionAndRotation(tile.WorldTop + Vector3.up * arrowHeight, rotation);
            arrow.SetActive(true);
        }

        VisibleCount = face.Count;
    }

    // 모든 안내 화살표를 숨깁니다.
    public void Hide()
    {
        for (int index = 0; index < arrows.Count; index++)
        {
            arrows[index].SetActive(false);
        }

        VisibleCount = 0;
    }

    // 생성한 안내 오브젝트와 공유 이미지를 정리합니다.
    public void Dispose()
    {
        DestroyObject(arrowRoot);
        DestroyObject(arrowSprite);
        DestroyObject(arrowTexture);
        arrows.Clear();
        faceByWind.Clear();
        VisibleCount = 0;
    }

    // 실제 PlayRect 바깥 한 줄에서 네 Special 면을 좌표 순서로 저장합니다.
    private void BuildFaces()
    {
        RectInt play = board.PlayRect;
        int west = play.xMin - 1;
        int east = play.xMax;
        int south = play.yMin - 1;
        int north = play.yMax;

        faceByWind[GridCalculator.Right] = ReadColumn(west, south, north);
        faceByWind[GridCalculator.Left] = ReadColumn(east, south, north);
        faceByWind[GridCalculator.Up] = ReadRow(south, west, east);
        faceByWind[GridCalculator.Down] = ReadRow(north, west, east);
    }

    // 한 열의 Special 타일을 아래에서 위 순서로 저장합니다.
    private List<Tile> ReadColumn(int column, int minRow, int maxRow)
    {
        List<Tile> face = new();
        for (int row = minRow; row <= maxRow; row++)
        {
            KeepSpecial(face, new Vector2Int(column, row));
        }

        return face;
    }

    // 한 행의 Special 타일을 왼쪽에서 오른쪽 순서로 저장합니다.
    private List<Tile> ReadRow(int row, int minColumn, int maxColumn)
    {
        List<Tile> face = new();
        for (int column = minColumn; column <= maxColumn; column++)
        {
            KeepSpecial(face, new Vector2Int(column, row));
        }

        return face;
    }

    // 실제로 존재하는 Special 타일만 현재 면에 추가합니다.
    private void KeepSpecial(List<Tile> face, Vector2Int cell)
    {
        if (board.TryGetCell(cell, out Tile tile) && tile.IsSpecial)
        {
            face.Add(tile);
        }
    }

    // 가장 긴 외곽 면 길이만큼 화살표 오브젝트를 만들어 재사용합니다.
    private void BuildArrows(Transform parent)
    {
        arrowRoot = new GameObject("WindPreview");
        arrowRoot.hideFlags = HideFlags.DontSave;
        arrowRoot.transform.SetParent(parent, false);
        arrowSprite = CreateSprite();

        int arrowCount = ReadMaxCount();
        for (int index = 0; index < arrowCount; index++)
        {
            GameObject arrow = new GameObject($"WindArrow_{index}");
            arrow.hideFlags = HideFlags.DontSave;
            arrow.transform.SetParent(arrowRoot.transform, false);
            arrow.transform.localScale = Vector3.one * arrowSize;
            SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = arrowSprite;
            renderer.color = arrowColor;
            renderer.sortingOrder = 20;
            arrow.SetActive(false);
            arrows.Add(arrow);
        }
    }

    // 네 외곽 면 중 가장 긴 타일 수를 조회합니다.
    private int ReadMaxCount()
    {
        int horizontal = Mathf.Max(
            faceByWind[GridCalculator.Right].Count,
            faceByWind[GridCalculator.Left].Count);
        int vertical = Mathf.Max(
            faceByWind[GridCalculator.Up].Count,
            faceByWind[GridCalculator.Down].Count);
        return Mathf.Max(horizontal, vertical);
    }

    // 논리 바람 벡터를 실제 보드의 월드 진행 방향으로 변환합니다.
    private Vector3 ReadDirection(Vector2Int wind)
    {
        Vector3 origin = board.CellPointToWorld(Vector2.zero);
        Vector3 target = board.CellPointToWorld(new Vector2(wind.x, wind.y));
        return (target - origin).normalized;
    }

    // 모든 화살표가 공유하는 단순한 위쪽 화살표 이미지를 만듭니다.
    private Sprite CreateSprite()
    {
        arrowTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        arrowTexture.hideFlags = HideFlags.DontSave;
        arrowTexture.filterMode = FilterMode.Point;
        arrowTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int row = 0; row < TextureSize; row++)
        {
            for (int column = 0; column < TextureSize; column++)
            {
                pixels[row * TextureSize + column] = ReadPixel(column, row);
            }
        }

        arrowTexture.SetPixels(pixels);
        arrowTexture.Apply();
        Rect rect = new Rect(0f, 0f, TextureSize, TextureSize);
        Sprite sprite = Sprite.Create(arrowTexture, rect, new Vector2(0.5f, 0.5f), TextureSize);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    // 한 픽셀이 화살표 몸통이나 머리 안에 있는지 계산합니다.
    private static Color ReadPixel(int column, int row)
    {
        bool shaft = column >= 13 && column <= 18 && row >= 3 && row <= 19;
        int halfWidth = 29 - row;
        bool headRow = row >= 15 && row <= 29;
        bool head = headRow && Mathf.Abs(column - 16) <= halfWidth;
        if (shaft || head)
        {
            return Color.white;
        }

        return Color.clear;
    }

    // Play Mode와 Editor 검사 환경에 맞게 생성 오브젝트를 제거합니다.
    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
            return;
        }

        UnityEngine.Object.DestroyImmediate(target);
    }
}
