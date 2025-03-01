using System;

namespace UserSecretsManager.UserSecrets;

/// <summary>
/// Строка в файле пользовательских секретов
/// </summary>
public class SecretLine
{
    /// <summary>
    /// Контент строки в исходном виде
    /// </summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// Обрезанная строка (без начальных пробелов)
    /// </summary>
    public string TrimmedLine { get; set; } = string.Empty;

    /// <summary>
    /// Значение строки (для чистых комментариев — текст без //)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Индекс первого символа строки в файле
    /// </summary>
    public int FirstCharIndex { get; set; }

    /// <summary>
    /// Индекс последнего символа строки в файле
    /// </summary>
    public int LastCharIndex => FirstCharIndex + RawContent.Length + Environment.NewLine.Length - 1;

    /// <summary>
    /// Номер строки в файле (начиная с 0)
    /// </summary>
    public int LineIndex { get; set; }
}