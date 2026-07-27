using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameSpeedUI : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
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

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if(Time.timeScale > 0f)
            {
                OnZeroSpeed();
            }
            else
            {
                OnNormalSpeed();
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
