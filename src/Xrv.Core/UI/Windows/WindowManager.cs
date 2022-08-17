using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using Xrv.Core.UI.Dialogs;

namespace Xrv.Core.UI.Windows
{
    public class WindowManager : SceneManager
    {
        [BindService]
        private AssetsService assetsService = null;

        public IEnumerable<Window> AllWindows 
        { 
            get => this.Managers.EntityManager.FindComponentsOfType<Window>(isExactType: false); 
        }

        public Window CreateWindow() => this.CreateWindow(Vector3.Zero);

        public Window CreateWindow(Vector3 position)
        {
            var prefab = this.GetWindowPrefab();
            var windowEntity = prefab.Instantiate();
            var window = windowEntity.FindComponentInChildren<Window>();
            var transform = windowEntity.FindComponent<Transform3D>();
            transform.Position = position;

            this.Managers.EntityManager.Add(windowEntity);
            windowEntity.IsEnabled = false;

            return window;
        }

        public AlertDialog ShowAlertDialog(string title, string text, string acceptText) =>
            this.ShowAlertDialog(title, text, acceptText, Vector3.Zero);

        public AlertDialog ShowAlertDialog(string title, string text, string acceptText, Vector3 position)
        {
            var dialog = this.CreateDialogAux<AlertDialog>(title, text, position);
            dialog.AcceptOption.Configuration.Text = acceptText;

            var contentPrefab = this.GetBaseDialogPrefab();
            dialog.Configuration.Content = contentPrefab.Instantiate();
            dialog.Open();

            return dialog;
        }

        public ConfirmDialog ShowConfirmDialog(string title, string text, string cancelText, string acceptText) =>
            this.ShowConfirmDialog(title, text, cancelText, acceptText, Vector3.Zero);

        public ConfirmDialog ShowConfirmDialog(string title, string text, string cancelText, string acceptText, Vector3 position)
        {
            var dialog = this.CreateDialogAux<ConfirmDialog>(title, text, position);
            dialog.CancelOption.Configuration.Text = cancelText;
            dialog.AcceptOption.Configuration.Text = acceptText;

            var contentPrefab = this.GetBaseDialogPrefab();
            dialog.Configuration.Content = contentPrefab.Instantiate();
            dialog.Open();

            return dialog;
        }

        private TDialog CreateDialogAux<TDialog>(string title, string text, Vector3 position)
            where TDialog : Dialog
        {
            const float DialogWidth = 0.2f;
            const float DialogHeight = 0.11f;

            var window = this.CreateWindow(position);
            var dialog = Activator.CreateInstance<TDialog>();
            dialog.AllowPin = false;

            var owner = window.Owner;
            owner.RemoveComponent<Window>();
            owner.RemoveComponent<BaseWindowConfigurator>(false);

            var dialogConfigurator = new DialogConfigurator
            {
                Title = title,
                Text = text,
            };
            var size = dialogConfigurator.Size;
            size.X = DialogWidth;
            size.Y = DialogHeight;
            dialogConfigurator.Size = size;
            dialogConfigurator.FrontPlateSize = size;
            var offset = dialogConfigurator.FrontPlateOffsets;
            offset.X = 0;
            offset.Y = 0;
            dialogConfigurator.FrontPlateOffsets = offset;
            dialogConfigurator.DisplayLogo = false;

            owner.AddComponent(dialogConfigurator);
            owner.AddComponent(dialog);

            return dialog;
        }

        private Prefab GetWindowPrefab() =>
            this.assetsService.Load<Prefab>(DefaultResourceIDs.Prefabs.Window);

        private Prefab GetBaseDialogPrefab() =>
            this.assetsService.Load<Prefab>(DefaultResourceIDs.Prefabs.BaseDialogContents);
    }
}
