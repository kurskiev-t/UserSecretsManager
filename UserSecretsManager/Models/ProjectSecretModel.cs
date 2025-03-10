using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UserSecretsManager.UserSecrets;

namespace UserSecretsManager.Models
{
    /// <summary>
    /// Пользовательские секреты проекта
    /// </summary>
    public class ProjectSecretModel : INotifyPropertyChanged
    {
        private string? _userSecretsJsonBackupPath;

        /// <summary>
        /// Название проекта
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Группы секций пользовательских секретов проекта, сгруппированные по названию секции
        /// </summary>

        public ObservableCollection<SecretSectionGroupModel> SectionGroups { get; set; } = new ObservableCollection<SecretSectionGroupModel>();

        /// <summary>
        /// Путь к secrets.json этого проекта
        /// </summary>
        public string UserSecretsJsonPath { get; set; } = string.Empty;

        /// <summary>
        /// Путь до бэкапа, например, "secrets.json.bak"
        /// </summary>
        public string? UserSecretsJsonBackupPath
        {
            get => _userSecretsJsonBackupPath;
            set => SetField(ref _userSecretsJsonBackupPath, value);
        }

        public List<SecretSection> AllSections { get; set; } = new List<SecretSection>(); // Добавляем для всех секций

        #region Property Changed

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
