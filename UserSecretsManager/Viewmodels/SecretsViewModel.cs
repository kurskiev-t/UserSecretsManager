using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UserSecretsManager.Commands;
using UserSecretsManager.Models;
using System.Linq;
using System;

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
                                Description = "DEV"
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
                                Description = "DEV"
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

    public ICommand ScanCommand { get; private set; }

    public ICommand SwitchSectionVariantCommand => new RelayCommand<(SecretSectionGroupModel SecretSectionGroup, SecretSectionModel SelectedSecretSection)>((groupWithSectionTuple) => SwitchSelectedSection(groupWithSectionTuple));

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

                continue;
            }
            
            secretSectionModel.Value = $"\\* {secretSectionModel.Value} *\\";
        }

        tuple.secretSectionGroup.SelectedVariant = tuple.selectedSecretSection;

        // Сделать выбранную секцию активной
        //tuple.selectedSecretSection.IsSelected = true;

        // Для выбранной секции раскомментировать, остальные закомментировать
    }
}
