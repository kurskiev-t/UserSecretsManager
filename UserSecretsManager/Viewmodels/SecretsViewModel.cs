using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using UserSecretsManager.Commands;
using UserSecretsManager.Models;

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
                                IsSelected = true
                            },
                            new SecretSectionModel
                            {
                                SectionName = "connectionSettings",
                                Value = "some value2",
                                Description = "LOCAL"
                            }
                        }
                    }
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
                                IsSelected = true
                            },
                            new SecretSectionModel
                            {
                                SectionName = "connectionSettings",
                                Value = "some value2",
                                Description = "LOCAL"
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

    public void ScanUserSecrets()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // Получаем IVsSolution напрямую через Package
        var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
        if (solution == null)
        {
            OnShowMessage("Не удалось получить доступ к решению.");
            return;
        }

        // Путь к папке User Secrets
        string userSecretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets");

        if (!Directory.Exists(userSecretsPath))
        {
            OnShowMessage("Папка User Secrets не найдена.");
            return;
        }

        // Сканируем все папки с User Secrets
        var userSecretsFolders = Directory.GetDirectories(userSecretsPath);

        Projects.Clear();

        foreach (var folder in userSecretsFolders)
        {
            string secretsJsonPath = Path.Combine(folder, "secrets.json");

            if (!File.Exists(secretsJsonPath))
                continue;

            string userSecretsId = Path.GetFileName(folder);
            var projectPath = FindProjectByUserSecretsId(solution, userSecretsId);

            if (projectPath == null)
                continue;

            var project = new ProjectSecretModel
            {
                ProjectName = Path.GetFileNameWithoutExtension(projectPath),

                // Сохраняем путь
                UserSecretsJsonPath = secretsJsonPath
            };
            ParseSecretsJson(secretsJsonPath, project);
            Projects.Add(project);
        }
    }

    // Но и сами блоки секций, а тут они тупо пропускаются. Тем не менее не закомментированные секций спарсились и даже UI отобразил хоть что-то) А это уже круто.
    // Логичнее будет там просто по regex, который ты уже сообразил какой нужен искать строки, исключая "//". А потом смотреть если они начинаются с "//", значит это закоммментированная секция, иначе - активная.Короче, я это сделаю.
    // 
    // значит это закоммментированная секция, иначе - активная - ЗАДАТЬ IsSelected для RadioButton, то есть она должна быть активна на UI (!!!!)
    private void ParseSecretsJson(string secretsJsonPath, ProjectSecretModel project)
    {
        try
        {
            var userSecretsJsonLines = File.ReadAllLines(secretsJsonPath);

            // Словарь для хранения секций и их вариантов
            var sectionVariants = new Dictionary<string, List<(string Value, bool IsCommented, string Comment, string RawContent)>>();

            for (var i = 0; i < userSecretsJsonLines.Length; i++)
            {
                var line = userSecretsJsonLines[i];

                string trimmedLine = line.Trim();

                // Пропускаем пустые строки и не-JSON
                if (string.IsNullOrWhiteSpace(trimmedLine) || !trimmedLine.Contains(":"))
                    continue;

                // Извлекаем ключ и значение
                var match = Regex.Match(trimmedLine, @"^//\s*[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$|^[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$");
                if (!match.Success)
                    continue;

                bool isSectionCommented = trimmedLine.StartsWith("//");
                string sectionKey = isSectionCommented ? match.Groups[1].Value : match.Groups[3].Value;
                string sectionValue = isSectionCommented ? match.Groups[2].Value : match.Groups[4].Value;

                if (isSectionCommented)
                {
                    sectionValue = sectionValue.TrimStart('/'); // Убираем комментарии из значения
                }

                if (!sectionVariants.ContainsKey(sectionKey))
                {
                    sectionVariants[sectionKey] = new List<(string, bool, string, string)>();
                }

                var previousLine = userSecretsJsonLines[i - 1];

                sectionVariants[sectionKey].Add((sectionValue, isSectionCommented, TryParsePureComment(previousLine, out var comment) ? comment : null, line)!);
            }

            // Заполняем модели
            project.SectionGroups = new ObservableCollection<SecretSectionGroupModel>(
                sectionVariants.Select((kvp, index) =>
                {
                    var group = new SecretSectionGroupModel
                    {
                        SectionName = kvp.Key,
                        SectionVariants = new ObservableCollection<SecretSectionModel>(
                            kvp.Value.Select((v, variantIndex) => new SecretSectionModel
                            {
                                SectionName = kvp.Key,
                                Value = v.Value,
                                Description = v.Comment ?? $"{kvp.Key} variant {variantIndex + 1}",
                                IsSelected = !v.IsCommented, // Активен тот, что не закомментирован
                                RawContent = v.RawContent
                            }))
                    };
                    group.SelectedVariant = group.SectionVariants.FirstOrDefault(v => v.IsSelected);
                    return group;
                }));
        }
        catch (Exception ex)
        {
            OnShowMessage($"Ошибка при парсинге {secretsJsonPath}: {ex.Message}");
        }
    }

    private static bool TryParsePureComment(string line, out string comment)
    {
        string trimmedLine = line.TrimStart();

        // Если строка не начинается с "//", это точно не комментарий
        if (!trimmedLine.StartsWith("//"))
        {
            comment = string.Empty;
            return false;
        }

        // Убираем "//" и пробелы после него
        comment = trimmedLine.Substring(2).TrimStart();

        // Проверяем, что строка НЕ содержит JSON-структуру
        // Чистый комментарий не должен содержать двоеточие с кавычками или без
        return !Regex.IsMatch(comment, @"[""']?[^""':]+[""']?\s*:\s*.+");
    }

    private static string? FindProjectByUserSecretsId(IVsSolution solution, string userSecretsId)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, Guid.Empty, out var projectsEnumerator);
        var projects = new IVsHierarchy[1];

        while (projectsEnumerator.Next(1, projects, out var fetched) == 0 && fetched == 1)
        {
            projects[0].GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ProjectDir, out var projectDirectory);

            if(projectDirectory == null)
                continue;

            var csprojFiles = Directory.GetFiles(projectDirectory.ToString(), "*.csproj", SearchOption.AllDirectories);

            foreach (var csprojFile in csprojFiles)
            {
                string csprojContent = File.ReadAllText(csprojFile);
                if (csprojContent.Contains($"<UserSecretsId>{userSecretsId}</UserSecretsId>"))
                {
                    return csprojFile;
                }
            }
        }

        return null;
    }

    // TODO: получать группу секций с дубликатами с разными вариантами одной и той же секции для их переключения
    public void SwitchSelectedSection((SecretSectionGroupModel secretSectionGroup, SecretSectionModel selectedSecretSection) tuple)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (var secretSectionModel in tuple.secretSectionGroup.SectionVariants)
        {
            secretSectionModel.IsSelected = secretSectionModel == tuple.selectedSecretSection;
        }
        tuple.secretSectionGroup.SelectedVariant = tuple.selectedSecretSection;

        var project = Projects.FirstOrDefault(p => p.SectionGroups.Contains(tuple.secretSectionGroup));
        if (project != null)
        {
            UpdateSecretsJson(project, tuple.secretSectionGroup);
        }
    }

    private void UpdateSecretsJson(ProjectSecretModel project, SecretSectionGroupModel updatedGroup)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrEmpty(project.UserSecretsJsonPath) || !File.Exists(project.UserSecretsJsonPath))
        {
            OnShowMessage($"Файл секретов для {project.ProjectName} не найден.");
            return;
        }

        var lines = File.ReadAllLines(project.UserSecretsJsonPath).ToList();
        var updatedLines = new List<string>();

        foreach (var line in lines)
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                updatedLines.Add(line);
                continue;
            }

            if (TryParsePureComment(line, out _))
            {
                updatedLines.Add(line);
                continue;
            }

            var match = Regex.Match(trimmedLine, @"^//\s*[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$|^[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$");
            if (!match.Success)
            {
                updatedLines.Add(line);
                continue;
            }

            bool isCommented = trimmedLine.StartsWith("//");
            string key = isCommented ? match.Groups[1].Value : match.Groups[3].Value;
            string value = isCommented ? match.Groups[2].Value : match.Groups[4].Value;

            if (key == updatedGroup.SectionName)
            {
                var variant = updatedGroup.SectionVariants.FirstOrDefault(v => v.Value.Trim() == value.Trim());
                if (variant != null)
                {
                    string rawContent = variant.RawContent.TrimStart();
                    string newLine = variant.IsSelected
                        ? rawContent.StartsWith("//") ? rawContent.Substring(2).TrimStart() : rawContent
                        : rawContent.StartsWith("//") ? rawContent : $"// {rawContent}";
                    updatedLines.Add(newLine);
                    continue;
                }
            }

            updatedLines.Add(line);
        }

        try
        {
            File.WriteAllLines(project.UserSecretsJsonPath, updatedLines);
        }
        catch (Exception ex)
        {
            OnShowMessage($"Ошибка при записи в {project.UserSecretsJsonPath}: {ex.Message}");
        }
    }
}
