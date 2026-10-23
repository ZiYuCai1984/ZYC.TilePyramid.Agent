using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using PropertyChanged;

namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

/// <summary>A dictionary of floating panel states with notifications for entries and their layout changes.</summary>
[DoNotNotify]
public class FloatingPanelStateCollection : IDictionary<string, FloatingPanelState>, INotifyPropertyChanged
{
    private Dictionary<string, FloatingPanelState> Items { get; } = new(StringComparer.Ordinal);

    /// <summary>Raised when an entry is added, replaced or removed, or a panel's layout changes.</summary>
    public event EventHandler? Changed;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public FloatingPanelState this[string key]
    {
        get => Items[key];
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (Items.TryGetValue(key, out var previous))
            {
                if (ReferenceEquals(previous, value))
                {
                    return;
                }

                Detach(previous);
            }

            Items[key] = value;
            Attach(value);
            OnEntriesChanged();
        }
    }

    /// <inheritdoc />
    public ICollection<string> Keys => Items.Keys;

    /// <inheritdoc />
    public ICollection<FloatingPanelState> Values => Items.Values;

    /// <inheritdoc />
    public int Count => Items.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(string key, FloatingPanelState value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Items.Add(key, value);
        Attach(value);
        OnEntriesChanged();
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<string, FloatingPanelState> item)
    {
        Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (!Items.Remove(key, out var panel))
        {
            return false;
        }

        Detach(panel);
        OnEntriesChanged();
        return true;
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, FloatingPanelState> item)
    {
        return Contains(item) && Remove(item.Key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (Count == 0)
        {
            return;
        }

        foreach (var panel in Values)
        {
            Detach(panel);
        }

        Items.Clear();
        OnEntriesChanged();
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        return Items.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out FloatingPanelState value)
    {
        return Items.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, FloatingPanelState> item)
    {
        return ((ICollection<KeyValuePair<string, FloatingPanelState>>)Items).Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, FloatingPanelState>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, FloatingPanelState>>)Items).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, FloatingPanelState>> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Creates an independent copy of every entry and its panel state.</summary>
    public FloatingPanelStateCollection Clone()
    {
        var panels = new FloatingPanelStateCollection();
        foreach (var (key, panel) in this)
        {
            panels.Add(key, panel.Clone());
        }

        return panels;
    }

    private void Attach(FloatingPanelState panel)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        ((INotifyPropertyChanged)panel).PropertyChanged += OnPanelChanged;
    }

    private void Detach(FloatingPanelState panel)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        ((INotifyPropertyChanged)panel).PropertyChanged -= OnPanelChanged;
    }

    private void OnPanelChanged(object? sender, PropertyChangedEventArgs e)
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEntriesChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Keys)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
