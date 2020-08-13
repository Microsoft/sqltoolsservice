// WARNING:
// This file was generated by the Microsoft DataWarehouse String Resource Tool 4.0.0.0
// from information in sr.strings
// DO NOT MODIFY THIS FILE'S CONTENTS, THEY WILL BE OVERWRITTEN
//
namespace Microsoft.SqlTools.ResourceProvider.Core
{
    using System;
    using System.Reflection;
    using System.Resources;
    using System.Globalization;

    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SR
    {
        protected SR()
        { }

        public static CultureInfo Culture
        {
            get
            {
                return Keys.Culture;
            }
            set
            {
                Keys.Culture = value;
            }
        }


        public static string NoSubscriptionsFound
        {
            get
            {
                return Keys.GetString(Keys.NoSubscriptionsFound);
            }
        }

        public static string AzureServerNotFound
        {
            get
            {
                return Keys.GetString(Keys.AzureServerNotFound);
            }
        }

        public static string AzureSubscriptionFailedErrorMessage
        {
            get
            {
                return Keys.GetString(Keys.AzureSubscriptionFailedErrorMessage);
            }
        }

        public static string DatabaseDiscoveryFailedErrorMessage
        {
            get
            {
                return Keys.GetString(Keys.DatabaseDiscoveryFailedErrorMessage);
            }
        }

        public static string FirewallRuleAccessForbidden
        {
            get
            {
                return Keys.GetString(Keys.FirewallRuleAccessForbidden);
            }
        }

        public static string FirewallRuleCreationFailed
        {
            get
            {
                return Keys.GetString(Keys.FirewallRuleCreationFailed);
            }
        }

        public static string FirewallRuleCreationFailedWithError
        {
            get
            {
                return Keys.GetString(Keys.FirewallRuleCreationFailedWithError);
            }
        }

        public static string InvalidIpAddress
        {
            get
            {
                return Keys.GetString(Keys.InvalidIpAddress);
            }
        }

        public static string InvalidServerTypeErrorMessage
        {
            get
            {
                return Keys.GetString(Keys.InvalidServerTypeErrorMessage);
            }
        }

        public static string LoadingExportableFailedGeneralErrorMessage
        {
            get
            {
                return Keys.GetString(Keys.LoadingExportableFailedGeneralErrorMessage);
            }
        }

        public static string FirewallRuleUnsupportedConnectionType
        {
            get
            {
                return Keys.GetString(Keys.FirewallRuleUnsupportedConnectionType);
            }
        }

        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class Keys
        {
            static ResourceManager resourceManager = new ResourceManager("Microsoft.SqlTools.ResourceProvider.Core.Localization.SR", typeof(SR).GetTypeInfo().Assembly);

            static CultureInfo _culture = null;


            public const string NoSubscriptionsFound = "NoSubscriptionsFound";


            public const string AzureServerNotFound = "AzureServerNotFound";


            public const string AzureSubscriptionFailedErrorMessage = "AzureSubscriptionFailedErrorMessage";


            public const string DatabaseDiscoveryFailedErrorMessage = "DatabaseDiscoveryFailedErrorMessage";


            public const string FirewallRuleAccessForbidden = "FirewallRuleAccessForbidden";


            public const string FirewallRuleCreationFailed = "FirewallRuleCreationFailed";


            public const string FirewallRuleCreationFailedWithError = "FirewallRuleCreationFailedWithError";


            public const string InvalidIpAddress = "InvalidIpAddress";


            public const string InvalidServerTypeErrorMessage = "InvalidServerTypeErrorMessage";


            public const string LoadingExportableFailedGeneralErrorMessage = "LoadingExportableFailedGeneralErrorMessage";


            public const string FirewallRuleUnsupportedConnectionType = "FirewallRuleUnsupportedConnectionType";


            private Keys()
            { }

            public static CultureInfo Culture
            {
                get
                {
                    return _culture;
                }
                set
                {
                    _culture = value;
                }
            }

            public static string GetString(string key)
            {
                return resourceManager.GetString(key, _culture);
            }

        }
    }
}
