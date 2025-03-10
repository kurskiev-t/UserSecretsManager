﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace UserSecretsManager.UserSecrets;

public class SecretSection
{
    /// <summary>
    /// Предшествующий секции комментарий (ссылка на секцию-комментарий)
    /// </summary>
    public SecretSection? PrecedingComment { get; set; }

    /// <summary>
    /// Активна ли секция (не закомментирована)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Является ли секция чистым комментарием
    /// </summary>
    public bool IsPureComment { get; set; }

    /// <summary>
    /// Номер начальной строки секции в файле
    /// </summary>
    public int StartingLineIndex { get; set; }

    /// <summary>
    /// Индекс первого символа секции в файле
    /// </summary>
    public int FirstCharIndex { get; set; }

    /// <summary>
    /// Индекс последнего символа секции в файле
    /// </summary>
    public int LastCharIndex => SectionLines.Last().LastCharIndex;

    /// <summary>
    /// Список строк, входящих в секцию
    /// </summary>
    public List<SecretLine> SectionLines { get; set; } = new List<SecretLine>();

    /// <summary>
    /// Ключ секции (например, "ConnectionStrings"), null для комментариев
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Идет ли эта секция сразу после предыдущей (связаны ли секции)
    /// </summary>
    public bool IsPreviousSectionConnected { get; set; }

    /// <summary>
    /// Контент секции в исходном виде (закомментированный или активный)
    /// </summary>
    public string RawContent => string.Join(Environment.NewLine, SectionLines.Select(l => l.RawContent));

    /// <summary>
    /// Значение (контент) секции
    /// </summary>
    public string Value => string.Join(Environment.NewLine, SectionLines.Select(l => l.Value));

    public override string ToString() => RawContent;
}