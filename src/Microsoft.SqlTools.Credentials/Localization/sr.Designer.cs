﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.SqlTools.Credentials.Localization {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///    A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class sr {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        internal sr() {
        }
        
        /// <summary>
        ///    Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.SqlTools.Credentials.Localization.sr", typeof(sr).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///    Overrides the current thread's CurrentUICulture property for all
        ///    resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Win32Credential object is already disposed.
        /// </summary>
        public static string CredentialServiceWin32CredentialDisposed {
            get {
                return ResourceManager.GetString("CredentialServiceWin32CredentialDisposed", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Invalid CriticalHandle!.
        /// </summary>
        public static string CredentialsServiceInvalidCriticalHandle {
            get {
                return ResourceManager.GetString("CredentialsServiceInvalidCriticalHandle", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The password has exceeded 512 bytes.
        /// </summary>
        public static string CredentialsServicePasswordLengthExceeded {
            get {
                return ResourceManager.GetString("CredentialsServicePasswordLengthExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Target must be specified to delete a credential.
        /// </summary>
        public static string CredentialsServiceTargetForDelete {
            get {
                return ResourceManager.GetString("CredentialsServiceTargetForDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Target must be specified to check existance of a credential.
        /// </summary>
        public static string CredentialsServiceTargetForLookup {
            get {
                return ResourceManager.GetString("CredentialsServiceTargetForLookup", resourceCulture);
            }
        }
    }
}
