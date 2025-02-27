namespace UserSecretsManager.UserSecrets;

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
    /// Индекс начала строки в файле (в символах)
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Номер строки в файле (начиная с 0)
    /// </summary>
    public int LineIndex { get; set; }
}