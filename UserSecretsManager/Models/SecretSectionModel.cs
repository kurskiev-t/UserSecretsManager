namespace UserSecretsManager.Models;

/// <summary>
/// Секция пользовательских секретов проекта
/// </summary>
public class SecretSectionModel
{
    /// <summary>
    /// Название секции
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Контент секции настройки
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Включена ли секция
    /// </summary>
    public bool IsSelected { get; set; }
}