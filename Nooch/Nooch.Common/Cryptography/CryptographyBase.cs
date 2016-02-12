
namespace Nooch.Common.Cryptography
{
    public abstract class CryptographyBase
    {
        /// <summary>
        /// Encrypts the specified plain string.
        /// </summary>
        /// <param name="plainString">The plain string.</param>
        /// <returns></returns>
        public abstract string Encrypt(string plainString, string cryptographyKey);

        /// <summary>
        /// Decrypts the specified encrypted string.
        /// </summary>
        /// <param name="encryptedString">The encrypted string.</param>
        /// <returns></returns>
        public abstract string Decrypt(string encryptedString, string cryptographyKey);
    }
}
