﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Test.CloudService.Development
{
    using Commands.CloudService.Development;
    using Commands.CloudService.Development.Scaffolding;
    using Commands.Utilities.CloudService;
    using Commands.Utilities.Common;
    using Commands.Utilities.Common.XmlSchema.ServiceConfigurationSchema;
    using System.Collections.Generic;
    using System.Linq;
    using Test.Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Basic unit tests for the Enable-Enable-AzureServiceProjectRemoteDesktop command.
    /// </summary>
    [TestClass]
    public class DisableAzureRemoteDesktopCommandTest : TestBase
    {
        private MockCommandRuntime mockCommandRuntime;

        private AddAzureNodeWebRoleCommand addNodeWebCmdlet;

        private AddAzureNodeWorkerRoleCommand addNodeWorkerCmdlet;

        private DisableAzureServiceProjectRemoteDesktopCommand disableRDCmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            mockCommandRuntime = new MockCommandRuntime();

            disableRDCmdlet = new DisableAzureServiceProjectRemoteDesktopCommand();
            disableRDCmdlet.CommandRuntime = mockCommandRuntime;
        }

        private static void VerifyDisableRoleSettings(CloudServiceProject service)
        {
            IEnumerable<RoleSettings> settings =
                Enumerable.Concat(
                    service.Components.CloudConfig.Role,
                    service.Components.LocalConfig.Role);
            foreach (RoleSettings roleSettings in settings)
            {
                Assert.AreEqual(
                    1,
                    roleSettings.ConfigurationSettings
                        .Where(c => c.name == "Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" && c.value == "false")
                        .Count());
            }
        }

        /// <summary>
        /// Enable remote desktop for an empty service.
        /// </summary>
        [TestMethod]
        public void DisableRemoteDesktopForEmptyService()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                files.CreateNewService("NEW_SERVICE");
                disableRDCmdlet.DisableRemoteDesktop();
            }
        }

        /// <summary>
        /// Disable remote desktop for a simple web role.
        /// </summary>
        [TestMethod]
        public void DisableRemoteDesktopForWebRole()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                string rootPath = files.CreateNewService("NEW_SERVICE");
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WebRole" };
                addNodeWebCmdlet.ExecuteCmdlet();
                disableRDCmdlet.PassThru = true;
                disableRDCmdlet.DisableRemoteDesktop();

                Assert.IsTrue((bool)mockCommandRuntime.OutputPipeline[1]);
            }
        }

        /// <summary>
        /// Disable remote desktop for web and worker roles.
        /// </summary>
        [TestMethod]
        public void DisableRemoteDesktopForWebAndWorkerRoles()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                string rootPath = files.CreateNewService("NEW_SERVICE");
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WebRole" };
                addNodeWebCmdlet.ExecuteCmdlet();
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WorkerRole" };
                addNodeWorkerCmdlet.ExecuteCmdlet();
                disableRDCmdlet.DisableRemoteDesktop();
            }
        }

        /// <summary>
        /// Enable then disable remote desktop for a simple web role.
        /// </summary>
        [TestMethod]
        public void EnableDisableRemoteDesktopForWebRole()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                string rootPath = files.CreateNewService("NEW_SERVICE");
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WebRole" };
                addNodeWebCmdlet.ExecuteCmdlet();
                EnableAzureRemoteDesktopCommandTest.EnableRemoteDesktop("user", "GoodPassword!");
                disableRDCmdlet.DisableRemoteDesktop();
                // Verify the role has been setup with forwarding, access,
                // and certs
                CloudServiceProject service = new CloudServiceProject(rootPath, null);
                EnableAzureRemoteDesktopCommandTest.VerifyWebRole(service.Components.Definition.WebRole[0], true);
                VerifyDisableRoleSettings(service);
            }
        }

        /// <summary>
        /// Enable then disable remote desktop for web and worker roles.
        /// </summary>
        [TestMethod]
        public void EnableDisableRemoteDesktopForWebAndWorkerRoles()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                string rootPath = files.CreateNewService("NEW_SERVICE");
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WebRole" };
                addNodeWebCmdlet.ExecuteCmdlet();
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WorkerRole" };
                addNodeWorkerCmdlet.ExecuteCmdlet();
                EnableAzureRemoteDesktopCommandTest.EnableRemoteDesktop("user", "GoodPassword!");
                disableRDCmdlet.DisableRemoteDesktop();
                // Verify the roles have been setup with forwarding, access,
                // and certs
                CloudServiceProject service = new CloudServiceProject(rootPath, null);
                EnableAzureRemoteDesktopCommandTest.VerifyWebRole(service.Components.Definition.WebRole[0], false);
                EnableAzureRemoteDesktopCommandTest.VerifyWorkerRole(service.Components.Definition.WorkerRole[0], true);
                VerifyDisableRoleSettings(service);
            }
        }

        /// <summary>
        /// Enable then disable remote desktop for web and worker roles.
        /// </summary>
        [TestMethod]
        public void EnableDisableEnableRemoteDesktopForWebAndWorkerRoles()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                string rootPath = files.CreateNewService("NEW_SERVICE");
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WebRole" };
                addNodeWebCmdlet.ExecuteCmdlet();
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = "WorkerRole" };
                addNodeWorkerCmdlet.ExecuteCmdlet();
                EnableAzureRemoteDesktopCommandTest.EnableRemoteDesktop("user", "GoodPassword!");
                disableRDCmdlet.DisableRemoteDesktop();
                EnableAzureRemoteDesktopCommandTest.EnableRemoteDesktop("user", "GoodPassword!");
                // Verify the roles have been setup with forwarding, access,
                // and certs
                CloudServiceProject service = new CloudServiceProject(rootPath, null);
                EnableAzureRemoteDesktopCommandTest.VerifyWebRole(service.Components.Definition.WebRole[0], false);
                EnableAzureRemoteDesktopCommandTest.VerifyWorkerRole(service.Components.Definition.WorkerRole[0], true);
                EnableAzureRemoteDesktopCommandTest.VerifyRoleSettings(service);
            }
        }    
    }
}
