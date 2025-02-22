using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UserSecretsManager.Models;

/// <summary>
/// Секция пользовательских секретов проекта
/// </summary>
public class SecretSectionModel : INotifyPropertyChanged
{
    private string _sectionName = string.Empty;
    private string _value = string.Empty;
    private string? _description;
    private bool _isSelected;
    private string _rawContent = string.Empty;

    /// <summary>
    /// Название секции пользовательского секрета
    /// </summary>
    public string SectionName
    {
        get => _sectionName;
        set => SetField(ref _sectionName, value);
    }

    /// <summary>
    /// Значение (контент) секции пользовательского секрета
    /// </summary>
    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    /// <summary>
    /// Описание секции пользовательского секрета (комментарий перед ней, если есть)
    /// </summary>
    public string? Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    /// <summary>
    /// Активна ли секция
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>
    /// Контент секции пользовательского секрета в исходном виде (закомментированный или активный)
    /// </summary>
    public string RawContent
    {
        get => _rawContent;
        set => SetField(ref _rawContent, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}