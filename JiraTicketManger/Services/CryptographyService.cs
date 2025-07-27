using System;
using System.Security.Cryptography;
using System.Text;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio di crittografia sicura usando DPAPI (Data Protection API) di Windows.
    /// Compatibile con il sistema VB.NET esistente mantenendo la stessa entropy string.
    /// </summary>
    public class CryptographyService
    {
        private readonly LoggingService _logger;

        // Stessa entropy string del VB.NET per compatibilità
        private const string ENTROPY_STRING = "JiraTicketManager_SecureConfig_2024";

        public CryptographyService()
        {
            _logger = LoggingService.CreateForComponent("CryptographyService");
        }

        /// <summary>
        /// Cripta una stringa usando DPAPI (Data Protection API) di Windows.
        /// I dati sono legati all'utente corrente e alla macchina.
        /// </summary>
        /// <param name="plainText">Testo da crittare</param>
        /// <returns>Stringa criptata in formato Base64</returns>
        public string EncryptString(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                {
                    _logger.LogDebug("EncryptString: Input vuoto, ritorno stringa vuota");
                    return "";
                }

                _logger.LogDebug($"EncryptString: Input = '{plainText.Substring(0, Math.Min(10, plainText.Length))}...' (length: {plainText.Length})");

                // Converti in byte array
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] entropyBytes = Encoding.UTF8.GetBytes(ENTROPY_STRING);

                _logger.LogDebug($"EncryptString: plainBytes length = {plainBytes.Length}");
                _logger.LogDebug($"EncryptString: entropyBytes length = {entropyBytes.Length}");

                // Cripta usando DPAPI legato all'utente corrente
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    entropyBytes,
                    DataProtectionScope.CurrentUser
                );

                _logger.LogDebug($"EncryptString: encryptedBytes length = {encryptedBytes.Length}");

                // Converti in Base64 per salvare nel config JSON
                string result = Convert.ToBase64String(encryptedBytes);

                _logger.LogInfo($"EncryptString: Crittografia completata (input: {plainText.Length}, output: {result.Length})");
                _logger.LogDebug($"EncryptString: Output = '{result.Substring(0, Math.Min(20, result.Length))}...'");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("EncryptString", ex);
                throw;
            }
        }

        /// <summary>
        /// Decripta una stringa precedentemente criptata con EncryptString.
        /// Gestisce automaticamente fallback per compatibilità con dati non criptati.
        /// </summary>
        /// <param name="encryptedText">Stringa criptata in formato Base64</param>
        /// <returns>Testo in chiaro</returns>
        public string DecryptString(string encryptedText)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                {
                    _logger.LogDebug("DecryptString: Input vuoto, ritorno stringa vuota");
                    return "";
                }

                _logger.LogDebug($"DecryptString: Inizio decrittografia (lunghezza input: {encryptedText.Length})");

                // Converti da Base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] entropyBytes = Encoding.UTF8.GetBytes(ENTROPY_STRING);

                // Decripta usando DPAPI
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    entropyBytes,
                    DataProtectionScope.CurrentUser
                );

                // Converti in stringa
                string result = Encoding.UTF8.GetString(decryptedBytes);

                _logger.LogInfo("DecryptString: Decrittografia completata con successo");
                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning($"DecryptString: Errore decrittografia DPAPI - {ex.Message}");
                // Se la decrittografia fallisce, probabilmente i dati sono corrotti o non criptati
                return "";
            }
            catch (FormatException ex)
            {
                _logger.LogWarning($"DecryptString: Formato Base64 non valido - {ex.Message}");
                // Se non è Base64 valido, probabilmente è testo in chiaro (compatibilità)
                _logger.LogInfo("DecryptString: Fallback su testo in chiaro per compatibilità");
                return encryptedText;
            }
            catch (Exception ex)
            {
                _logger.LogError("DecryptString - Exception", ex);
                throw new InvalidOperationException($"Errore imprevisto durante la decrittografia: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifica se una stringa è già criptata (formato Base64 valido con pattern DPAPI).
        /// Utile per determinare se una credenziale necessita di crittografia o è già criptata.
        /// </summary>
        /// <param name="text">Testo da verificare</param>
        /// <returns>True se il testo appare criptato</returns>
        public bool IsEncrypted(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    return false;
                }

                // Se è troppo corto, probabilmente non è criptato
                if (text.Length < 50)
                {
                    _logger.LogDebug($"IsEncrypted: Testo troppo corto ({text.Length} caratteri)");
                    return false;
                }

                // Verifica pattern tipico DPAPI (inizia spesso con "AQAAANC" o simile)
                if (!text.StartsWith("AQAAANC") && !text.StartsWith("AQAAAD") && !text.StartsWith("AQAAAN"))
                {
                    _logger.LogDebug("IsEncrypted: Pattern DPAPI non riconosciuto");
                    return false;
                }

                // Verifica se è Base64 valido e se la decrittografia funziona
                byte[] bytes = Convert.FromBase64String(text);
                byte[] entropyBytes = Encoding.UTF8.GetBytes(ENTROPY_STRING);

                // Prova a decrittare senza eccezioni
                byte[] decrypted = ProtectedData.Unprotect(bytes, entropyBytes, DataProtectionScope.CurrentUser);
                string decryptedText = Encoding.UTF8.GetString(decrypted);

                // Se la decrittografia funziona ed è ragionevole, è criptato
                bool isValid = !string.IsNullOrEmpty(decryptedText) && decryptedText.Length > 0 && decryptedText.Length < 1000;

                _logger.LogDebug($"IsEncrypted: Risultato = {isValid}");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"IsEncrypted: Eccezione durante verifica - {ex.Message}");
                // Se fallisce, probabilmente non è criptato
                return false;
            }
        }

        /// <summary>
        /// Cripta una stringa solo se non è già criptata.
        /// Utile per aggiornare configurazioni esistenti senza perdere dati.
        /// </summary>
        /// <param name="text">Testo da processare</param>
        /// <returns>Testo criptato</returns>
        public string EnsureEncrypted(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    return "";
                }

                if (IsEncrypted(text))
                {
                    _logger.LogDebug("EnsureEncrypted: Testo già criptato, ritorno invariato");
                    return text;
                }

                _logger.LogInfo("EnsureEncrypted: Crittografia testo in chiaro");
                return EncryptString(text);
            }
            catch (Exception ex)
            {
                _logger.LogError("EnsureEncrypted", ex);
                throw;
            }
        }

        /// <summary>
        /// Decripta una stringa solo se è criptata, altrimenti ritorna il testo invariato.
        /// Gestisce automaticamente la compatibilità con configurazioni miste.
        /// </summary>
        /// <param name="text">Testo da processare</param>
        /// <returns>Testo in chiaro</returns>
        public string EnsureDecrypted(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    return "";
                }

                if (!IsEncrypted(text))
                {
                    _logger.LogDebug("EnsureDecrypted: Testo già in chiaro, ritorno invariato");
                    return text;
                }

                _logger.LogInfo("EnsureDecrypted: Decrittografia testo criptato");
                return DecryptString(text);
            }
            catch (Exception ex)
            {
                _logger.LogError("EnsureDecrypted", ex);
                // In caso di errore, ritorna il testo originale per sicurezza
                return text;
            }
        }

        /// <summary>
        /// Factory method per creare istanze del servizio
        /// </summary>
        /// <returns>Nuova istanza di CryptographyService</returns>
        public static CryptographyService CreateDefault()
        {
            return new CryptographyService();
        }
    }
}