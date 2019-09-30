﻿/*
    Glitched Epistle - Mobile Client
    Copyright (C) 2019 Raphael Beck

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Xamarin.Forms;

using Unity;
using Unity.Lifetime;

using System;
using System.IO;
using System.Reflection;

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.Services.JwtService;
using GlitchedPolygons.Services.CompressionUtility;
using GlitchedPolygons.Services.Cryptography.Symmetric;
using GlitchedPolygons.Services.Cryptography.Asymmetric;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Constants;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources.Themes;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources.Themes.Base;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.ServerHealth;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Cryptography.Messages;

using Prism.Events;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile
{
    public partial class App : Application
    {
        /// <summary>
        /// The client version number.
        /// </summary>
        public static string Version => Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();

        /// <summary>
        /// Gets the currently active GUI theme (appearance of the app).
        /// </summary>
        /// <value>The current theme.</value>
        public string CurrentTheme { get; private set; } = Themes.DARK_THEME;

        /// <summary>
        /// Dependency injection container.
        /// </summary>
        private readonly IUnityContainer container = new UnityContainer();

        public App()
        {
            InitializeComponent();
            Directory.CreateDirectory(Paths.ROOT_DIRECTORY);

            DependencyService.Register<MockDataStore>();

            // Register transient types:
            container.RegisterType<JwtService>();
            container.RegisterType<IUserService, UserService>();
            container.RegisterType<IConvoService, ConvoService>();
            container.RegisterType<ICompressionUtility, GZipUtility>();
            container.RegisterType<ICompressionUtilityAsync, GZipUtilityAsync>();
            container.RegisterType<IAsymmetricKeygenRSA, AsymmetricKeygenRSA>();
            container.RegisterType<ISymmetricCryptography, SymmetricCryptography>();
            container.RegisterType<IAsymmetricCryptographyRSA, AsymmetricCryptographyRSA>();
            container.RegisterType<IMessageCryptography, MessageCryptography>();
            container.RegisterType<IServerConnectionTest, ServerConnectionTest>();
            container.RegisterType<IMessageSender, MessageSender>();
            container.RegisterType<ILoginService, LoginService>();
            container.RegisterType<IPasswordChanger, PasswordChanger>();
            container.RegisterType<IRegistrationService, RegistrationService>();

            // Register IoC singletons:
            container.RegisterType<User>(new ContainerControlledLifetimeManager()); // This is the application's user.
            container.RegisterType<IMethodQ, MethodQ>(new ContainerControlledLifetimeManager());
            container.RegisterType<ILogger, TextLogger>(new ContainerControlledLifetimeManager());
            container.RegisterType<IAppSettings, AppSettingsJson>(new ContainerControlledLifetimeManager());
            container.RegisterType<IUserSettings, UserSettingsJson>(new ContainerControlledLifetimeManager());
            container.RegisterType<IEventAggregator, EventAggregator>(new ContainerControlledLifetimeManager());
            container.RegisterType<IViewModelFactory, ViewModelFactory>(new ContainerControlledLifetimeManager());
            container.RegisterType<IConvoPasswordProvider, ConvoPasswordProvider>(new ContainerControlledLifetimeManager());
            container.RegisterType<IMessageFetcher, MessageFetcher>(new ContainerControlledLifetimeManager());
        }

        protected override void OnStart()
        {
            IAppSettings appSettings = container.Resolve<IAppSettings>();
            ILocalization localization = DependencyService.Get<ILocalization>();

            ChangeTheme(appSettings["Theme", Themes.DARK_THEME]);

            string lang = appSettings["Language"];
            if (lang.NullOrEmpty())
            {
                lang = appSettings["Language"] = localization.GetCurrentCultureInfo()?.ToString() ?? "en";
            }

            localization.SetCurrentCultureInfo(new System.Globalization.CultureInfo(lang));

            var vm = Resolve<LoginViewModel>();
            vm.UserId = appSettings.LastUserId;
            MainPage = new LoginPage { BindingContext = vm };
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        /// <summary>
        /// Resolves a <c>class</c> instance through the IoC container. Only call this method from factories!
        /// </summary>
        /// <typeparam name="T">The class to resolve/create.</typeparam>
        /// <returns>Instantiated/resolved class.</returns>
        public T Resolve<T>() where T : class
        {
            return container?.Resolve<T>();
        }

        /// <summary>
        /// Changes the application GUI's theme.
        /// </summary>
        /// <param name="theme">The theme to switch to.</param>
        /// <returns>Whether the theme change occurred or not (e.g. in case of changing to a theme that's already active, or in case of a failure, this method returns <c>false</c>).</returns>
        public bool ChangeTheme(string theme)
        {
            if (theme.NullOrEmpty() || theme.Equals(CurrentTheme))
            {
#if DEBUG
                if (theme.NullOrEmpty()) throw new ArgumentException($"{nameof(App)}::{nameof(ChangeTheme)}: Attempted to change theme with a null or empty theme identifier parameter. Please only provide a valid theme parameter to this method!", nameof(theme));
#else
                return false;
#endif
            }

            ResourceDictionary themeDictionary = null;
            ILogger logger = container.Resolve<ILogger>();

            switch (theme)
            {
                case Themes.DARK_THEME:
                    themeDictionary = new DarkTheme();
                    break;
                case Themes.LIGHT_THEME:
                    themeDictionary = new LightTheme();
                    break;
                case Themes.OLED_THEME:
                    themeDictionary = new OLEDTheme();
                    break;
            }

            if (themeDictionary is null)
            {
                logger?.LogWarning($"Theme \"{theme}\" couldn't be found/does not exist.");
                return false;
            }

            try
            {
                Resources.Clear();
                Resources.Add(themeDictionary);
                Resources.Add(new Theme());
                Resources.Add(new ControlTemplates());

                CurrentTheme = theme;
                return true;
            }
            catch (Exception e)
            {
                logger?.LogWarning($"Theme \"{themeDictionary}\" couldn't be applied. Reverting to default theme... Thrown exception: {e.ToString()}");
                return false;
            }
        }
    }
}
