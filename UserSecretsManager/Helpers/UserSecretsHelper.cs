using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UserSecretsManager.Models;
using UserSecretsManager.UserSecrets;

namespace UserSecretsManager.Helpers;

public static class UserSecretsHelper
{
    public static List<SecretSection> GetUserSecretSections(string userSecretsFilePath)
    {
        var jsonLines = File.ReadAllLines(userSecretsFilePath);
        var sections = new List<SecretSection>();
        int currentCharIndex = 0;

        for (int i = 0; i < jsonLines.Length; i++)
        {
            string jsonLine = jsonLines[i];
            string trimmedLine = jsonLine.TrimStart();
            bool isCommentedLine = trimmedLine.StartsWith("//");
            int lineFirstCharIndex = currentCharIndex;
            currentCharIndex += jsonLine.Length + Environment.NewLine.Length;

            var section = new SecretSection
            {
                IsActive = !isCommentedLine,
                StartingLineIndex = i,
                FirstCharIndex = lineFirstCharIndex,
                SectionLines = new List<SecretLine>
                {
                    new SecretLine
                    {
                        RawContent = jsonLine,
                        TrimmedLine = trimmedLine,
                        LineIndex = i,
                        FirstCharIndex = lineFirstCharIndex,
                        Value = isCommentedLine ? trimmedLine.Substring(2).Trim() : trimmedLine.Trim() // isCommentedLine && !trimmedLine.Contains(":") ? trimmedLine.Substring(2).Trim() : null
                    }
                }
            };

            // Определяем тип секции
            var sectionKeyMatch = Regex.Match(trimmedLine, @"^(?://)?\s*""([^""]+)""\s*:");
            if (sectionKeyMatch.Success)
            {
                section.Key = sectionKeyMatch.Groups[1].Value;

                // Блок с круглыми скобками {}
                if (trimmedLine.Contains("{"))
                {
                    ProcessBracketBlock(section, jsonLines, ref i, ref currentCharIndex, '{', '}');
                }
                // Блок с квадратными скобками []
                else if (trimmedLine.Contains("["))
                {
                    ProcessBracketBlock(section, jsonLines, ref i, ref currentCharIndex, '[', ']');
                }
            }
            // Чистый комментарий или пустая строка, или, например, начало файла "{"
            else
            {
                section.IsPureComment = isCommentedLine && !string.IsNullOrWhiteSpace(trimmedLine);
                section.Key = null;
            }

            // Устанавливаем PrecedingComment и IsPreviousSectionConnected
            if (sections.Count > 0)
            {
                var lastSection = sections[sections.Count - 1];
                if (lastSection.IsPureComment)
                {
                    section.PrecedingComment = lastSection;
                }
                else if (lastSection.Key != null && lastSection.StartingLineIndex + lastSection.SectionLines.Count == section.StartingLineIndex)
                {
                    section.IsPreviousSectionConnected = true;
                    if (lastSection.PrecedingComment != null)
                        section.PrecedingComment = lastSection.PrecedingComment;
                }
            }

            sections.Add(section);
        }

        return sections;
    }

    // Метод для обработки блоков {} и []
    private static void ProcessBracketBlock(SecretSection section, string[] jsonLines, ref int i, ref int currentCharIndex, char openBracket, char closeBracket)
    {
        int bracketCount = section.SectionLines[0].TrimmedLine.Count(c => c == openBracket);
        bracketCount -= section.SectionLines[0].TrimmedLine.Count(c => c == closeBracket);

        while (bracketCount > 0 && i + 1 < jsonLines.Length)
        {
            i++;
            string jsonLine = jsonLines[i];
            string trimmedLine = jsonLine.TrimStart();
            int lineFirstCharIndex = currentCharIndex;
            currentCharIndex += jsonLine.Length + Environment.NewLine.Length;

            // Проверка бардака
            if (!section.IsActive && !trimmedLine.StartsWith("//"))
            {
                throw new FormatException($"Inconsistent commenting in section '{section.Key}' at line {i}: block starts commented but contains uncommented lines.");
            } 

            section.SectionLines.Add(new SecretLine
            {
                RawContent = jsonLine,
                TrimmedLine = trimmedLine,
                LineIndex = i,
                FirstCharIndex = lineFirstCharIndex,
                Value = section.IsActive ? trimmedLine.Trim() : trimmedLine.Substring(2).Trim() // trimmedLine.StartsWith("//") && !trimmedLine.Contains(":") ? trimmedLine.Substring(2).Trim() : null
            });

            bracketCount += trimmedLine.Count(c => c == openBracket);
            bracketCount -= trimmedLine.Count(c => c == closeBracket);
        }

        if (bracketCount > 0)
        {
            throw new FormatException($"Unclosed block for section '{section.Key}' in {jsonLines[i]}");
        }
    }

    public static ProjectSecretModel BuildProjectModel(string projectFileName, string projectSecretsJsonPath, List<SecretSection> sections)
    {
        var project = new ProjectSecretModel
        {
            ProjectName = projectFileName,
            // Сохраняем путь
            UserSecretsJsonPath = projectSecretsJsonPath,
            AllSections = sections
        };

        var sectionsByKeys = sections
            .Where(section => section.Key != null)
            .GroupBy(section => section.Key)
            .ToDictionary(group => group.Key, group => group.ToList());

        //foreach (var section in sections.Where(x => x.Key != null))
        //{
        //    if (!sectionsByKeys.ContainsKey(section.Key!))
        //        sectionsByKeys[section.Key!] = new List<SecretSection>();

        //    sectionsByKeys[section.Key!].Add(section);
        //}

        project.SectionGroups = new ObservableCollection<SecretSectionGroupModel>(
            sectionsByKeys.Select((keyWithSections, index) =>
            {
                var group = new SecretSectionGroupModel
                {
                    SectionName = keyWithSections.Key,

                    SectionVariants = new ObservableCollection<SecretSectionModel>(
                        keyWithSections.Value.Select((section, variantIndex) => new SecretSectionModel
                        {
                            SectionName = keyWithSections.Key,
                            Value = section.Value,
                            Description = section.PrecedingComment?.Value ?? $"{keyWithSections.Key} variant {variantIndex + 1}",
                            IsSelected = section.IsActive,
                            RawContent = section.RawContent,
                            Section = section // Сохраняем ссылку
                        }))
                };

                group.SelectedVariant = group.SectionVariants.FirstOrDefault(v => v.IsSelected);
                return group;
            }));

        return project;
    }
}