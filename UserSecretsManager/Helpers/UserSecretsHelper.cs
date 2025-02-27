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
    public static IEnumerable<SecretLine> GetUserSecretSections(string userSecretsFilePath)
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

    private static List<string> GetLines(string secretJson)
    {
        var result = new List<string>();

        var jsonSpan = secretJson.AsSpan();

        int start = 0;

        while (start < jsonSpan.Length)
        {
            int end = jsonSpan.Slice(start).IndexOf('\n');

            // Последняя строка
            if (end == -1)
                end = jsonSpan.Length - start;

            var line = jsonSpan.Slice(start, end);

            if (line.EndsWith("\r".AsSpan()))
                line = line.Slice(start, line.Length - 1);

            result.Add(line.ToString());

            // Пропускаем \n
            start += end + 1;
        }

        return result;
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