using Evergine.Common.Input;
using Evergine.Common.Input.Keyboard;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.XR.QR;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace XrvSamples.Services
{
    public class FakeQRWatcherService : UpdatableService, IQRCodeWatcherService
    {
        private Scene currentScene;
        private Camera3D camera;
        private KeyboardDispatcher keyboardDispatcher;
        private List<QRCode> qrCodes;
        private (string Data, float Size, Vector3 RefPosition) currentDetectionInfo;

        [BindService]
        private ScreenContextManager screenContext = null;

        [BindService]
        private GraphicsPresenter graphicsPresenter = null;

        public FakeQRWatcherService()
        {
            this.qrCodes = new List<QRCode>();
        }

        public bool IsSupported => true;

        public bool IsWatcherRunning { get; private set; }

        public string ValidQRCodeData { get; set; } = "This is XRV!";

        public string InvalidQRCodeData { get; set; } = "invalid";

        public IEnumerable<QRCode> QRCodes => this.qrCodes.AsReadOnly();

        public bool DebugEnabled { get; set; } = false;

        public event EventHandler IsWatcherRunningChanged;
        public event EventHandler<QRCode> QRCodeAdded;
        public event EventHandler<QRCode> QRCodeUpdated;
        public event EventHandler<QRCode> QRCodeRemoved;

        protected override void OnActivated()
        {
            base.OnActivated();
            this.screenContext.OnActivatingScene += this.ScreenContext_OnActivatingScene;
            this.screenContext.OnDesactivatingScene += this.ScreenContext_OnDesactivatingScene;
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.screenContext.OnActivatingScene -= this.ScreenContext_OnActivatingScene;
            this.screenContext.OnDesactivatingScene -= this.ScreenContext_OnDesactivatingScene;
        }

        public override void Update(TimeSpan gameTime)
        {
            this.camera = this.currentScene?.Managers.RenderManager.ActiveCamera3D;
            this.keyboardDispatcher = this.graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;

            if (this.keyboardDispatcher == null || !this.IsWatcherRunning)
            {
                return;
            }

            if (this.keyboardDispatcher.ReadKeyState(Keys.D1) == ButtonState.Pressing)
            {
                this.currentDetectionInfo = (this.ValidQRCodeData, 0.1f, default);
            }
            else if (this.keyboardDispatcher.ReadKeyState(Keys.D2) == ButtonState.Pressing)
            {
                this.currentDetectionInfo = (this.InvalidQRCodeData, 0.1f, default);
            }

            this.UpdateDetection();
        }

        public Task StartQRWatchingAsync(CancellationToken cancellationToken = default)
        {
            this.IsWatcherRunning = true;
            this.IsWatcherRunningChanged?.Invoke(this, EventArgs.Empty);

            return Task.CompletedTask;
        }

        public Task StopQRWatchingAsync()
        {
            this.IsWatcherRunning = false;
            this.IsWatcherRunningChanged?.Invoke(this, EventArgs.Empty);

            return Task.CompletedTask;
        }

        public void ClearQRCodes()
        {
            this.qrCodes.Clear();
        }

        private void UpdateDetection()
        {
            if (this.currentDetectionInfo == default)
            {
                return;
            }

            var detectedCode = this.qrCodes.FirstOrDefault(code => code.Data == this.currentDetectionInfo.Data);
            bool isNew = detectedCode == null;

            if (isNew)
            {
                this.ClearQRCodes();
                var code = new FakeQrCode(this.currentDetectionInfo.Data, physicalSideLength: this.currentDetectionInfo.Size);
                this.qrCodes.Add(code);
                detectedCode = code;

                var cameraTransform = this.camera.Owner.FindComponent<Transform3D>();
                var position = cameraTransform.Position + cameraTransform.WorldTransform.Forward * 1.5f;
                position.Y = 0.01f;

                var transform = 
                    Matrix4x4.CreateFromYawPitchRoll(0, 0, 0) * Matrix4x4.CreateTranslation(position);

                ((FakeQrCode)detectedCode).Update(transform);
                this.QRCodeAdded?.Invoke(this, detectedCode);
            }
            else
            {
                this.QRCodeUpdated?.Invoke(this, detectedCode);
            }

            if (this.DebugEnabled)
            {
                var lineBatch = this.currentScene.Managers.RenderManager.LineBatch3D;
                foreach (var code in this.qrCodes.Where(c => c.Transform.HasValue))
                {
                    lineBatch.DrawAxis(code.Transform.Value, code.PhysicalSideLength * 2f);
                }
            }
        }

        private void ScreenContext_OnActivatingScene(Scene scene) => this.currentScene = scene;

        private void ScreenContext_OnDesactivatingScene(Scene scene)
        {
            if (this.currentScene == scene)
            {
                this.currentScene = null;
            }
        }

        private class FakeQrCode : QRCode
        {
            private Matrix4x4? transform = Matrix4x4.Identity;
            private DateTimeOffset lastDetectedTime = DateTime.Now;
            private float physicalSideLength;

            public FakeQrCode(string data, Matrix4x4? transform = null, float physicalSideLength = 0.1f)
                : base(Guid.NewGuid(), data)
            {
                this.transform = transform;
                this.physicalSideLength = physicalSideLength;
            }

            public override float PhysicalSideLength => this.physicalSideLength;

            public override DateTimeOffset LastDetectedTime => this.lastDetectedTime;

            public override Matrix4x4? Transform => this.transform;

            public void Update(Matrix4x4 transform)
            {
                this.transform = transform;
            }
        }
    }
}
