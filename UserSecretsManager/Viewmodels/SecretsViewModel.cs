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
using System.Windows;
using System.Windows.Input;
using UserSecretsManager.Commands;
using UserSecretsManager.Models;

namespace UserSecretsManager.ViewModels
{
    public class SecretsViewModel : INotifyPropertyChanged
    {
        private readonly IVsSolution _vsSolution;
        private ObservableCollection<ProjectSecretModel> _projects;

        public ObservableCollection<ProjectSecretModel> Projects
        {
            get => _projects;
            set => SetField(ref _projects, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<string> ShowMessage;

        public SecretsViewModel(IVsSolution vsSolution)
        {
            _vsSolution = vsSolution ?? throw new ArgumentNullException(nameof(vsSolution));
            Projects = new ObservableCollection<ProjectSecretModel>();
            ScanUserSecretsCommand = new RelayCommand(() => Application.Current.Dispatcher.Invoke(ScanUserSecrets));
            SwitchSectionVariantCommand = new RelayCommand<(SecretSectionGroupModel, SecretSectionModel)>(SwitchSelectedSection);

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

        protected virtual void OnShowMessage(string message)
        {
            ShowMessage?.Invoke(this, message);
        }

        public ICommand ScanUserSecretsCommand { get; }

        public ICommand SwitchSectionVariantCommand { get; }

        public void ScanUserSecrets()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string userSecretsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets");
            if (!Directory.Exists(userSecretsPath))
            {
                OnShowMessage("Папка User Secrets не найдена.");
                return;
            }

            var userSecretsFolders = Directory.GetDirectories(userSecretsPath);
            Projects.Clear();

            foreach (var folder in userSecretsFolders)
            {
                string secretsJsonPath = Path.Combine(folder, "secrets.json");
                if (File.Exists(secretsJsonPath))
                {
                    string userSecretsId = Path.GetFileName(folder);
                    var projectPath = FindProjectByUserSecretsId(userSecretsId);

                    if (projectPath != null)
                    {
                        var project = new ProjectSecretModel { ProjectName = Path.GetFileNameWithoutExtension(projectPath) };
                        // Здесь можно добавить парсинг secrets.json и заполнение SectionGroups
                        Projects.Add(project);
                    }
                }
            }
        }

        private string? FindProjectByUserSecretsId(string userSecretsId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _vsSolution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, Guid.Empty, out var enumerator);
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

        public void SwitchSelectedSection((SecretSectionGroupModel secretSectionGroup, SecretSectionModel selectedSecretSection) tuple)
        {
            foreach (var secretSectionModel in tuple.secretSectionGroup.SectionVariants)
            {
                if (secretSectionModel == tuple.selectedSecretSection)
                {
                    secretSectionModel.Value = secretSectionModel.Value.Replace("\\* ", "").Replace(" *\\", "");
                    secretSectionModel.IsSelected = true;
                }
                else
                {
                    secretSectionModel.Value = $"\\* {secretSectionModel.Value} *\\";
                    secretSectionModel.IsSelected = false;
                }
            }
            tuple.secretSectionGroup.SelectedVariant = tuple.selectedSecretSection;
        }
    }
}