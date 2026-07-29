using UnityEngine;
using UnityEngine.InputSystem;

public class GameSpeedUI : MonoBehaviour
{
    [SerializeField] private Key timeIncreaseKey = Key.Tab;
    [SerializeField] private Key timeStopKey = Key.Space;
    private float beforeTimeSpeed = 1f;
    private Keyboard keyboard;

    private void Awake()
    {
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard[timeIncreaseKey].wasPressedThisFrame)
        {
            if(Time.timeScale < 0.5f)
            {
                OnHalfSpeed();
            }
            else if (Time.timeScale < 1f)
            {
                OnNormalSpeed();
            }
            else if (Time.timeScale < 2f)
            {
                OnDoubleSpeed();
            }
            else if (Time.timeScale < 3f)
            {
                OnTripleSpeed();
            }
        }

        if (keyboard[timeStopKey].wasPressedThisFrame)
        {
            if(Time.timeScale > 0f)
            {
                beforeTimeSpeed = Time.timeScale;
                OnZeroSpeed();
            }
            else
            {
                Time.timeScale = beforeTimeSpeed;
            }
        }
    }

    public void OnHalfSpeed()
    {
        Time.timeScale = 0.5f;
    }

    public void OnZeroSpeed()
    {
        Time.timeScale = 0f;
    }

    public void OnNormalSpeed()
    {
        Time.timeScale = 1f;
    }

    public void OnDoubleSpeed()
    {
        Time.timeScale = 2f;
    }

    public void OnTripleSpeed()
    {
        Time.timeScale = 3f;
    }
}
