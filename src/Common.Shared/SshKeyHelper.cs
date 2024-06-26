// -----------------------------------------------------------------------
// <copyright file="SshKeyHelper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared
{
    using System;
    using System.Security.Cryptography;

    public static class SshKeyHelper
    {
        public static (string publicKey, string privateKey) GenerateSshKeypair()
        {
            using var rsa = new RSACryptoServiceProvider(2048);
            RSAParameters privateKey = rsa.ExportParameters(true);
            RSAParameters publicKey = rsa.ExportParameters(false);
            return (ConvertPublicKeyToOpenSshFormat(publicKey), ConvertPublicKeyToOpenSshFormat(privateKey));
        }

        private static string ConvertPublicKeyToOpenSshFormat(RSAParameters publicKey)
        {
            var sshRsaPrefix = new byte[] { 0x00, 0x00, 0x00, 0x07, 0x73, 0x73, 0x68, 0x2D, 0x72, 0x73, 0x61 };
            var lengthOfModulus = GetLengthInBytes(publicKey.Modulus!.Length);
            var lengthOfExponent = GetLengthInBytes(publicKey.Exponent!.Length);
            var totalLength = sshRsaPrefix.Length + lengthOfModulus.Length + publicKey.Modulus.Length +
                              lengthOfExponent.Length + publicKey.Exponent.Length;
            var buffer = new byte[totalLength];
            Buffer.BlockCopy(sshRsaPrefix, 0, buffer, 0, sshRsaPrefix.Length);
            Buffer.BlockCopy(lengthOfModulus, 0, buffer, sshRsaPrefix.Length, lengthOfModulus.Length);
            Buffer.BlockCopy(publicKey.Modulus, 0, buffer, sshRsaPrefix.Length + lengthOfModulus.Length, publicKey.Modulus.Length);
            Buffer.BlockCopy(lengthOfExponent, 0, buffer, sshRsaPrefix.Length + lengthOfModulus.Length + publicKey.Modulus.Length, lengthOfExponent.Length);
            Buffer.BlockCopy(publicKey.Exponent, 0, buffer, sshRsaPrefix.Length + lengthOfModulus.Length + publicKey.Modulus.Length + lengthOfExponent.Length, publicKey.Exponent.Length);
            return "ssh-rsa " + Convert.ToBase64String(buffer);
        }

        private static byte[] GetLengthInBytes(int length)
        {
            var len = BitConverter.GetBytes(length);
            Array.Reverse(len); // Reverse the bytes to match the Big Endian ordering used in SSH.
            return len;
        }
    }
}