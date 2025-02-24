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

    private static readonly Regex SectionRegex = new Regex(@"^//\s*[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$|^[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$");

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

    public ICommand UpdateRawContentCommand => new RelayCommand<SecretSectionModel>(UpdateRawContent);

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
    
    private void ParseSecretsJson(string secretsJsonPath, ProjectSecretModel project)
    {
        try
        {
            var userSecretsJsonLines = File.ReadAllLines(secretsJsonPath);
            var sectionVariants = new Dictionary<string, List<(string Value, bool IsCommented, string Comment, string RawContent)>>();
            string currentComment = null;

            for (var i = 0; i < userSecretsJsonLines.Length; i++)
            {
                var line = userSecretsJsonLines[i];
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                if (TryParsePureComment(line, out var comment))
                {
                    currentComment = comment;
                    continue;
                }

                var match = SectionRegex.Match(trimmedLine);
                if (!match.Success)
                    continue;

                bool isSectionCommented = trimmedLine.StartsWith("//");
                string sectionKey = isSectionCommented ? match.Groups[1].Value : match.Groups[3].Value;
                string sectionValue = isSectionCommented ? match.Groups[2].Value : match.Groups[4].Value;

                if (isSectionCommented)
                {
                    sectionValue = sectionValue.TrimStart('/');
                }

                if (!sectionVariants.ContainsKey(sectionKey))
                {
                    sectionVariants[sectionKey] = new List<(string, bool, string, string)>();
                }

                sectionVariants[sectionKey].Add((sectionValue, isSectionCommented, currentComment, line)!);
            }

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
                                IsSelected = !v.IsCommented,
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

            #region Grok Version



            //// Собираем все связанные группы через пересечение Description
            //var processedGroups = new HashSet<SecretSectionGroupModel>();
            //var descriptionsToProcess = new HashSet<string> { selectedDescription };
            //var allDescriptions = new HashSet<string>();

            //while (descriptionsToProcess.Any())
            //{
            //    var currentDescriptions = new HashSet<string>(descriptionsToProcess);
            //    descriptionsToProcess.Clear();

            //    foreach (var group in project.SectionGroups.Where(g => !processedGroups.Contains(g)))
            //    {
            //        var groupDescriptions = group.SectionVariants.Select(v => v.Description).ToHashSet();
            //        if (groupDescriptions.Intersect(currentDescriptions).Any())
            //        {
            //            processedGroups.Add(group);
            //            allDescriptions.UnionWith(groupDescriptions);
            //            descriptionsToProcess.UnionWith(groupDescriptions.Except(currentDescriptions));
            //        }
            //    }
            //}



            #endregion

            var processedGroups = new HashSet<SecretSectionGroupModel>();
            var descriptionsSet = new HashSet<string>(tuple.secretSectionGroup.SectionVariants.Select(x => x.Description));
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
    
    private void UpdateSecretsJson(ProjectSecretModel project)
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

            if (string.IsNullOrWhiteSpace(trimmedLine) || TryParsePureComment(line, out _))
            {
                updatedLines.Add(line);
                continue;
            }

            bool updated = false;
            foreach (var group in project.SectionGroups)
            {
                var variant = group.SectionVariants.FirstOrDefault(v => v.RawContent.Trim() == trimmedLine);

                if (variant != null)
                {
                    UpdateSecretSectionRawContent(variant);
                    updatedLines.Add(variant.RawContent);
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                updatedLines.Add(line);
            }
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

    private void UpdateSecretsJson(ProjectSecretModel project, SecretSectionModel secretSection)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (string.IsNullOrEmpty(project.UserSecretsJsonPath) || !File.Exists(project.UserSecretsJsonPath))
        {
            OnShowMessage($"Файл секретов для {project.ProjectName} не найден.");
            return;
        }

        var lines = File.ReadAllLines(project.UserSecretsJsonPath).ToList();
        string newSectionKey = null;

        for (var i = 0; i < lines.Count; i++)
        {
            var currentLine = lines[i];

            string trimmedLine = currentLine.Trim();

            if (secretSection.PreviousRawContent.Trim() != trimmedLine)
                continue;

            var match = SectionRegex.Match(secretSection.RawContent.Trim());

            if (match.Success)
            {
                bool isSectionCommented = trimmedLine.StartsWith("//");
                newSectionKey = isSectionCommented ? match.Groups[1].Value : match.Groups[3].Value;
            }
            
            lines[i] = secretSection.RawContent;
            break;
        }

        try
        {
            File.WriteAllLines(project.UserSecretsJsonPath, lines);

            if (secretSection.SectionName != newSectionKey)
            {
                // Пересканировать секреты пользователей, т. к. ключ секции поменялся
                ScanUserSecrets();
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
