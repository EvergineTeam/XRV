// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.XR.QR;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xrv.Core.Networking;
using Xrv.Core.Utils;

namespace Xrv.Core.Services.QR
{
    /// <summary>
    /// Scanning workflow for QR codes.
    /// </summary>
    public class QrScanningFlow
    {
        private readonly EntityManager entityManager;
        private readonly RenderManager renderManager;
        private readonly AssetsService assetsService;
        private readonly IQRCodeWatcherService watcherService;

        private Entity qrScannerEntity;
        private Entity qrMarkerEntityPivot;
        private Entity qrMarkerEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="QrScanningFlow"/> class.
        /// </summary>
        /// <param name="entityManager">Entity manager.</param>
        /// <param name="renderManager">Render manager.</param>
        /// <param name="assetsService">Assets service.</param>
        public QrScanningFlow(
            EntityManager entityManager,
            RenderManager renderManager,
            AssetsService assetsService)
        {
            this.entityManager = entityManager;
            this.renderManager = renderManager;
            this.assetsService = assetsService;
            this.RegisterQrCodeWatcherServiceByPlatform();
            this.watcherService = Application.Current.Container.Resolve<IQRCodeWatcherService>();
        }

        /// <summary>
        /// Raised when an expected QR code has been detected.
        /// </summary>
        public event EventHandler<QrScanningResultEventArgs> ExpectedCodeDetected;

        /// <summary>
        /// Raised when an unexpected QR code has been detected.
        /// </summary>
        public event EventHandler<QrScanningResultEventArgs> UnexpectedCodeDetected;

        /// <summary>
        /// Raised when workflow is canceled.
        /// </summary>
        public event EventHandler Canceled;

        /// <summary>
        /// Gets or sets expected codes to be detected.
        /// </summary>
        public IEnumerable<string> ExpectedCodes { get; set; }

        /// <summary>
        /// Gets entity marker that is used when a QR code is detected.
        /// </summary>
        public Entity Marker { get => this.qrMarkerEntityPivot; }

        /// <summary>
        /// Executes QR detection workflow:
        /// - Detection hub is shown.
        /// - Once a QR is detected, marker appears and executes an animation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>QR scanning result.</returns>
        public async Task<QrScanningResult> ExecuteFlowAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(() => this.Stop());

            var taskCompletionSource = new TaskCompletionSource<QrScanningResult>();
            this.Start();

            var scanCompletedHandler = new EventHandler<QrScanningResultEventArgs>((sender, args) =>
            {
                var result = new QrScanningResult(args.Code, args.Pose);
                if (taskCompletionSource.Task.Status < TaskStatus.RanToCompletion)
                {
                    taskCompletionSource.TrySetResult(result);
                }
            });
            var cancelledHandler = new EventHandler((sender, args) =>
            {
                if (taskCompletionSource.Task.Status < TaskStatus.RanToCompletion)
                {
                    taskCompletionSource.TrySetCanceled();
                }
            });

            QrScanningResult result = null;

            try
            {
                this.ExpectedCodeDetected += scanCompletedHandler;
                this.Canceled += cancelledHandler;

                result = await taskCompletionSource.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                this.ExpectedCodeDetected -= scanCompletedHandler;
                this.Canceled -= cancelledHandler;

                this.InternalStop(false);
            }

            return result;
        }

        /// <summary>
        /// Starts worflow.
        /// </summary>
        /// <exception cref="NotSupportedException">Raised if QR watch is not supported.</exception>
        public void Start()
        {
            if (this.watcherService?.IsSupported != true)
            {
                throw new NotSupportedException($"QR scanning not supported");
            }

            // Create and attach entity to the camera
            if (this.qrScannerEntity == null)
            {
                var scannerPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Services.QR.QrScanner_weprefab);
                this.qrScannerEntity = scannerPrefab.Instantiate();
                this.qrScannerEntity.Name = "qrScannerHub";

                var transform = this.qrScannerEntity.FindComponent<Transform3D>();
                transform.LocalPosition = Vector3.Forward * 0.5f;

                var camera = this.renderManager.ActiveCamera3D;
                var owner = camera.Owner;
                owner.AddChild(this.qrScannerEntity);

                var markerPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Services.QR.QrMarker_weprefab);
                this.qrMarkerEntity = markerPrefab.Instantiate();
                this.qrMarkerEntity.Name = "qrMarker";
                this.qrMarkerEntityPivot = new Entity("qrMarkerPivot")
                    .AddComponent(new Transform3D())
                    .AddComponent(new WorldAnchor());
                this.qrMarkerEntityPivot.IsEnabled = false;

                this.qrMarkerEntityPivot.AddChild(this.qrMarkerEntity);
                this.entityManager.Add(this.qrMarkerEntityPivot);
            }

            this.SubscribeEvents();
            this.watcherService.ClearQRCodes();
            this.qrScannerEntity.IsEnabled = true;
            this.qrMarkerEntityPivot.IsEnabled = false;

            // Remove marker spatial anchor, if any
            var worldAnchor = this.qrMarkerEntityPivot.FindComponent<WorldAnchor>();
            worldAnchor.RemoveAnchor();
        }

        /// <summary>
        /// Stops worflow.
        /// </summary>
        public void Stop() => this.InternalStop();

        private void InternalStop(bool raiseCancelledEvent = true)
        {
            this.UnsubscribeEvents();
            this.qrScannerEntity.IsEnabled = false;

            var qrMarker = this.qrMarkerEntity.FindComponent<QrMarker>();
            if (qrMarker?.IsValidMarker != true)
            {
                this.qrMarkerEntityPivot.IsEnabled = false;
            }

            if (raiseCancelledEvent)
            {
                this.Canceled?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SubscribeEvents()
        {
            if (this.watcherService != null)
            {
                this.watcherService.QRCodeAdded += this.WatcherService_QRCodeAdded;
            }
        }

        private void UnsubscribeEvents()
        {
            if (this.watcherService != null)
            {
                this.watcherService.QRCodeAdded -= this.WatcherService_QRCodeAdded;
            }
        }

        private void WatcherService_QRCodeAdded(object sender, QRCode code)
        {
            if (!code.Transform.HasValue)
            {
                return;
            }

            QrScanningResultEventArgs args = null;
            Matrix4x4 pose = code.Transform.Value;
            this.FixUpCodePoseByPlatform(ref pose);

            // Recalculate pose values, we want scale to be code physical length
            pose = Matrix4x4.CreateFromTRS(pose.Translation, pose.Orientation, new Vector3(code.PhysicalSideLength));

            bool anyExpectedCode = this.ExpectedCodes?.Any() == true;
            if (anyExpectedCode)
            {
                bool expectedCodeFound = this.ExpectedCodes.Any(data => code.Data == data);
                args = new QrScanningResultEventArgs(expectedCodeFound, code, pose);
            }
            else
            {
                args = new QrScanningResultEventArgs(true, code, pose);
            }

            this.qrMarkerEntityPivot.IsEnabled = true;
            var transform = this.qrMarkerEntityPivot.FindComponent<Transform3D>();

            // Vector.One? look comment below
            transform.WorldTransform = Matrix4x4.CreateFromTRS(args.Pose.Translation, args.Pose.Orientation, Vector3.One);
            transform = this.qrMarkerEntity.FindComponent<Transform3D>();
            var qrLocalPosition = transform.LocalPosition;
            this.FixUpCodeOrigin(ref qrLocalPosition);

            // apply scale separately to avoid all qrMarkerEntityPivot to be scaled (we are
            // only interested in qrMarkerEntity, that should fit real QR size.
            transform.LocalPosition = qrLocalPosition * pose.Scale.X;
            transform.Scale = pose.Scale;

            var qrMarker = this.qrMarkerEntity.FindComponent<QrMarker>();
            qrMarker.IsValidMarker = args.IsValidResult;

            var worldAnchor = this.qrMarkerEntityPivot.FindComponent<WorldAnchor>();
            worldAnchor.SaveAnchor();

            if (args.IsValidResult)
            {
                this.ExpectedCodeDetected?.Invoke(this, args);
            }
            else
            {
                this.UnexpectedCodeDetected?.Invoke(this, args);
            }
        }

        private void FixUpCodePoseByPlatform(ref Matrix4x4 pose)
        {
            if (DeviceHelper.IsHoloLens())
            {
                // https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/qr-code-tracking-unity#getting-the-coordinate-system-for-a-qr-code
                pose = Matrix4x4.CreateRotationX(-MathHelper.Pi / 2) * pose;
            }
        }

        private void FixUpCodeOrigin(ref Vector3 position)
        {
            /* As stated here
             * https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/qr-code-tracking-unity#getting-the-coordinate-system-for-a-qr-code
             *
             * HoloLens API coordinate system considers top-left corner of QR code as origin.
             * Our prefab origin is in the center (0.5, 0.5). We don't change this because maybe, in the future,
             * we integrate other platforms for QR scanning, and maybe those platforms returns QR position from its center.
             */

            if (DeviceHelper.IsHoloLens())
            {
                position.X = position.Z = 0.5f;
            }
        }

        private void RegisterQrCodeWatcherServiceByPlatform()
        {
            var container = Application.Current.Container;
            if (!container.IsRegistered<IQRCodeWatcherService>())
            {
#if UWP
                if (Microsoft.MixedReality.QR.QRCodeWatcher.IsSupported())
                {
                    var qr = new Evergine.MixedReality.QR.QRCodeWatcherService();
                    Application.Current.Container.RegisterInstance(qr);
                }
#endif
            }
        }
    }
}
