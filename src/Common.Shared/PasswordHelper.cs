// -----------------------------------------------------------------------
// <copyright file="RNGExtensions.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public static class PasswordHelper
    {
        public static string GenerateRandomPassword(int length = 18)
        {
            const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string Digits = "1234567890";
            const string SpecialChars = "!@#$%^&*";
            const string AllChars = LowerCase + UpperCase + Digits + SpecialChars;

            if (length < 4)
            {
                throw new ArgumentException("Length must be at least 4", nameof(length));
            }

            var res = new StringBuilder();
            using (var rng = RandomNumberGenerator.Create())
            {
                var uintBuffer = new byte[sizeof(uint)];

                // Ensure the password includes at least one lower case, one upper case, one digit, and two special characters
                res.Append(LowerCase[rng.GetNonZeroRandomByte(uintBuffer, LowerCase.Length)]);
                res.Append(UpperCase[rng.GetNonZeroRandomByte(uintBuffer, UpperCase.Length)]);
                res.Append(Digits[rng.GetNonZeroRandomByte(uintBuffer, Digits.Length)]);
                res.Append(SpecialChars[rng.GetNonZeroRandomByte(uintBuffer, SpecialChars.Length)]);
                res.Append(SpecialChars[rng.GetNonZeroRandomByte(uintBuffer, SpecialChars.Length)]);

                // Generate the rest of the password
                for (var i = 5; i < length; i++)
                {
                    res.Append(AllChars[rng.GetNonZeroRandomByte(uintBuffer, AllChars.Length)]);
                }
            }

            // Mix up the result to ensure randomness
            res.Shuffle();
            return res.ToString();
        }

        private static byte GetNonZeroRandomByte(this RandomNumberGenerator rng, byte[] buffer, int maxValue)
        {
            var scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                rng.GetBytes(buffer);
                scale = BitConverter.ToUInt32(buffer, 0);
            }

            return (byte)(scale % maxValue);
        }

        private static void Shuffle(this StringBuilder builder)
        {
            using var rng = RandomNumberGenerator.Create();
            var n = builder.Length;
            while (n > 1)
            {
                var box = new byte[1];
                do
                {
                    rng.GetBytes(box);
                }
                while (!(box[0] < n * (byte.MaxValue / n)));

                var k = (box[0] % n);
                n--;
                (builder[k], builder[n]) = (builder[n], builder[k]);
            }
        }
    }
}