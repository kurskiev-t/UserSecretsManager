using System.Collections.Generic;

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
    public int StartIndex { get; set; }

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
}