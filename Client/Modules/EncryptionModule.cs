using System.Security.Cryptography;
using System.Text;

namespace Client.Modules;

/// <summary>
/// Модуль шифрования данных
/// </summary>
public static class EncryptionModule
{
    private static readonly byte[] Key = DeriveKeyFromPassword("SortingClient2025");
    private static readonly byte[] IV = new byte[16] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 
                                                       0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };

    /// <summary>
    /// Генерирует ключ из пароля
    /// </summary>
    private static byte[] DeriveKeyFromPassword(string password)
    {
        var salt = Encoding.UTF8.GetBytes("SortingClientSalt2025");
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 256 бит
    }

    /// <summary>
    /// Шифрует строку данных
    /// </summary>
    /// <param name="plainText">Текст для шифрования</param>
    /// <returns>Зашифрованная строка в Base64</returns>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка шифрования: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Расшифровывает строку данных
    /// </summary>
    /// <param name="cipherText">Зашифрованная строка в Base64</param>
    /// <returns>Расшифрованный текст</returns>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка расшифровки: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Шифрует данные для передачи на сервер
    /// </summary>
    /// <param name="data">Данные для шифрования</param>
    /// <returns>Зашифрованные данные в Base64</returns>
    public static string EncryptForTransmission(string data)
    {
        // Для передачи на сервер используем тот же алгоритм
        return Encrypt(data);
    }

    /// <summary>
    /// Расшифровывает данные, полученные с сервера
    /// </summary>
    /// <param name="encryptedData">Зашифрованные данные в Base64</param>
    /// <returns>Расшифрованные данные</returns>
    public static string DecryptFromTransmission(string encryptedData)
    {
        return Decrypt(encryptedData);
    }
}

