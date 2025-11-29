using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AttendenceManagementSystem.Helpers
{
    /// <summary>
    /// Generates cryptographically secure random passwords for new users.
    /// </summary>
    public static class PasswordGenerator
    {
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string DigitChars = "0123456789";
        private const string SpecialChars = "@#$%&*";
        private const string AllChars = UppercaseChars + LowercaseChars + DigitChars + SpecialChars;

        /// <summary>
        /// Generates a random password with specified length.
        /// Ensures at least one character from each category (uppercase, lowercase, digit, special).
        /// </summary>
        /// <param name="length">Password length (minimum 12)</param>
        /// <returns>Cryptographically secure random password</returns>
        public static string Generate(int length = 12)
        {
            if (length < 12)
                throw new ArgumentException("Password length must be at least 12 characters", nameof(length));

            using (var rng = RandomNumberGenerator.Create())
            {
                var password = new StringBuilder(length);

                // Ensure at least one character from each category
                password.Append(GetRandomChar(rng, UppercaseChars));
                password.Append(GetRandomChar(rng, LowercaseChars));
                password.Append(GetRandomChar(rng, DigitChars));
                password.Append(GetRandomChar(rng, SpecialChars));

                // Fill remaining characters randomly from all categories
                for (int i = 4; i < length; i++)
                {
                    password.Append(GetRandomChar(rng, AllChars));
                }

                // Shuffle the password to avoid predictable patterns
                return Shuffle(rng, password.ToString());
            }
        }

        /// <summary>
        /// Gets a random character from the specified character set.
        /// </summary>
        private static char GetRandomChar(RandomNumberGenerator rng, string charSet)
        {
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            int randomIndex = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % charSet.Length;
            return charSet[randomIndex];
        }

        /// <summary>
        /// Shuffles the characters in a string using Fisher-Yates algorithm.
        /// </summary>
        private static string Shuffle(RandomNumberGenerator rng, string input)
        {
            char[] array = input.ToCharArray();
            int n = array.Length;

            for (int i = n - 1; i > 0; i--)
            {
                byte[] randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                int j = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % (i + 1);

                // Swap
                char temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            return new string(array);
        }
    }
}
