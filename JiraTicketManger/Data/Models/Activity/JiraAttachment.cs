using System;
using System.IO;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Services;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Data.Models.Activity
{
    /// <summary>
    /// Modello per gli allegati Jira.
    /// TRADUZIONE DIRETTA da JiraAttachment.vb con miglioramenti.
    /// </summary>
    public class JiraAttachment : ActivityItemBase
    {
        public string Filename { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }
        public string Content { get; set; }
        public string Thumbnail { get; set; }
        public string AuthorDisplayName { get; set; }
        public string AuthorEmailAddress { get; set; }

        /// <summary>
        /// Dimensione in formato leggibile (es. "1.5 MB")
        /// TRADUZIONE DIRETTA da JiraAttachment.vb HumanReadableSize
        /// </summary>
        public string HumanReadableSize
        {
            get
            {
                if (Size <= 0)
                    return "N/A";
                else if (Size < 1024)
                    return $"{Size} B";
                else if (Size < 1024 * 1024)
                    return $"{Math.Round(Size / 1024.0, 1)} KB";
                else if (Size < 1024 * 1024 * 1024)
                    return $"{Math.Round(Size / (1024.0 * 1024.0), 1)} MB";
                else
                    return $"{Math.Round(Size / (1024.0 * 1024.0 * 1024.0), 1)} GB";
            }
        }

        /// <summary>
        /// Estensione del file
        /// </summary>
        public string FileExtension => Path.GetExtension(Filename)?.ToLower() ?? "";

        /// <summary>
        /// Tipo di file categorizzato per UI
        /// </summary>
        public AttachmentFileType FileType
        {
            get
            {
                return FileExtension switch
                {
                    ".pdf" => AttachmentFileType.Pdf,
                    ".doc" or ".docx" => AttachmentFileType.Word,
                    ".xls" or ".xlsx" => AttachmentFileType.Excel,
                    ".ppt" or ".pptx" => AttachmentFileType.PowerPoint,
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => AttachmentFileType.Image,
                    ".zip" or ".rar" or ".7z" => AttachmentFileType.Archive,
                    ".txt" => AttachmentFileType.Text,
                    ".csv" => AttachmentFileType.Csv,
                    _ => AttachmentFileType.Other
                };
            }
        }

        /// <summary>
        /// Icona emoji per il tipo di file
        /// TRADUZIONE DIRETTA da VB.NET GetFileIcon()
        /// </summary>
        public string FileIcon
        {
            get
            {
                return FileType switch
                {
                    AttachmentFileType.Pdf => "📋",
                    AttachmentFileType.Word => "📝",
                    AttachmentFileType.Excel => "📊",
                    AttachmentFileType.PowerPoint => "📈",
                    AttachmentFileType.Image => "🖼️",
                    AttachmentFileType.Archive => "🗂️",
                    AttachmentFileType.Text => "📃",
                    AttachmentFileType.Csv => "📋",
                    _ => "📄"
                };
            }
        }

        /// <summary>
        /// Indica se il file può essere visualizzato in anteprima
        /// </summary>
        public bool CanPreview => FileType == AttachmentFileType.Image ||
                                 FileType == AttachmentFileType.Pdf ||
                                 FileType == AttachmentFileType.Text;

        /// <summary>
        /// Colore di sfondo per la card del file (per UI moderna)
        /// </summary>
        public string FileCardColor
        {
            get
            {
                return FileType switch
                {
                    AttachmentFileType.Pdf => "#dc3545",     // Rosso
                    AttachmentFileType.Word => "#0d6efd",    // Blu
                    AttachmentFileType.Excel => "#198754",   // Verde
                    AttachmentFileType.PowerPoint => "#fd7e14", // Arancione
                    AttachmentFileType.Image => "#6f42c1",   // Viola
                    AttachmentFileType.Archive => "#6c757d", // Grigio
                    _ => "#17a2b8"                           // Ciano
                };
            }
        }

        /// <summary>
        /// Crea un JiraAttachment da un JToken JSON.
        /// Utilizza le funzioni helper esistenti del progetto C#.
        /// </summary>
        public static JiraAttachment FromJToken(JToken attachmentToken)
        {
            try
            {
                var attachment = new JiraAttachment();

                // Proprietà base usando helper esistenti
                attachment.Id = JiraDataConverter.GetSafeStringValue(attachmentToken["id"]);
                attachment.Filename = JiraDataConverter.GetSafeStringValue(attachmentToken["filename"]);
                attachment.Content = JiraDataConverter.GetSafeStringValue(attachmentToken["content"]);
                attachment.Thumbnail = JiraDataConverter.GetSafeStringValue(attachmentToken["thumbnail"]);
                attachment.MimeType = JiraDataConverter.GetSafeStringValue(attachmentToken["mimeType"]);

                // Parsing dimensione con controllo errori
                var sizeString = JiraDataConverter.GetSafeStringValue(attachmentToken["size"]);
                if (long.TryParse(sizeString, out long size))
                    attachment.Size = size;

                // Data creazione con controllo errori
                var createdString = JiraDataConverter.GetSafeStringValue(attachmentToken["created"]);
                if (DateTime.TryParse(createdString, out DateTime created))
                    attachment.Created = created;

                // Autore usando helper esistenti
                var authorToken = attachmentToken["author"];
                if (authorToken != null)
                {
                    attachment.Author = JiraDataConverter.GetSafeStringValue(authorToken["name"]);
                    attachment.AuthorDisplayName = JiraDataConverter.GetSafeStringValue(authorToken["displayName"]);
                    attachment.AuthorEmailAddress = JiraDataConverter.GetSafeStringValue(authorToken["emailAddress"]);
                }

                return attachment;
            }
            catch (Exception ex)
            {
                var logger = LoggingService.CreateForComponent("JiraAttachment");
                logger.LogError($"Errore parsing allegato: {ex.Message}");
                throw;
            }
        }
    }
}
