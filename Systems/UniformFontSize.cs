using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UniformFontSize : MonoBehaviour
{
    [SerializeField] private float minFontSize = 8f;
    [SerializeField] private float maxFontSize = 72f;

    private readonly List<TMP_Text> _texts = new();
    private readonly List<TMP_Text> _allTexts = new();
    private bool _dirty;
    private int _lastActiveCount;
    private int _lastTextHash;

    private void OnEnable()
    {
        StartCoroutine(DelayedRefresh());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        Refresh();
    }

    private void OnRectTransformDimensionsChange()
    {
        _dirty = true;
    }

    private void LateUpdate()
    {
        CheckForChanges();
        if (!_dirty) return;
        _dirty = false;
        Refresh();
    }

    private void CheckForChanges()
    {
        _allTexts.Clear();
        GetComponentsInChildren(true, _allTexts);

        int activeCount = 0;
        int textHash = 17;
        foreach (var t in _allTexts)
        {
            if (t.enabled && t.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(t.text))
                activeCount++;
            textHash = textHash * 31 + (t.text != null ? t.text.Length : 0);
        }

        if (activeCount != _lastActiveCount || textHash != _lastTextHash)
        {
            _lastActiveCount = activeCount;
            _lastTextHash = textHash;
            _dirty = true;
        }
    }

    public void Refresh()
    {
        _texts.Clear();
        GetComponentsInChildren(false, _texts);
        _texts.RemoveAll(t => !t.enabled || string.IsNullOrWhiteSpace(t.text));

        if (_texts.Count == 0) return;

        foreach (var t in _texts)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = minFontSize;
            t.fontSizeMax = maxFontSize;
            t.ForceMeshUpdate();
        }

        float smallest = maxFontSize;
        foreach (var t in _texts)
        {
            if (t.fontSize < smallest)
                smallest = t.fontSize;
        }

        foreach (var t in _texts)
        {
            t.enableAutoSizing = false;
            t.fontSize = smallest;
        }
    }
}
