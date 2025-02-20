using System.Collections.ObjectModel;

namespace UserSecretsManager.Models
{
    /// <summary>
    /// Пользовательские секреты проекта
    /// </summary>
    public class ProjectSecretModel
    {
        /// <summary>
        /// Название проекта
        /// </summary>
        public string ProjectName { get; set; }

        // TODO: группировать по дубликатам
        /// <summary>
        /// Группы секций пользовательских секретов проекта, сгруппированные по названию секции
        /// </summary>

        public ObservableCollection<SecretSectionModel> Sections { get; set; } = new ObservableCollection<SecretSectionModel>();
    }
}
