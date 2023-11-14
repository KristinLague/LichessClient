using System;
using System.Text;
using System.Runtime.InteropServices;

namespace LichessClient.Models
{
    public class KeychainHelper
    {
        private const int k_errSecItemNotFound = -25300; 
        
        /// <summary>
        /// Uses platform invocation to add a token to the keychain on macOS.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="accountName"></param>
        /// <param name="token"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static void AddTokenToKeychain(string serviceName, string accountName, string token)
        {
            if (string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(accountName) || token == null)
            {
                throw new ArgumentException("Service name, account name, and password cannot be null or empty.");
            }

            var serviceNameBytes = Encoding.UTF8.GetBytes(serviceName);
            var accountNameBytes = Encoding.UTF8.GetBytes(accountName);
            var passwordBytes = Encoding.UTF8.GetBytes(token);

            var result = Keychain.SecKeychainAddGenericPassword(IntPtr.Zero, (uint)serviceNameBytes.Length, serviceNameBytes, (uint)accountNameBytes.Length, accountNameBytes, (uint)passwordBytes.Length, passwordBytes, out var itemRef);

            if (result != 0)
            {
                throw new Exception($"Error adding password to Keychain. Error code: {result}");
            }
        }

        /// <summary>
        /// Uses platform invocation to retrieve a token from the keychain on macOS.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="accountName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetTokenFromKeychain(string serviceName, string accountName)
        {
            var serviceNameBytes = Encoding.UTF8.GetBytes(serviceName);
            var accountNameBytes = Encoding.UTF8.GetBytes(accountName);

            int result = Keychain.SecKeychainFindGenericPassword(IntPtr.Zero, (uint)serviceNameBytes.Length, serviceNameBytes, (uint)accountNameBytes.Length, accountNameBytes, out uint passwordLength, out IntPtr passwordData, out IntPtr itemRef);

            if (result != 0)
            {
                //Elegantly exit null if the token has not yet been set
                if (result == k_errSecItemNotFound)
                {
                    return null;
                }

                throw new Exception($"Error finding password in Keychain. Error code: {result}");
            }

            byte[] passwordBytes = new byte[passwordLength];
            Marshal.Copy(passwordData, passwordBytes, 0, (int)passwordLength);
            Keychain.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);

            return Encoding.UTF8.GetString(passwordBytes);
        }



    }

    public static class Keychain
    {
        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        public static extern int SecKeychainAddGenericPassword(IntPtr keychain, uint serviceNameLength, byte[] serviceName, uint accountNameLength, byte[] accountName, uint passwordLength, byte[] passwordData, out IntPtr itemRef);

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        public static extern int SecKeychainFindGenericPassword(IntPtr keychain, uint serviceNameLength, byte[] serviceName, uint accountNameLength, byte[] accountName, out uint passwordLength, out IntPtr passwordData, out IntPtr itemRef);

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        public static extern int SecKeychainItemFreeContent(IntPtr itemContent, IntPtr passwordData);
    }

}

