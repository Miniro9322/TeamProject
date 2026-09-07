using UnityEngine;
using UnityEngine.InputSystem;

public class HardwareCursor : MonoBehaviour
{
    private const TextureFormat CursorTextureFormat = TextureFormat.RGBA32;

    [SerializeField] private Texture2D sourceTexture;
    [SerializeField] private Vector2 hotspot;
    [Range(8, 512)]
    [SerializeField] private int cursorSize = 64;
    [Range(0.5f, 1f)]
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float pressedOffsetY = 8f;

    private InputAction clickAction;
    private Texture2D normalTexture;
    private Texture2D pressedTexture;

    private void Awake()
    {
        clickAction = new InputAction(
            "CursorClick",
            InputActionType.Button,
            "<Mouse>/leftButton");

        RebuildCursorTextures();
    }

    private void OnEnable()
    {
        clickAction.performed += OnPressed;
        clickAction.canceled += OnReleased;
        clickAction.Enable();
        ApplyNormalCursor();
    }

    private void OnDisable()
    {
        clickAction.Disable();
        clickAction.performed -= OnPressed;
        clickAction.canceled -= OnReleased;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnDestroy()
    {
        clickAction.Dispose();
        DestroyCursorTexture(normalTexture);
        DestroyCursorTexture(pressedTexture);
    }

    private void OnValidate()
    {
        RebuildCursorTextures();

        if (Application.isPlaying)
        {
            ApplyNormalCursor();
        }
    }

    private void OnPressed(InputAction.CallbackContext context)
    {
        Cursor.SetCursor(pressedTexture, hotspot, CursorMode.Auto);
    }

    private void OnReleased(InputAction.CallbackContext context)
    {
        ApplyNormalCursor();
    }

    private void ApplyNormalCursor()
    {
        Cursor.SetCursor(normalTexture, hotspot, CursorMode.Auto);
    }

    private void RebuildCursorTextures()
    {
        if (sourceTexture == null)
        {
            return;
        }

        int targetWidth = Mathf.RoundToInt(sourceTexture.width * (float)cursorSize / sourceTexture.height);
        int targetHeight = cursorSize;

        DestroyCursorTexture(normalTexture);
        DestroyCursorTexture(pressedTexture);

        normalTexture = ResizeWithGpu(sourceTexture, targetWidth, targetHeight);
        pressedTexture = BuildPressedTexture(normalTexture, pressedScale, pressedOffsetY);
    }

    private Texture2D ResizeWithGpu(Texture2D source, int width, int height)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;

        Graphics.Blit(source, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D result = new Texture2D(width, height, CursorTextureFormat, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);

        return result;
    }

    private Texture2D BuildPressedTexture(Texture2D normal, float scale, float offsetY)
    {
        int width = normal.width;
        int normalHeight = normal.height;
        int scaledWidth = Mathf.RoundToInt(width * scale);
        int scaledHeight = Mathf.RoundToInt(normalHeight * scale);
        int offsetYPixels = Mathf.Max(0, Mathf.RoundToInt(offsetY));
        int pressedHeight = normalHeight + offsetYPixels;

        int offsetX = (width - scaledWidth) / 2;
        int startY = (normalHeight - scaledHeight) / 2;

        Texture2D shrunk = ResizeWithGpu(normal, scaledWidth, scaledHeight);
        Texture2D pressed = CompositeOnTransparentCanvas(shrunk, width, pressedHeight, offsetX, startY);

        DestroyCursorTexture(shrunk);
        return pressed;
    }

    private Texture2D CompositeOnTransparentCanvas(Texture2D source, int canvasWidth, int canvasHeight, int destX, int destY)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(canvasWidth, canvasHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;

        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        Graphics.CopyTexture(source, 0, 0, 0, 0, source.width, source.height, renderTexture, 0, 0, destX, destY);

        Texture2D result = new Texture2D(canvasWidth, canvasHeight, CursorTextureFormat, false);
        result.ReadPixels(new Rect(0, 0, canvasWidth, canvasHeight), 0, 0);
        result.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);

        return result;
    }

    private void DestroyCursorTexture(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(texture);
        }
        else
        {
            DestroyImmediate(texture);
        }
    }
}
