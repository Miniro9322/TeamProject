using UnityEngine;
using System;

/// <summary>
/// 테스트용 피격 컴포넌트. 타일 기반 전투가 IDamageAble로 직접 접근해 데미지를 준다.
/// </summary>
public class DummyDamageable : MonoBehaviour, IDamageAble
{
    [Header("Dummy Stats")]
    public float maxHp = 20f;
    public int defense = 0;

    [Header("Debug")]
    public bool logHits = true;

    private float _hp;
    private bool _initialized;

    public float Hp => _hp;
    public int Defense => defense;

    public event Action<DummyDamageable> Died;//

    private void Start() => Initialize();

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _hp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        if (!_initialized) Initialize();

        int hit = Mathf.Max(1, damage - defense);
        _hp -= hit;
        if (logHits) Debug.Log($"{name}{hit}, HP {_hp:0.#}/{maxHp:0.#}", this);
        if (_hp <= 0f) Die();
    }

    public void Die()
    {
        Died?.Invoke(this);
        Destroy(gameObject);
    }
}
