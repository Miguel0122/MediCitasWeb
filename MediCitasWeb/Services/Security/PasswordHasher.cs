using System;
using System.Security.Cryptography;

namespace MediCitasWeb.Services.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;   // 128 bits
        private const int KeySize = 32;    // 256 bits
        private const int Iterations = 10000;

        // ===============================
        // HASH PASSWORD
        // ===============================
        public static string Hash(string password)
        {
            using (var algorithm =
                new Rfc2898DeriveBytes(
                    password,
                    SaltSize,
                    Iterations,
                    HashAlgorithmName.SHA256))
            {
                byte[] salt = algorithm.Salt;
                byte[] key = algorithm.GetBytes(KeySize);

                byte[] result = new byte[SaltSize + KeySize];

                Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
                Buffer.BlockCopy(key, 0, result, SaltSize, KeySize);

                return Convert.ToBase64String(result);
            }
        }

        // ===============================
        // VERIFY PASSWORD
        // ===============================
        public static bool Verify(string password, string hash)
        {
            byte[] hashBytes = Convert.FromBase64String(hash);

            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            using (var algorithm =
                new Rfc2898DeriveBytes(
                    password,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256))
            {
                byte[] key = algorithm.GetBytes(KeySize);

                for (int i = 0; i < KeySize; i++)
                {
                    if (hashBytes[i + SaltSize] != key[i])
                        return false;
                }

                return true;
            }
        }
    }
}