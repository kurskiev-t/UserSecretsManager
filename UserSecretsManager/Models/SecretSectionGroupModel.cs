using System.Collections.ObjectModel;

namespace UserSecretsManager.Models;

/// <summary>
/// Группа одинаковых секций пользовательских секретов проекта
/// </summary>
public class SecretSectionGroupModel
{
    /// <summary>
    /// Название секции
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Список вариантов для этой секции (например, DEV, LOCAL)
    /// </summary>
    public ObservableCollection<SecretSectionModel> Variants { get; set; } = new ObservableCollection<SecretSectionModel>();

    /// <summary>
    /// Выбранный вариант секции
    /// </summary>
    public SecretSectionModel SelectedVariant { get; set; }
}