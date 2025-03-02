using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UserSecretsManager.Commands;
using UserSecretsManager.Helpers;
using UserSecretsManager.Models;
using UserSecretsManager.UserSecrets;

namespace UserSecretsManager.ViewModels;

public class SecretsViewModel : INotifyPropertyChanged
{
    private ObservableCollection<ProjectSecretModel> _projects;
    public ObservableCollection<ProjectSecretModel> Projects
    {
        get => _projects;
        set
        {
            if (_projects == value)
                return;

            _projects = value;
            OnPropertyChanged(nameof(Projects));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static readonly Regex SectionRegex = new Regex(@"^//\s*[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$|^[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$");

    private static readonly string UserSecretsFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft", "UserSecrets");

    #region Protected members

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

    #endregion

    // Для теста
    public SecretsViewModel()
    {
        // Заполнение коллекции проектов (заглушка для примера)
        Projects = new ObservableCollection<ProjectSecretModel>
        {
            new()
            {
                ProjectName = "Project1",
                SectionGroups = new ObservableCollection<SecretSectionGroupModel>
                {
                    new SecretSectionGroupModel
                    {
                        SectionName = "connectionSettings",
                        SectionVariants = new ObservableCollection<SecretSectionModel>
                        {
                            new SecretSectionModel
                            {
                                SectionName = "connectionSettings",
                                Value = "some value1",
                                Description = "DEV",
                                IsSelected = true,
                                Section = new SecretSection
                                {
                                    Key = "connectionSettings"
                                }
                            },
                            new SecretSectionModel
                            {
                                SectionName = "connectionSettings",
                                Value = "some value2",
                                Description = "LOCAL",
                                Section = new SecretSection
                                {
                                    Key = "connectionSettings"
                                }
                            }
                        }
                    },
                    new SecretSectionGroupModel
                    {
                        SectionName = "enableMigrations",
                        SectionVariants = new ObservableCollection<SecretSectionModel>
                        {
                            new SecretSectionModel
                            {
                                SectionName = "enableMigrations",
                                Value = "true",
                                Description = "DEV",
                                IsSelected = true,
                                Section = new SecretSection
                                {
                                    Key = "enableMigrations"
                                }
                            },
                            new SecretSectionModel
                            {
                                SectionName = "enableMigrations",
                                Value = "false",
                                Description = "LOCAL",
                                Section = new SecretSection
                                {
                                    Key = "enableMigrations"
                                }
                            }
                        }
                    },
                }
            },
            new()
            {
                ProjectName = "Project2",
                SectionGroups = new ObservableCollection<SecretSectionGroupModel>
                {
                    new SecretSectionGroupModel
                    {
                        SectionName = "connectionSettings",
                        SectionVariants = new ObservableCollection<SecretSectionModel>
                        {
                            new SecretSectionModel
                            {
                                SectionName = "connectionSettings",
                                Value = "some value1",
                                Description = "DEV",
                                IsSelected = true,
                                Section = new SecretSection
                                {
                                    Key = "connectionSettings"
                                }
                            },
                            new SecretSectionModel
                            {
                                SectionName = "connectionSettings",
                                Value = "some value2",
                                Description = "LOCAL",
                                Section = new SecretSection
                                {
                                    Key = "connectionSettings"
                                }
                            }
                        }
                    }
                }
            }
        };

        foreach (ProjectSecretModel projectSecretModel in Projects)
        {
            foreach (SecretSectionGroupModel secretSectionGroupModel in projectSecretModel.SectionGroups)
            {
                secretSectionGroupModel.SelectedVariant = secretSectionGroupModel.SectionVariants.First();
            }
        }
    }

    public event EventHandler<string> ShowMessage;

    protected virtual void OnShowMessage(string message)
    {
        ShowMessage?.Invoke(this, message);
    }

    public ICommand ScanUserSecretsCommand => new RelayCommand(() => Application.Current.Dispatcher.Invoke(ScanUserSecrets));

    public ICommand SwitchSectionVariantCommand => new RelayCommand<(SecretSectionGroupModel SecretSectionGroup, SecretSectionModel SelectedSecretSection)>((groupWithSectionTuple) => SwitchSelectedSection(groupWithSectionTuple));

    public ICommand UpdateRawContentCommand => new RelayCommand<SecretSectionModel>(UpdateRawContent);

    public ICommand ShowSecretsFileCommand => new RelayCommand<ProjectSecretModel>(async (projectSecretModel) => await ShowSecretsFile(projectSecretModel));

    private async Task ShowSecretsFile(ProjectSecretModel project)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (string.IsNullOrEmpty(project.UserSecretsJsonPath) || !File.Exists(project.UserSecretsJsonPath))
        {
            OnShowMessage($"Файл секретов для {project.ProjectName} не найден.");
            return;
        }

        try
        {
            await VS.Documents.OpenAsync(project.UserSecretsJsonPath);
        }
        catch (Exception ex)
        {
            OnShowMessage($"Ошибка при открытии файла: {ex.Message}");
        }
    }

    public async Task ScanUserSecrets()
    {
        if (!Directory.Exists(UserSecretsFolderPath))
        {
            OnShowMessage("Папка User Secrets не найдена.");
            return;
        }

        // Сканируем все папки с User Secrets
        var userSecretsFolders = Directory.GetDirectories(UserSecretsFolderPath);

        Projects.Clear();

        foreach (var folder in userSecretsFolders)
        {
            string secretsJsonPath = Path.Combine(folder, "secrets.json");

            if (!File.Exists(secretsJsonPath))
                continue;

            string userSecretsId = Path.GetFileName(folder);
            var projectPath = await FindProjectByUserSecretsIdAsync(userSecretsId);

            if (projectPath == null)
                continue;

            var sections = UserSecretsHelper.GetUserSecretSections(secretsJsonPath);

            var project = UserSecretsHelper.BuildProjectModel(
                Path.GetFileNameWithoutExtension(projectPath),
                secretsJsonPath,
                sections
            );

            Projects.Add(project);
        }
    }

    private static async Task<string?> FindProjectByUserSecretsIdAsync(string userSecretsId)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var projects = await VS.Solutions.GetAllProjectsAsync();

        foreach (var project in projects)
        {
            string? csprojPath = project.FullPath;
            
            if (string.IsNullOrEmpty(csprojPath) || !File.Exists(csprojPath))
                continue;

            string csprojContent = File.ReadAllText(csprojPath);
            
            if (csprojContent.Contains($"<UserSecretsId>{userSecretsId}</UserSecretsId>"))
            {
                return csprojPath;
            }
        }

        return null;
    }

    // Флаг для программных изменений
    private bool _isProgrammaticUpdate = false;

    public void SwitchSelectedSection((SecretSectionGroupModel secretSectionGroup, SecretSectionModel selectedSecretSection) tuple)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _isProgrammaticUpdate = true;

        try
        {
            var project = Projects.First(p => p.SectionGroups.Contains(tuple.secretSectionGroup));
            var selectedDescription = tuple.selectedSecretSection.Description;

            var processedGroups = new HashSet<SecretSectionGroupModel>();
            var descriptionsSet = new HashSet<string?>(tuple.secretSectionGroup.SectionVariants.Select(x => x.Description));
            var groupsQueue = new Queue<SecretSectionGroupModel>();

            // Добавляем начальную группу
            processedGroups.Add(tuple.secretSectionGroup);
            groupsQueue.Enqueue(tuple.secretSectionGroup);

            while (groupsQueue.Any())
            {
                var currentGroup = groupsQueue.Dequeue();

                // Проверяем только непроверенные группы
                foreach (var group in project.SectionGroups.Except(processedGroups))
                {
                    var groupDescriptions = group.SectionVariants.Select(x => x.Description).ToHashSet();
                    if (groupDescriptions.Intersect(descriptionsSet).Any())
                    {
                        // Добавляем новые описания
                        descriptionsSet.UnionWith(groupDescriptions);
                        // Добавляем группу в очередь и отмечаем как обработанную
                        groupsQueue.Enqueue(group);
                        processedGroups.Add(group);
                    }
                }
            }

            // Обновляем все группы
            // Обновляем только связанные группы
            foreach (var secretSectionGroup in processedGroups)
            {
                foreach (var secretSectionModel in secretSectionGroup.SectionVariants)
                {
                    secretSectionModel.IsSelected = secretSectionModel.Description == selectedDescription;

                    if (secretSectionModel.IsSelected)
                        secretSectionGroup.SelectedVariant = secretSectionModel;

                    else if (secretSectionGroup.SelectedVariant == secretSectionModel)
                        secretSectionGroup.SelectedVariant = null; // Сбрасываем SelectedVariant только для связанных
                }
            }

            UpdateSecretsJson(project);
        }
        finally
        {
            _isProgrammaticUpdate = false;
        }
    }

    private bool _isUpdating = false;
    private void UpdateRawContent(SecretSectionModel sectionModel)
    {
        if (sectionModel == null || _isUpdating || _isProgrammaticUpdate)
            return;

        // Защита от рекурсии
        _isUpdating = true;

        try
        {
            var project =
                Projects.FirstOrDefault(p => p.SectionGroups.Any(g => g.SectionVariants.Contains(sectionModel)));

            if (project == null)
                return;
            
            UpdateSecretsJson(project, sectionModel);
        }
        finally
        {
            _isUpdating = false;
        }
    }
    
    // TODO: перевести на человеческий код; заморочка с отступами - может учесть их на стадии сканирования
    private void UpdateSecretsJson(ProjectSecretModel project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrEmpty(project.UserSecretsJsonPath) || !File.Exists(project.UserSecretsJsonPath))
        {
            OnShowMessage($"Файл секретов для {project.ProjectName} не найден.");
            return;
        }

        try
        {
            // Создаём словарь моделей секций из GUI по FirstCharIndex
            var variantActivity = new Dictionary<int, (bool IsSelected, string RawContent)>();
            foreach (var group in project.SectionGroups)
            {
                foreach (var variant in group.SectionVariants)
                {
                    if (variant.Section != null)
                    {
                        variantActivity[variant.Section.FirstCharIndex] = (variant.IsSelected, variant.RawContent);
                    }
                }
            }

            // Собираем строки из AllSections с учётом активности из GUI
            var lines = new List<string>();
            foreach (var section in project.AllSections)
            {
                var sectionLines = section.RawContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (var line in sectionLines)
                {
                    string trimmedLine = line.TrimStart();
                    int indent = line.Length - trimmedLine.Length;

                    if (variantActivity.TryGetValue(section.FirstCharIndex, out var activity))
                    {
                        // Это секция из GUI, обновляем её активность
                        if (activity.IsSelected && trimmedLine.StartsWith("//"))
                        {
                            lines.Add(new string(' ', indent) + trimmedLine.Substring(2)); // Раскомментируем
                        }
                        else if (!activity.IsSelected && !trimmedLine.StartsWith("//"))
                        {
                            lines.Add(new string(' ', indent) + "//" + trimmedLine); // Комментируем
                        }
                        else
                        {
                            lines.Add(line); // Оставляем как есть
                        }
                    }
                    else
                    {
                        // Промежуточные секции (комментарии, пустые строки, корень)
                        lines.Add(line);
                    }
                }
            }

            // Записываем файл
            File.WriteAllLines(project.UserSecretsJsonPath, lines);

            // Пересканируем файл для обновления моделей
            var sections = UserSecretsHelper.GetUserSecretSections(project.UserSecretsJsonPath);
            var updatedProject = UserSecretsHelper.BuildProjectModel(project.ProjectName, project.UserSecretsJsonPath, sections);
            Projects.Remove(project);
            Projects.Add(updatedProject);
        }
        catch (Exception ex)
        {
            OnShowMessage($"Ошибка при записи в {project.UserSecretsJsonPath}: {ex.Message}");
        }
    }

    private void UpdateSecretsJson(ProjectSecretModel project, SecretSectionModel secretSection)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrEmpty(project.UserSecretsJsonPath) || !File.Exists(project.UserSecretsJsonPath))
        {
            OnShowMessage($"Файл секретов для {project.ProjectName} не найден.");
            return;
        }

        try
        {
            // Находим индекс секции в AllSections по FirstCharIndex
            var sectionIndex = project.AllSections.FindIndex(s => s.FirstCharIndex == secretSection.Section.FirstCharIndex);
            if (sectionIndex == -1)
            {
                OnShowMessage("Секция не найдена в списке всех секций.");
                return;
            }

            // Проверяем, изменился ли ключ секции
            var originalSection = project.AllSections[sectionIndex];
            var originalKey = originalSection.Key;
            string? newKey = null;

            var match = Regex.Match(secretSection.RawContent.Trim(), @"^(?://)?\s*""([^""]+)""\s*:");
            if (match.Success)
            {
                newKey = match.Groups[1].Value;
            }

            bool keyChanged = originalKey != newKey;

            // Обновляем SectionLines в SecretSection на основе нового RawContent
            var newLines = secretSection.RawContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            originalSection.SectionLines.Clear();
            int currentCharIndex = originalSection.FirstCharIndex;
            for (int i = 0; i < newLines.Length; i++)
            {
                string line = newLines[i];
                string trimmedLine = line.TrimStart();
                bool isCommented = trimmedLine.StartsWith("//");
                originalSection.SectionLines.Add(new SecretLine
                {
                    RawContent = line,
                    TrimmedLine = trimmedLine,
                    LineIndex = originalSection.StartingLineIndex + i,
                    FirstCharIndex = currentCharIndex,
                    Value = isCommented ? trimmedLine.Substring(2).Trim() : trimmedLine.Trim()
                });
                currentCharIndex += line.Length + Environment.NewLine.Length;
            }
            originalSection.IsActive = secretSection.IsSelected;

            // Собираем строки из AllSections
            var lines = new List<string>();
            foreach (var section in project.AllSections)
            {
                var sectionLines = section.RawContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (var line in sectionLines)
                {
                    string trimmedLine = line.TrimStart();
                    if (section == originalSection)
                    {
                        if (secretSection.IsSelected && trimmedLine.StartsWith("//"))
                        {
                            lines.Add(line.Replace("//", "").TrimStart());
                        }
                        else if (!secretSection.IsSelected && !trimmedLine.StartsWith("//"))
                        {
                            lines.Add("//" + line);
                        }
                        else
                        {
                            lines.Add(line);
                        }
                    }
                    else
                    {
                        lines.Add(line); // Остальные секции как есть
                    }
                }
            }

            // Записываем файл
            File.WriteAllLines(project.UserSecretsJsonPath, lines);

            // Пересканируем только если ключ изменился
            if (keyChanged)
            {
                var sections = UserSecretsHelper.GetUserSecretSections(project.UserSecretsJsonPath);
                var updatedProject = UserSecretsHelper.BuildProjectModel(project.ProjectName, project.UserSecretsJsonPath, sections);
                Projects.Remove(project);
                Projects.Add(updatedProject);
            }
        }
        catch (Exception ex)
        {
            OnShowMessage($"Ошибка при записи в {project.UserSecretsJsonPath}: {ex.Message}");
        }
    }

    private static void UpdateSecretSectionRawContent(SecretSectionModel secretSectionModel)
    {
        string rawContent = secretSectionModel.RawContent.TrimStart();

        if (secretSectionModel.IsSelected)
        {
            secretSectionModel.RawContent = rawContent.StartsWith("//") ? rawContent.Substring(2).TrimStart() : rawContent;
        }
        else
        {
            secretSectionModel.RawContent = rawContent.StartsWith("//") ? rawContent : $"// {rawContent}";
        }
    }
}
