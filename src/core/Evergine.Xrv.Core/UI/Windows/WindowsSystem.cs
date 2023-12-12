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
using Evergine.Xrv.Core.UI.Dialogs;
using Evergine.Xrv.Core.UI.Notifications;

namespace Evergine.Xrv.Core.UI.Windows
{
    /// <summary>
    /// Windows system to manage windows and dialogs.
    /// </summary>
    public class WindowsSystem
    {
        private readonly EntityManager entityManager;
        private readonly AssetsService assetsService;

        private AlertDialog alertDialog;
        private ConfirmationDialog confirmationDialog;
        private NotificationsHub notificationsHub;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsSystem"/> class.
        /// </summary>
        /// <param name="entityManager">Entity manager.</param>
        /// <param name="assetsService">Assets service.</param>
        public WindowsSystem(EntityManager entityManager, AssetsService assetsService)
        {
            this.entityManager = entityManager;
            this.assetsService = assetsService;
            this.Distances = new Distances();
        }

        /// <summary>
        /// Gets all instantiated windows from the scene.
        /// </summary>
        public IEnumerable<Window> AllWindows
        {
            get => this.entityManager.FindComponentsOfType<Window>(isExactType: false);
        }

        /// <summary>
        /// Gets registered distances.
        /// </summary>
        public Distances Distances { get; private set; }

        /// <summary>
        /// Gets or sets default icon for windows. If not set, default window icon
        /// will be used.
        /// </summary>
        public Material OverrideIconMaterial { get; set; }

        /// <summary>
        /// Creates a window and adds it to <see cref="EntityManager" />.
        /// </summary>
        /// <param name="configure">Callback invoked to configure created window.</param>
        /// <param name="addToScene">Indicates if item should be added to scene.</param>
        /// <returns><see cref="Window"/> component.</returns>
        public Window CreateWindow(
            Action<WindowConfigurator> configure = null,
            bool addToScene = true) =>
            this.CreateWindow(out var _, configure, addToScene);

        /// <summary>
        /// Creates a window and adds it to <see cref="EntityManager" />.
        /// </summary>
        /// <param name="windowEntity">Gets window owner entity. It's useful to retrieve
        /// window owner before scene is fully loaded.</param>
        /// <param name="configure">Callback invoked to configure created window.</param>
        /// <param name="addToScene">Indicates if item should be added to scene.</param>
        /// <returns><see cref="Window"/> component.</returns>
        public Window CreateWindow(
            out Entity windowEntity,
            Action<WindowConfigurator> configure = null,
            bool addToScene = true)
        {
            var window = new Window();
            windowEntity = this.BuildWindow(window, (BaseWindowConfigurator)null);
            if (configure != null)
            {
                var configurator = windowEntity.FindComponent<WindowConfigurator>();
                configure.Invoke(configurator);
            }

            if (addToScene)
            {
                this.entityManager.Add(windowEntity);
            }

            return window;
        }

        /// <summary>
        /// Shows an alert dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="text">Dialog text message.</param>
        /// <param name="acceptText">Accept button text.</param>
        /// <returns><see cref="AlertDialog"/> component.</returns>
        public AlertDialog ShowAlertDialog(string title, string text, string acceptText) =>
            this.ShowAlertDialog(() => title, () => text, () => acceptText);

        /// <summary>
        /// Shows an alert dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="text">Dialog text message.</param>
        /// <param name="acceptText">Accept button text.</param>
        /// <returns><see cref="AlertDialog"/> component.</returns>
        public AlertDialog ShowAlertDialog(Func<string> title, Func<string> text, Func<string> acceptText)
        {
            bool anyOpened = this.CloseAllDialogs();

            var configurator = this.alertDialog.Configurator as DialogConfigurator;

            configurator.LocalizedTitle = title;
            configurator.LocalizedText = text;
            this.alertDialog.AcceptOption.Configuration.LocalizedText = acceptText;
            this.OpenDialogWithDelayIfRequired(this.alertDialog, anyOpened);

            return this.alertDialog;
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="text">Dialog text message.</param>
        /// <param name="cancelText">Cancel button text.</param>
        /// <param name="acceptText">Accept button text.</param>
        /// <returns><see cref="ConfirmationDialog"/> component.</returns>
        public ConfirmationDialog ShowConfirmationDialog(string title, string text, string cancelText, string acceptText) =>
            this.ShowConfirmationDialog(() => title, () => text, () => cancelText, () => acceptText);

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="text">Dialog text message.</param>
        /// <param name="cancelText">Cancel button text.</param>
        /// <param name="acceptText">Accept button text.</param>
        /// <returns><see cref="ConfirmationDialog"/> component.</returns>
        public ConfirmationDialog ShowConfirmationDialog(Func<string> title, Func<string> text, Func<string> cancelText, Func<string> acceptText)
        {
            bool anyOpened = this.CloseAllDialogs();

            var configurator = this.confirmationDialog.Configurator as DialogConfigurator;
            configurator.LocalizedTitle = title;
            configurator.LocalizedText = text;
            this.confirmationDialog.CancelOption.Configuration.LocalizedText = cancelText;
            this.confirmationDialog.AcceptOption.Configuration.LocalizedText = acceptText;
            this.OpenDialogWithDelayIfRequired(this.confirmationDialog, anyOpened);

            return this.confirmationDialog;
        }

        /// <summary>
        /// Builds a window but it's not added to the scene.
        /// </summary>
        /// <typeparam name="TWindow">Window type.</typeparam>
        /// <typeparam name="TConfigurator">Window configurator type.</typeparam>
        /// <param name="instance">Window instance.</param>
        /// <param name="configInstance">Window configurator instance.</param>
        /// <returns>Window owner entity.</returns>
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

        /// <summary>
        /// Shows a notification message that will disappear automatically after a given time.
        /// </summary>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification message text.</param>
        public void ShowNotification(string title, string message) =>
            this.ShowNotification(title, message, CoreResourcesIDs.Materials.Icons.info);

        /// <summary>
        /// Shows a notification message that will disappear automatically after a given time.
        /// </summary>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification message text.</param>
        /// <param name="icon">Notification icon material. This icon will be tinted following theming settings.</param>
        public void ShowNotification(string title, string message, Guid icon) =>
            this.notificationsHub.AddNotification(new NotificationConfiguration
            {
                IconMaterial = icon,
                Title = title,
                Text = message,
            });

        internal void Load()
        {
            this.CreateAlertDialogInstance();
            this.CreateConfirmDialogInstance();
            this.CreateNotificationsHub();
        }

        private void CreateAlertDialogInstance()
        {
            this.alertDialog = new AlertDialog();
            var owner = this.CreateDialogAux(this.alertDialog, string.Empty, string.Empty);
            this.entityManager.Add(owner);

            var contentPrefab = this.GetBaseDialogPrefab();
            var configurator = owner.FindComponent<DialogConfigurator>();
            configurator.Content = contentPrefab.Instantiate();

            var acceptConfig = this.alertDialog.AcceptOption.Configuration;
            acceptConfig.Plate = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.SecondaryColor4);

            owner.IsEnabled = false;
        }

        private void CreateConfirmDialogInstance()
        {
            this.confirmationDialog = new ConfirmationDialog();
            var owner = this.CreateDialogAux(this.confirmationDialog, string.Empty, string.Empty);
            this.entityManager.Add(owner);

            var contentPrefab = this.GetBaseDialogPrefab();
            var configurator = owner.FindComponent<DialogConfigurator>();
            configurator.Content = contentPrefab.Instantiate();

            var cancelConfig = this.confirmationDialog.CancelOption.Configuration;
            cancelConfig.Plate = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.SecondaryColor4);

            var acceptConfig = this.confirmationDialog.AcceptOption.Configuration;
            acceptConfig.Plate = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.SecondaryColor3);

            owner.IsEnabled = false;
        }

        private void CreateNotificationsHub()
        {
            var prefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Windows.NotificationHub_weprefab);
            var hubEntity = prefab.Instantiate();
            hubEntity.Name = "Notifications-Hub";

            this.notificationsHub = hubEntity.FindComponent<NotificationsHub>();
            this.entityManager.Add(hubEntity);
        }

        private Entity CreateDialogAux<TDialog>(TDialog dialog, string title, string text)
            where TDialog : Dialog
        {
            const float DialogWidth = 0.2f;
            const float DialogHeight = 0.11f;

            var dialogConfigurator = new DialogConfigurator
            {
                Title = title,
                LocalizedText = () => text,
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

            return owner;
        }

        private bool CloseAllDialogs()
        {
            bool anyOpen = false;

            if (this.confirmationDialog.IsOpened)
            {
                anyOpen = true;
                this.confirmationDialog.Close();
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
