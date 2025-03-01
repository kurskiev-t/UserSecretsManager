using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UserSecretsManager.UserSecrets;

namespace UserSecretsManager.Helpers;

public static class UserSecretsHelper
{
    // TODO: or take in the file itself, or a stream
    public static IEnumerable<SecretLine> GetUserSecretSectionsMyVersion(string userSecretsFilePath)
    {
        // using var stream = new MemoryStream();

        #region Previous attempt

        //using var streamReader = new StreamReader(userSecretsFilePath);

        //var jsonString = streamReader.ReadToEnd();

        //var result = JsonConvert.DeserializeObject(jsonString);

        //foreach (var VARIABLE in COLLECTION)
        //{
            
        //}

        //var test = GetLines(jsonString);

        #endregion

        var jsonLines = File.ReadAllLines(userSecretsFilePath);

        var json = File.ReadAllText(userSecretsFilePath);

        // TODO: try - catch -  если не парсится, вывести сообщение
        var jObject = JObject.Parse(json);

        var beforeStart = true;
        // var isPreviousSection

        var sections = new List<SecretSection>();

        for (var i = 0; i < jsonLines.Length; i++)
        {
            // TODO: раскомментировать файл; чистые комменты убрать
            // десериализовать в JDocument
            // как-то запомнить где были комменты или может в JDocument есть индексы строк

            #region Older

            //var jsonLine = jsonLines[i];

            //if (string.IsNullOrWhiteSpace(jsonLine) && beforeStart)
            //    continue;

            //if (jsonLine.Contains("{"))
            //{
            //    sections.Add(new SecretLine
            //    {
            //        RawContent = jsonLine,
            //        SectionLines = new List<string> { jsonLine },
            //        LineIndex =
            //    });

            //    continue;
            //}

            //if (TryParsePureComment(jsonLine, out var comment))
            //{
            //    // sections.Add();
            //}

            #endregion

            var jsonLine = jsonLines[i];

            var trimmedLine = jsonLine.TrimStart();

            var isCommentedLine = jsonLine.TrimStart().StartsWith("//");

            var sectionKeyMatch = Regex.Match(trimmedLine, "^\\s*\".+?\"\\s*:");

            if (sectionKeyMatch.Success)
            {
                // Определение блока { }
                if (jsonLine.Contains("{"))
                {
                    // начало блока { } тогда
                    // читать до последней "}"

                    var section = new SecretSection
                    {
                        IsActive = isCommentedLine,
                        StartingLineIndex = i,
                        // PrecedingComment = i > 0 ? // проверить является ли предыдущая секция комментом
                    };
                    
                    while (!jsonLine.Contains("}"))
                    {
                        i++;

                        // TODO: учесть внутренние строки с новым блоками { }, то есть мне нужна строка с последней закрывающейся круглой скобкой, чтоб это было цельной секцией

                        // TODO: добавить в секцию все строки, входящие в нее


                    }
                }
                else if (jsonLine.Contains("["))
                {
                    // начало блока [ ] тогда
                    // читать до последней "]"

                    // TODO: учесть внутренние строки с новым блоками [ ], то есть мне нужна строка с последней закрывающейся квадратной скобкой, чтоб это было цельной секцией
                    while (!jsonLine.Contains("]"))
                    {
                        i++;

                        // TODO: учесть внутренние строки с новым блоками { }, то есть мне нужна строка с последней закрывающейся круглой скобкой, чтоб это было цельной секцией


                    }
                }
                // Просто строка
                else
                {
                    sections.Add(new SecretSection
                    {
                        SectionLines = new()
                        {
                            new SecretLine
                            {
                                RawContent = jsonLine,
                                LineIndex = i,

                            }
                        }
                    });
                }

            }
        }

        return Enumerable.Empty<SecretLine>();
    }

    public static IEnumerable<SecretSection> GetUserSecretSections(string userSecretsFilePath)
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
                        Value = isCommentedLine && !trimmedLine.Contains(":") ? trimmedLine.Substring(2).Trim() : null
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
                Value = trimmedLine.StartsWith("//") && !trimmedLine.Contains(":") ? trimmedLine.Substring(2).Trim() : null
            });

            bracketCount += trimmedLine.Count(c => c == openBracket);
            bracketCount -= trimmedLine.Count(c => c == closeBracket);
        }

        if (bracketCount > 0)
        {
            throw new FormatException($"Unclosed block for section '{section.Key}' in {jsonLines[i]}");
        }
    }

    // Пример использования
    public static void TestParsing(string userSecretsFilePath)
    {
        var sections = GetUserSecretSections(userSecretsFilePath);
        foreach (var section in sections)
        {
            Console.WriteLine($"Section: {section.Key ?? "Comment"}, Active: {section.IsActive}, PureComment: {section.IsPureComment}, PrecedingComment: {section.PrecedingComment?.SectionLines[0].RawContent}");
            foreach (var line in section.SectionLines)
            {
                Console.WriteLine($"  Line {line.LineIndex}: Raw: {line.RawContent}, Trimmed: {line.TrimmedLine}, Value: {line.Value ?? "N/A"} (Start: {line.FirstCharIndex})");
            }
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
}