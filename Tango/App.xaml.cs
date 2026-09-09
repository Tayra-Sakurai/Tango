// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.DependencyInjection;
using Maizuru.Contexts;
using Maizuru.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.Storage;
using OpenAI.Embeddings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tango
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            Ioc.Default.ConfigureServices(GetService());
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();

            AppNotificationManager.Default.NotificationInvoked += Default_NotificationInvoked;
            AppNotificationManager.Default.Register();

            AppActivationArguments activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

            if (activatedEventArgs.Kind == ExtendedActivationKind.AppNotification)
            {
                AppNotificationActivatedEventArgs appNotificationActivatedEventArgs = (AppNotificationActivatedEventArgs)activatedEventArgs.Data;
                await HandleNotification(appNotificationActivatedEventArgs);
            }
            else
            {
                await WindowActivate();
            }
        }

        private async void Default_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            await HandleNotification(args);
        }

        private static IServiceProvider GetService()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddEmbeddingGenerator(
                new EmbeddingClient(
                    model: "models/gemini-embedding-2",
                    credential: new System.ClientModel.ApiKeyCredential(Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? throw new NotImplementedException()),
                    options: new()
                    {
                        Endpoint = new("https://generativelanguage.googleapis.com/v1beta/openai"),
                    })
                .AsIEmbeddingGenerator());
            services.AddDbContextFactory<MaizuruContext>(
                options => options
                .UseSqlite($"Data Source={System.IO.Path.Join(Microsoft.Windows.Storage.ApplicationData.GetDefault().LocalFolder.Path, "Maizuru.db")}"));
            services.AddTransient<CategoriesViewModel>();
            services.AddTransient<CategoryViewModel>();
            services.AddTransient<PaymentMethodsViewModel>();
            services.AddTransient<PaymentMethodViewModel>();
            services.AddTransient<ItemsViewModel>();
            services.AddTransient<ItemViewModel>();

            return services.BuildServiceProvider();
        }

        private async Task HandleNotification(AppNotificationActivatedEventArgs args)
        {
            string action = args.Arguments.ContainsKey("action") ? args.Arguments["action"] : "(none)";
            string pageName = args.Arguments.ContainsKey("page") ? args.Arguments["page"] : "(none)";

            _window!.DispatcherQueue.TryEnqueue(async () =>
            {
                switch (action)
                {
                    case "Background":
                        if (!_window.Visible)
                            Application.Current.Exit();
                        break;

                    default:
                        await WindowActivate();
                        ((MainWindow)_window).TakeToNotificationNavigatedPage(pageName);
                        break;
                }
            });
        }

        private async Task WindowActivate()
        {
            _window!.Activate();

            Uri iconUri = new("ms-appx:///Assets/Icons/icon-copy-_1_.ico");

            StorageFile? storageFile = null;
            try
            {
                storageFile = await StorageFile.GetFileFromApplicationUriAsync(iconUri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                if (storageFile != null)
                {
                    _window.AppWindow.SetIcon(storageFile.Path);
                }

                if (_window.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }

                IDbContextFactory<MaizuruContext> factory = Ioc.Default.GetRequiredService<IDbContextFactory<MaizuruContext>>();
                using MaizuruContext context = await factory.CreateDbContextAsync();
                await context.Database.MigrateAsync();
            }
        }
    }
}
