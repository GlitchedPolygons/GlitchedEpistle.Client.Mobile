/*
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

using Unity;
using Unity.Lifetime;

using System;
using System.Reflection;

using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.Services.CompressionUtility;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;

using Xamarin.Forms;

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
        public string CurrentTheme { get; private set; } = "Dark";

        /// <summary>
        /// Dependency injection container.
        /// </summary>
        private readonly IUnityContainer container = new UnityContainer();

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();

            // Transient injections:
            container.RegisterType<ILogger, TextLogger>();
            container.RegisterType<ICompressionUtilityAsync, GZipUtilityAsync>();
            container.RegisterType<IViewModelFactory, ViewModelFactory>();

            // IoC singletons:
            container.RegisterType<User>(new ContainerControlledLifetimeManager());
            container.RegisterType<IMethodQ, MethodQ>(new ContainerControlledLifetimeManager());
        }

        protected override void OnStart()
        {
            // TODO: Load settings here.
            //       Call ILocalization.SetCurrentCultureInfo;
            //       If a custom language setting is found inside the config, use that as parameter. 
            //       Otherwise ILocalize.GetCurrentCultureInfo

            var vm = Resolve<LoginViewModel>();
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
                throw new ArgumentException($"{nameof(App)}::{nameof(ChangeTheme)}: Attempted to change theme with a null or empty theme identifier parameter. Please only provide a valid theme parameter to this method!", nameof(theme));
#else
                return false;
#endif
            }

            string path = null;
            ILogger logger = container.Resolve<ILogger>();

            switch (theme)
            {
                case "Dark":
                    path = "/Resources/Themes/DarkTheme.xaml";
                    break;
                case "Light":
                    path = "/Resources/Themes/LightTheme.xaml";
                    break;
                case "OLED":
                    path = "/Resources/Themes/OLEDTheme.xaml";
                    break;
            }

            if (path.NullOrEmpty())
            {
                logger?.LogWarning($"Theme \"{theme}\" couldn't be found/does not exist.");
                return false;
            }

            try
            {
                Resources.Clear();
                Resources.Add(new ResourceDictionary { Source = new Uri(path, UriKind.Relative) });
                Resources.Add(new ResourceDictionary { Source = new Uri("/Resources/Themes/Base/Theme.xaml", UriKind.Relative) });
                Resources.Add(new ResourceDictionary { Source = new Uri("/Resources/Themes/Base/ControlTemplates.xaml", UriKind.Relative) });

                CurrentTheme = theme;
                return true;
            }
            catch (Exception e)
            {
                logger?.LogWarning($"Theme \"{path}\" couldn't be applied. Reverting to default theme... Thrown exception: {e.ToString()}");
                return false;
            }
        }
    }
}
