using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EnemySoundDatabase", menuName = "Data/EnemySoundDatabase")]
public class EnemySoundDataBase : ScriptableObject
{
    public enum SoundType
    {
        Sfx,
        Bgm,
    }

    [System.Serializable]
    public class Entry
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public SoundType type = SoundType.Sfx;
        public bool loop = false;
        public float soundTime = 0;
    }

    public List<Entry> entries;

    private Dictionary<string, Entry> _lookup;

    public Entry Get(string key)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, Entry>();
            foreach (var e in entries)
                if (e != null && e.clip != null && !string.IsNullOrEmpty(e.key))
                    _lookup[e.key] = e;
        }
        return _lookup.TryGetValue(key, out var entry) ? entry : null;
    }
}
