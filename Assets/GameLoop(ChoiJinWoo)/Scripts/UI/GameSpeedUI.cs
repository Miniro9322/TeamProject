using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameSpeedUI : MonoBehaviour
{
    [SerializeField] private Key timeIncreaseKey = Key.Tab;
    [SerializeField] private Key timeStopKey = Key.Space;
    [SerializeField] private Image halfImage;
    [SerializeField] private Image zeroImage;
    [SerializeField] private Image oneImage;
    [SerializeField] private Image twoImage;
    [SerializeField] private Image threeImage;
    private Speed beforeTimeSpeed = Speed.Normal;
    private InputAction timeIncreaseAction;
    private InputAction timeStopAction;

    private Dictionary<Speed, Image> images;

    private bool initialized;

    private Speed gameSpeed;
    private Speed GameSpeed
    {
        get
        {
            return gameSpeed;
        }

        set
        {
            initialized = true;
            switch (value)
            {
                case Speed.Zero:
                    Time.timeScale = 0f;
                    gameSpeed = Speed.Zero;
                    ChangeImage(value);
                    break;
                case Speed.Half:
                    Time.timeScale = 0.5f;
                    gameSpeed = Speed.Half;
                    ChangeImage(value);
                    break;
                case Speed.Normal:
                    if(Time.timeScale != 1f)
                        Time.timeScale = 1f;
                    gameSpeed = Speed.Normal;
                    ChangeImage(value);
                    break;
                case Speed.Double:
                    Time.timeScale = 2f;
                    gameSpeed = Speed.Double;
                    ChangeImage(value);
                    break;
                case Speed.Triple:
                    Time.timeScale = 3f;
                    gameSpeed = Speed.Triple;
                    ChangeImage(value);
                    break;
                default:
                    Debug.LogWarning($"{value}는 없는 값입니다.");
                    gameSpeed = Speed.Error;
                    break;
            }
        }
    }

    private void Awake()
    {
        images = new()
        {
            { Speed.Zero, zeroImage },
            { Speed.Half, halfImage },
            { Speed.Normal, oneImage },
            { Speed.Double, twoImage },
            { Speed.Triple, threeImage },
        };

        if (!initialized) GameSpeed = Speed.Normal;

        timeIncreaseAction = new InputAction("GameSpeedIncrease", binding: Keyboard.current[timeIncreaseKey].path);
        timeIncreaseAction.performed += OnTimeIncreasePerformed;

        timeStopAction = new InputAction("GameSpeedStop", binding: Keyboard.current[timeStopKey].path);
        timeStopAction.performed += OnTimeStopPerformed;
    }

    private void OnEnable()
    {
        GameSpeed = gameSpeed;
        timeIncreaseAction.Enable();
        timeStopAction.Enable();
    }

    private void OnDisable()
    {
        timeIncreaseAction.Disable();
        timeStopAction.Disable();
    }

    private void OnDestroy()
    {
        timeIncreaseAction.performed -= OnTimeIncreasePerformed;
        timeIncreaseAction.Dispose();

        timeStopAction.performed -= OnTimeStopPerformed;
        timeStopAction.Dispose();
    }

    private void OnTimeIncreasePerformed(InputAction.CallbackContext context)
    {
        if (SpawnerManager.Instance.isDirecting || TutorialInputGate.BlockHotkeys) return;

        if (Time.timeScale < 0.5f)
        {
            GameSpeed = Speed.Half;
        }
        else if (Time.timeScale < 1f)
        {
            GameSpeed = Speed.Normal;
        }
        else if (Time.timeScale < 2f)
        {
            GameSpeed = Speed.Double;
        }
        else if (Time.timeScale < 3f)
        {
            GameSpeed = Speed.Triple;
        }
    }

    private void OnTimeStopPerformed(InputAction.CallbackContext context)
    {
        if (SpawnerManager.Instance.isDirecting || TutorialInputGate.BlockHotkeys) return;

        if (Time.timeScale > 0f)
        {
            beforeTimeSpeed = GameSpeed;
            GameSpeed = Speed.Zero;
        }
        else
        {
            GameSpeed = beforeTimeSpeed;
        }
    }

    public void OnButtonClick(int value)
    {
        GameSpeed = (Speed)value;
    }

    private void ChangeImage(Speed value)
    {
        images ??= new()
        {
            { Speed.Zero, zeroImage },
            { Speed.Half, halfImage },
            { Speed.Normal, oneImage },
            { Speed.Double, twoImage },
            { Speed.Triple, threeImage },
        };

        foreach (var image in images)
        {
            image.Value.color = image.Key == value
                ? new Color32(255, 255, 255, 255)
                : new Color32(130, 130, 130, 255);
        }
    }
}
