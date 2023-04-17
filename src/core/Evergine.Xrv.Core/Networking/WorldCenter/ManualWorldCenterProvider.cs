// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.UI.Windows;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.WorldCenter
{
    /// <summary>
    /// World center provider that lets user to manually place a reference marker.
    /// </summary>
    public class ManualWorldCenterProvider : IWorldCenterProvider
    {
        private readonly AssetsService assetsService;
        private readonly EntityManager entityManager;
        private readonly WindowsSystem windowsSystem;
        private readonly LocalizationService localization;
        private readonly NetworkSystem networkSystem;
        private TaskCompletionSource<Matrix4x4?> poseCompletionSource;
        private Entity instance;
        private ManualWorldCenterController controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualWorldCenterProvider"/> class.
        /// </summary>
        /// <param name="assetsService">Assets service.</param>
        /// <param name="entityManager">Entity manager.</param>
        /// <param name="windowsSystem">Windows system.</param>
        /// <param name="localization">Localization.</param>
        /// <param name="networkSystem">Network system.</param>
        public ManualWorldCenterProvider(
            AssetsService assetsService,
            EntityManager entityManager,
            WindowsSystem windowsSystem,
            LocalizationService localization,
            NetworkSystem networkSystem)
        {
            this.assetsService = assetsService;
            this.entityManager = entityManager;
            this.windowsSystem = windowsSystem;
            this.localization = localization;
            this.networkSystem = networkSystem;
        }

        /// <inheritdoc/>
        public async Task<Matrix4x4?> GetWorldCenterPoseAsync(CancellationToken cancellationToken = default)
        {
            this.poseCompletionSource?.TrySetCanceled();
            this.poseCompletionSource = new TaskCompletionSource<Matrix4x4?>();

            await this.EnsureMarkerIsCreatedAsync().ConfigureEvergineAwait(EvergineTaskContinueOn.Foreground);

            this.windowsSystem.ShowAlertDialog(
                () => this.localization.GetString(() => Resources.Strings.WorldCenter_Manual_InitialMessage_Title),
                () => this.localization.GetString(() => Resources.Strings.WorldCenter_Manual_InitialMessage_Message),
                () => this.localization.GetString(() => Resources.Strings.Global_Ok));

            await this.poseCompletionSource.Task;
            return this.poseCompletionSource.Task.Result;
        }

        /// <inheritdoc/>
        public void OnWorldCenterPoseUpdate(Entity worldCenter)
        {
        }

        /// <inheritdoc/>
        public void CleanWorldCenterEntity(Entity worldCenter)
        {
            if (this.instance != null)
            {
                this.instance.IsEnabled = false;
            }
        }

        private async Task EnsureMarkerIsCreatedAsync()
        {
            if (this.instance != null)
            {
                this.instance.IsEnabled = true;
                return;
            }

            await EvergineBackgroundTask.Run(() =>
            {
                var markerPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Networking.ManualMarker_weprefab);
                this.instance = markerPrefab.Instantiate();
                this.instance.Name = "WorldCenterMarker";
                this.controller = this.instance.FindComponent<ManualWorldCenterController>();
                this.controller.LockedChanged += this.Controller_LockedChanged;
            }).ConfigureEvergineAwait(EvergineTaskContinueOn.Foreground);

            this.entityManager.Add(this.instance);
        }

        private void Controller_LockedChanged(object sender, EventArgs e)
        {
            if (!this.controller.IsLocked)
            {
                return;
            }

            /*
             * When user locks the orientation, we assume that this action is
             * like a confirmation, and proceed with pose update. This will actually
             * change world-center pose.
             * There are two paths here (expressed as code reading order, but not as user interaction order):
             * - Pose is being updated while session is alive.
             * - Pose is being set for first time in networking join to session flow.
             */
            this.controller.ConfirmCurrentPoseIsNewReferenceValue();
            if (this.poseCompletionSource.Task.IsCompleted)
            {
                this.networkSystem.SetWorldCenterPose(this.controller.WorldCenterPose);
            }
            else
            {
                this.poseCompletionSource.TrySetResult(this.controller.WorldCenterPose);
            }
        }
    }
}
