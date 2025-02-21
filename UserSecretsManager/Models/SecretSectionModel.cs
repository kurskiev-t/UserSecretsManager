namespace UserSecretsManager.Models;

/// <summary>
/// Секция пользовательских секретов проекта
/// </summary>
public class SecretSectionModel
{
    /// <summary>
    /// Название секции пользовательского секрета
    /// </summary>
    public string SectionName { get; set; } = string.Empty;

    /// <summary>
    /// Значение (контент) секции пользовательского секрета
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Описание секции пользовательского секрета
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Активна ли секция
    /// </summary>
    public bool IsSelected { get; set; }
}