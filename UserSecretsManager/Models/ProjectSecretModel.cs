using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UserSecretsManager.Models;

/// <summary>
/// Пользовательские секреты проекта
/// </summary>
public class ProjectSecretModel : INotifyPropertyChanged
{
    private string _projectName = string.Empty;
    private ObservableCollection<SecretSectionGroupModel> _sectionGroups = new ObservableCollection<SecretSectionGroupModel>();
    private string _userSecretsJsonPath = string.Empty;
    private bool _isVisible = true;

    /// <summary>
    /// Название проекта
    /// </summary>
    public string ProjectName
    {
        get => _projectName;
        set => SetField(ref _projectName, value);
    }

    /// <summary>
    /// Группы секций пользовательских секретов проекта, сгруппированные по названию секции
    /// </summary>
    public ObservableCollection<SecretSectionGroupModel> SectionGroups
    {
        get => _sectionGroups;
        set => SetField(ref _sectionGroups, value);
    }

    /// <summary>
    /// Путь к secrets.json этого проекта
    /// </summary>
    public string UserSecretsJsonPath
    {
        get => _userSecretsJsonPath;
        set => SetField(ref _userSecretsJsonPath, value);
    }

    /// <summary>
    /// Виден ли этот проект на гуи
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
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