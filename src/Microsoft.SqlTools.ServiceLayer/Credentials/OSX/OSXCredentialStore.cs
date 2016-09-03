//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.OSX
{
    /// <summary>
    /// OSX implementation of the credential store
    /// </summary>
    internal class OSXCredentialStore : ICredentialStore
    {
        public bool DeletePassword(string credentialId)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            return DeletePasswordImpl(credentialId);
        }

        public bool TryGetPassword(string credentialId, out string password)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            return FindPassword(credentialId, out password);
        }

        public bool Save(Credential credential)
        {
            Credential.ValidateForSave(credential);
            bool result = false;

            // TODO: Consider updating password properties. OSX blocks AddPassword if the credential 
            // already exists, so for now we delete the password if already present since we're updating
            // the value. In the future, we could consider updating.
            DeletePasswordImpl(credential.CredentialId);

            // Now add the password
            result = AddGenericPassword(credential);
            return result;
        }

        private bool AddGenericPassword(Credential credential)
        {            
            IntPtr passwordPtr = Marshal.StringToCoTaskMemUni(credential.Password);
            Interop.Security.OSStatus status = Interop.Security.SecKeychainAddGenericPassword(
              IntPtr.Zero, 
              InteropUtils.GetLengthInBytes(credential.CredentialId), 
              credential.CredentialId,
              0, 
              null,
              InteropUtils.GetLengthInBytes(credential.Password),
              passwordPtr,
              IntPtr.Zero);

            return status == Interop.Security.OSStatus.ErrSecSuccess;
        }

        /// <summary>
        /// Finds the first password matching this credential
        /// </summary>
        private bool FindPassword(string credentialId, out string password)
        {
            password = null;
            using (KeyChainItemHandle handle = LookupKeyChainItem(credentialId))
            {
                if( handle == null)
                {
                    return false;
                }
                password = handle.Password;
            }

            return true;
        }

        private KeyChainItemHandle LookupKeyChainItem(string credentialId)
        {
            UInt32 passwordLength;
            IntPtr passwordPtr;
            IntPtr item;
            Interop.Security.OSStatus status = Interop.Security.SecKeychainFindGenericPassword(
                IntPtr.Zero,
                InteropUtils.GetLengthInBytes(credentialId),
                credentialId,
                0,
                null,
                out passwordLength,
                out passwordPtr,
                out item);

            if(status == Interop.Security.OSStatus.ErrSecSuccess)
            {
                return new KeyChainItemHandle(item, passwordPtr, passwordLength);
            }
            return null;
        }

        private bool DeletePasswordImpl(string credentialId)
        {
            // Find password, then Delete, then cleanup
            using (KeyChainItemHandle handle = LookupKeyChainItem(credentialId))
            {
                if (handle == null)
                {
                    return false;
                }
                Interop.Security.OSStatus status = Interop.Security.SecKeychainItemDelete(handle);
                return status == Interop.Security.OSStatus.ErrSecSuccess;
            }            
        }

        private class KeyChainItemHandle : SafeCreateHandle
        {
            private IntPtr passwordPtr;
            private int passwordLength;

            public KeyChainItemHandle() : base()
            {

            }
            
            public KeyChainItemHandle(IntPtr itemPtr) : this(itemPtr, IntPtr.Zero, 0)
            {
                
            }

            public KeyChainItemHandle(IntPtr itemPtr, IntPtr passwordPtr, UInt32 passwordLength)
                : base(itemPtr)
            {
                this.passwordPtr = passwordPtr;
                this.passwordLength = (int) passwordLength;
            }
            
            public string Password 
            { 
                get {
                    if (IsInvalid)
                    {
                        return null;
                    }
                    return InteropUtils.CopyToString(passwordPtr, passwordLength);
                }
            }
            protected override bool ReleaseHandle()
            {
                if (passwordPtr != IntPtr.Zero)
                {
                    Interop.Security.SecKeychainItemFreeContent(IntPtr.Zero, passwordPtr);
                }
                base.ReleaseHandle();
                return true;
            }
        }
    }
}