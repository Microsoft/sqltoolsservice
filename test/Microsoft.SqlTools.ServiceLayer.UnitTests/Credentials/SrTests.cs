//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Credentials.Utility;
using NUnit.Framework;

using CredSR = Microsoft.SqlTools.Credentials.SR;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Credentials
{
    /// <summary>
    /// ScriptFile test case
    /// </summary>
    public class SrTests
    {
        /// <summary>
        /// Simple "test" to access string resources
        /// The purpose of this test is for code coverage.  It's probably better to just 
        /// exclude string resources in the code coverage report than maintain this test.
        /// </summary>
        [Test]
        public void SrStringsTest()
        {
            var culture = CredSR.Culture;
            CredSR.Culture = culture;
            Assert.True(CredSR.Culture == culture);

            var CredentialsServiceInvalidCriticalHandle = CredSR.CredentialsServiceInvalidCriticalHandle;
            var CredentialsServicePasswordLengthExceeded = CredSR.CredentialsServicePasswordLengthExceeded;
            var CredentialsServiceTargetForDelete = CredSR.CredentialsServiceTargetForDelete;
            var CredentialsServiceTargetForLookup = CredSR.CredentialsServiceTargetForLookup;
            var CredentialServiceWin32CredentialDisposed = CredSR.CredentialServiceWin32CredentialDisposed;
        }

        [Test]
        public void SrStringsTestWithEnLocalization()
        {
            string locale = "en";
            var args = new string[] { "--locale", locale };
            CredentialsCommandOptions options = new CredentialsCommandOptions(args);
            Assert.AreEqual(CredSR.Culture.Name, options.Locale);
            Assert.AreEqual(options.Locale, locale);

            var TestLocalizationConstant = CredSR.TestLocalizationConstant;
            Assert.AreEqual("test", TestLocalizationConstant);
        }

        [Test]
        public void SrStringsTestWithEsLocalization()
        {
            string locale = "es";
            var args = new string[] { "--locale", locale };
            CredentialsCommandOptions options = new CredentialsCommandOptions(args);
            Assert.AreEqual(CredSR.Culture.Name, options.Locale);
            Assert.AreEqual(options.Locale, locale);

            var TestLocalizationConstant = CredSR.TestLocalizationConstant;
            Assert.AreEqual("prueba", TestLocalizationConstant);

            // Reset the locale
            SrStringsTestWithEnLocalization(); 
        }

        [Test]
        public void SrStringsTestWithNullLocalization()
        {
            CredSR.Culture = null;
            var args = new string[] { "" };
            CredentialsCommandOptions options = new CredentialsCommandOptions(args);
            Assert.Null(CredSR.Culture);
            Assert.AreEqual("", options.Locale);

            var TestLocalizationConstant = CredSR.TestLocalizationConstant;
            Assert.AreEqual("test", TestLocalizationConstant);
        }
    }
}
