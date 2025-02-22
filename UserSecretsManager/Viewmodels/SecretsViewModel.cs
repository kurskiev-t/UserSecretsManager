using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UserSecretsManager.Commands;
using UserSecretsManager.Models;
using System.Linq;
using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Windows;
using Microsoft.VisualStudio;
using System.Text.RegularExpressions;

namespace UserSettingsManager.ViewModels;

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
            if (File.Exists(secretsJsonPath))
            {
                string userSecretsId = Path.GetFileName(folder);
                var projectPath = FindProjectByUserSecretsId(solution, userSecretsId);

                if (projectPath != null)
                {
                    var project = new ProjectSecretModel { ProjectName = Path.GetFileNameWithoutExtension(projectPath) };
                    ParseSecretsJson(secretsJsonPath, project);
                    Projects.Add(project);
                }
            }
        }
    }

    private void ParseSecretsJson(string secretsJsonPath, ProjectSecretModel project)
    {
        try
        {
            var lines = File.ReadAllLines(secretsJsonPath);

            // Словарь для хранения секций и их вариантов
            var sectionVariants = new Dictionary<string, List<(string Value, bool IsCommented, string Comment)>>();
            string currentComment = null;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                // Проверяем комментарии
                if (trimmedLine.StartsWith("//"))
                {
                    currentComment = trimmedLine.Substring(2).Trim();
                    continue;
                }

                // Пропускаем пустые строки и не-JSON
                if (string.IsNullOrWhiteSpace(trimmedLine) || !trimmedLine.Contains(":")) continue;

                // Извлекаем ключ и значение
                var match = Regex.Match(trimmedLine, @"[""']?([^""':]+)[""']?\s*:\s*(.+?)\s*,?\s*$");
                if (!match.Success) continue;

                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value.Trim();
                bool isCommented = trimmedLine.StartsWith("//");

                if (isCommented)
                {
                    value = value.TrimStart('/'); // Убираем комментарии из значения
                }

                if (!sectionVariants.ContainsKey(key))
                {
                    sectionVariants[key] = new List<(string, bool, string)>();
                }

                sectionVariants[key].Add((value, isCommented, currentComment));
                currentComment = null; // Сбрасываем комментарий после использования
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
                                IsSelected = !v.IsCommented // Активен тот, что не закомментирован
                            }))
                    };
                    group.SelectedVariant = group.SectionVariants.FirstOrDefault(v => v.IsSelected) ?? group.SectionVariants.First();
                    return group;
                }));
        }
        catch (Exception ex)
        {
            OnShowMessage($"Ошибка при парсинге {secretsJsonPath}: {ex.Message}");
        }
    }

    private string? FindProjectByUserSecretsId(IVsSolution solution, string userSecretsId)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, Guid.Empty, out var enumerator);
        var projects = new IVsHierarchy[1];

        while (enumerator.Next(1, projects, out var fetched) == 0 && fetched == 1)
        {
            projects[0].GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ProjectDir, out var projectDir);
            var csprojFiles = Directory.GetFiles(projectDir.ToString(), "*.csproj", SearchOption.AllDirectories);

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
        // Закомментировать все остальные секции в проекте
        foreach (var secretSectionModel in tuple.secretSectionGroup.SectionVariants)
        {
            if (secretSectionModel == tuple.selectedSecretSection)
            {
                secretSectionModel.Value = secretSectionModel.Value.Replace("\\* ", "");
                secretSectionModel.Value = secretSectionModel.Value.Replace(" *\\", "");

                secretSectionModel.IsSelected = true;

                continue;
            }
            
            secretSectionModel.Value = $"\\* {secretSectionModel.Value} *\\";
            secretSectionModel.IsSelected = false;
        }

        tuple.secretSectionGroup.SelectedVariant = tuple.selectedSecretSection;

        // Сделать выбранную секцию активной
        //tuple.selectedSecretSection.IsSelected = true;

        // Для выбранной секции раскомментировать, остальные закомментировать
    }
}
