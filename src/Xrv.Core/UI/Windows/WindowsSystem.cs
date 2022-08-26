// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xrv.Core.UI.Dialogs;

namespace Xrv.Core.UI.Windows
{
    public class WindowsSystem
    {
        private readonly EntityManager entityManager;
        private readonly AssetsService assetsService;

        private AlertDialog alertDialog;
        private ConfirmDialog confirmDialog;

        public IEnumerable<Window> AllWindows
        {
            get => this.entityManager.FindComponentsOfType<Window>(isExactType: false);
        }

        public WindowsSystem(EntityManager entityManager, AssetsService assetsService)
        {
            this.entityManager = entityManager;
            this.assetsService = assetsService;
            this.Distances = new Distances();
        }

        public Distances Distances { get; private set; }

        public Material OverrideIconMaterial { get; set; }

        internal void Load()
        {
            this.CreateAlertDialogInstance();
            this.CreateConfirmDialogInstance();
        }

        private void CreateAlertDialogInstance()
        {
            this.alertDialog = new AlertDialog();
            var owner = this.CreateDialogAux(this.alertDialog, string.Empty, string.Empty);
            this.entityManager.Add(owner);

            var contentPrefab = this.GetBaseDialogPrefab();
            var configurator = owner.FindComponent<DialogConfigurator>();
            configurator.Content = contentPrefab.Instantiate();

            owner.IsEnabled = false;
        }

        private void CreateConfirmDialogInstance()
        {
            this.confirmDialog = new ConfirmDialog();
            var owner = this.CreateDialogAux(this.confirmDialog, string.Empty, string.Empty);
            this.entityManager.Add(owner);

            var contentPrefab = this.GetBaseDialogPrefab();
            var configurator = owner.FindComponent<DialogConfigurator>();
            configurator.Content = contentPrefab.Instantiate();

            owner.IsEnabled = false;
        }

        public Window CreateWindow(Action<WindowConfigurator> configure = null)
        {
            var window = new Window();
            var windowEntity = this.BuildWindow(window, (BaseWindowConfigurator)null);
            if (configure != null)
            {
                var configurator = windowEntity.FindComponent<WindowConfigurator>();
                configure.Invoke(configurator);
            }

            this.entityManager.Add(windowEntity);
            return window;
        }

        public AlertDialog ShowAlertDialog(string title, string text, string acceptText)
        {
            bool anyOpened = this.CloseAllDialogs();

            var configurator = this.alertDialog.Configurator as DialogConfigurator;
            configurator.Title = title;
            configurator.Text = text;
            this.alertDialog.AcceptOption.Configuration.Text = acceptText;
            this.OpenDialogWithDelayIfRequired(this.alertDialog, anyOpened);

            return this.alertDialog;
        }

        public ConfirmDialog ShowConfirmDialog(string title, string text, string cancelText, string acceptText)
        {
            bool anyOpened = this.CloseAllDialogs();

            var configurator = this.confirmDialog.Configurator as DialogConfigurator;
            configurator.Title = title;
            configurator.Text = text;
            this.confirmDialog.CancelOption.Configuration.Text = cancelText;
            this.confirmDialog.AcceptOption.Configuration.Text = acceptText;
            this.OpenDialogWithDelayIfRequired(this.confirmDialog, anyOpened);

            return this.confirmDialog;
        }

        public Entity BuildWindow<TWindow, TConfigurator>(TWindow instance, TConfigurator configInstance)
            where TWindow : Window
            where TConfigurator : BaseWindowConfigurator
        {
            var prefab = this.GetWindowPrefab();
            var windowEntity = prefab.Instantiate();
            windowEntity.AddComponent(instance);

            if (configInstance != default(TConfigurator))
            {
                var configurator = windowEntity.FindComponent<BaseWindowConfigurator>(isExactType: false);
                windowEntity.RemoveComponent(configurator);
                windowEntity.AddComponent(configInstance);
            }
            else if (this.OverrideIconMaterial != null)
            {
                var configurator = windowEntity.FindComponent<WindowConfigurator>(isExactType: false);
                configurator.LogoMaterial = this.OverrideIconMaterial;
            }

            windowEntity.IsEnabled = false;

            return windowEntity;
        }

        private Entity CreateDialogAux<TDialog>(TDialog dialog, string title, string text)
            where TDialog : Dialog
        {
            const float DialogWidth = 0.2f;
            const float DialogHeight = 0.11f;

            var dialogConfigurator = new DialogConfigurator
            {
                Title = title,
                Text = text,
            };
            var owner = this.BuildWindow(dialog, dialogConfigurator);
            dialog.AllowPin = false;
            dialog.EnableManipulation = false;

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

            return owner;
        }

        private bool CloseAllDialogs()
        {
            bool anyOpen = false;

            if (this.confirmDialog.IsOpened)
            {
                anyOpen = true;
                this.confirmDialog.Close();
            }
            else if (this.alertDialog.IsOpened)
            {
                anyOpen = true;
                this.alertDialog.Close();
            }

            return anyOpen;
        }

        private void OpenDialogWithDelayIfRequired(Dialog dialog, bool delayed)
        {
            var delay = TimeSpan.FromMilliseconds(delayed ? 200 : 0);
            var ignore = EvergineForegroundTask.Run(async () =>
            {
                await Task.Delay(delay);
                dialog.Open();
            });
        }

        private Prefab GetWindowPrefab() =>
            this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Window);

        private Prefab GetBaseDialogPrefab() =>
            this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.BaseDialogContents);
    }
}
