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

using Xamarin.Forms;
using Xamarin.Essentials;
using Unity;
using Unity.Lifetime;
using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;
using GlitchedPolygons.ExtensionMethods;
using GlitchedPolygons.Services.MethodQ;
using GlitchedPolygons.Services.JwtService;
using GlitchedPolygons.Services.CompressionUtility;
using GlitchedPolygons.Services.Cryptography.Symmetric;
using GlitchedPolygons.Services.Cryptography.Asymmetric;
using GlitchedPolygons.GlitchedEpistle.Client.Models;
using GlitchedPolygons.GlitchedEpistle.Client.Models.DTOs;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Constants;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Views;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.ViewModels;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.PubSubEvents;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Totp;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Factories;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Services.Localization;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources.Themes;
using GlitchedPolygons.GlitchedEpistle.Client.Mobile.Resources.Themes.Base;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Logging;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Settings;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Users;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.Convos;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Web.ServerHealth;
using GlitchedPolygons.GlitchedEpistle.Client.Services.Cryptography.Messages;
using GlitchedPolygons.GlitchedEpistle.Client.Utilities;
using Plugin.SimpleAudioPlayer;
using Prism.Events;

namespace GlitchedPolygons.GlitchedEpistle.Client.Mobile
{
    public partial class App : Application
    {
        /// <summary>
        /// The client version number.
        /// </summary>
        public static string Version => Assembly.GetCallingAssembly()?.GetName()?.Version?.ToString();
        
        /// <summary>
        /// The app's central audio player unit.
        /// </summary>
        public static ISimpleAudioPlayer AudioPlayer => audioPlayer;

        /// <summary>
        /// Gets the currently active GUI theme (appearance of the app).
        /// </summary>
        /// <value>The current theme.</value>
        public string CurrentTheme { get; private set; } = Themes.DARK_THEME;

        /// <summary>
        /// Dependency injection container.
        /// </summary>
        private readonly IUnityContainer container = new UnityContainer();

        private static readonly ISimpleAudioPlayer audioPlayer = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();

        private readonly User user;
        private readonly ILogger logger;
        private readonly IMethodQ methodQ;
        private readonly IAppSettings appSettings;
        private readonly IUserSettings userSettings;
        private readonly IUserService userService;
        private readonly IEventAggregator eventAggregator;
        private readonly IViewModelFactory viewModelFactory;
        private readonly IServerConnectionTest connectionTest;
        private readonly IConvoPasswordProvider convoPasswordProvider;

        private ulong? scheduledAuthRefresh;

        /// <summary>
        /// Shorthand for <c>Device.BeginInvokeOnMainThread(action);</c>
        /// </summary>
        /// <param name="action">What you want to execute on the UI thread.</param>
        protected static void ExecUI(Action action)
        {
            if (action != null) Device.BeginInvokeOnMainThread(action);
        }
        
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
            container.RegisterType<ITotpProvider, TotpProvider>();
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

            user = container.Resolve<User>();
            logger = container.Resolve<ILogger>();
            methodQ = container.Resolve<IMethodQ>();
            userService = container.Resolve<IUserService>();
            appSettings = container.Resolve<IAppSettings>();
            userSettings = container.Resolve<IUserSettings>();
            eventAggregator = container.Resolve<IEventAggregator>();
            viewModelFactory = container.Resolve<IViewModelFactory>();
            connectionTest = container.Resolve<IServerConnectionTest>();
            convoPasswordProvider = container.Resolve<IConvoPasswordProvider>();

            // Subscribe to important IEventAggregator PubSubEvents.
            eventAggregator.GetEvent<LogoutEvent>().Subscribe(Logout);
            eventAggregator.GetEvent<ClickedRegisterButtonEvent>().Subscribe(ShowRegistrationPage);
            eventAggregator.GetEvent<ClickedConfigureServerUrlButtonEvent>().Subscribe(ShowConfigServerUrlPage);
            eventAggregator.GetEvent<UserCreationSucceededEvent>().Subscribe(OnUserCreationSuccessful);
            eventAggregator.GetEvent<UserCreationVerifiedEvent>().Subscribe(()=>ShowLoginPage(false));
            eventAggregator.GetEvent<LoginSucceededEvent>().Subscribe(OnLoginSuccessful);
            eventAggregator.GetEvent<JoinedConvoEvent>().Subscribe(OnJoinedConvo);
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
                if (theme.NullOrEmpty()) logger.LogError($"{nameof(App)}::{nameof(ChangeTheme)}: Attempted to change theme with a null or empty theme identifier parameter. Please only provide a valid theme parameter to this method!");
#endif
                return false;
            }

            ResourceDictionary themeDictionary = null;

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

        protected override void OnStart()
        {
            ILocalization localization = DependencyService.Get<ILocalization>();

            ChangeTheme(appSettings["Theme", Themes.DARK_THEME]);

            string lang = appSettings["Language"];
            if (lang.NullOrEmpty())
            {
                lang = appSettings["Language"] = localization.GetCurrentCultureInfo()?.ToString() ?? "en";
            }

            localization.SetCurrentCultureInfo(new CultureInfo(lang));

            Logout();

            Task.Run(async () =>
            {
                bool serverUrlConfigured = false;
                string url = appSettings.ServerUrl;

                if (url.NotNullNotEmpty())
                {
                    UrlUtility.SetEpistleServerUrl(url);
                    serverUrlConfigured = await connectionTest.TestConnection();
                }

                ExecUI(delegate
                {
                    if (serverUrlConfigured)
                    {
                        ShowLoginPage();
                    }
                    else
                    {
                        ExecUI(ShowConfigServerUrlPage);
                    }
                });
            });
        }

        protected override void OnSleep()
        {
            //nop
        }

        protected override void OnResume()
        {
            StopAuthRefreshingCycle();
            RefreshAuth();
            StartAuthRefreshingCycle();
        }

        private void StopAuthRefreshingCycle()
        {
            if (scheduledAuthRefresh.HasValue)
            {
                methodQ.Cancel(scheduledAuthRefresh.Value);
                scheduledAuthRefresh = null;
            }
        }

        private void StartAuthRefreshingCycle()
        {
            scheduledAuthRefresh = methodQ.Schedule(RefreshAuth, TimeSpan.FromMinutes(13.37));
        }

        private async void RefreshAuth()
        {
            if (user?.Token is null)
            {
                return;
            }
            
            string freshToken = await userService.RefreshAuthToken(user.Id, user.Token.Item2);
                
            if (freshToken.NotNullNotEmpty())
            {
                user.Token = new Tuple<DateTime, string>(DateTime.UtcNow, freshToken);
            }
            else
            {
                Logout();
            }
        }

        private void OnLoginSuccessful()
        {
            StopAuthRefreshingCycle();
            StartAuthRefreshingCycle();
            ShowConvosPage();
        }

        private void Logout()
        {
            // If we're already logging in, nvm.
            if (MainPage is LoginPage)
            {
                return;
            }

            // Nullify all stored user tokens, passwords and such...
            user.Token = null;
            user.PasswordSHA512 = user.PublicKeyPem = user.PrivateKeyPem = null;
            
            StopAuthRefreshingCycle();

            //convoProvider = null;
            convoPasswordProvider?.Clear();
            
            // Show the login page.
            ShowLoginPage(false);
        }

        private void OnUserCreationSuccessful(UserCreationResponseDto userCreationResponseDto)
        {
            var viewModel = viewModelFactory.Create<UserCreationSuccessfulViewModel>();

            SecureStorage.SetAsync("totp:" + userCreationResponseDto.Id, userCreationResponseDto.TotpSecret);

            viewModel.Secret = userCreationResponseDto.TotpSecret;
            viewModel.QR = $"otpauth://totp/GlitchedEpistle:{userCreationResponseDto.Id}?secret={userCreationResponseDto.TotpSecret}";
            viewModel.BackupCodes = userCreationResponseDto.TotpEmergencyBackupCodes;

            MainPage = new UserCreationSuccessfulView {BindingContext = viewModel};
        }
        
        private async void OnJoinedConvo(Convo convo)
        {
            var viewModel = viewModelFactory.Create<ActiveConvoViewModel>();
            viewModel.ActiveConvo = convo;

            var view = new ActiveConvoPage {BindingContext = viewModel};
            await Application.Current.MainPage.Navigation.PushModalAsync(view);
        }

        private void ShowLoginPage(bool autoPromptForFingerprint = true)
        {
            var viewModel = viewModelFactory.Create<LoginViewModel>();
            
            viewModel.UserId = appSettings.LastUserId;
            viewModel.AutoPromptForFingerprint = autoPromptForFingerprint;

            MainPage = new LoginPage {BindingContext = viewModel};
        }

        private void ShowRegistrationPage()
        {
            var viewModel = viewModelFactory.Create<RegisterViewModel>();
            MainPage = new RegisterPage {BindingContext = viewModel};
        }

        private void ShowConfigServerUrlPage()
        {
            var viewModel = viewModelFactory.Create<ServerUrlViewModel>();
            MainPage = new ServerUrlPage {BindingContext = viewModel};
        }

        private void ShowConvosPage()
        {
            var viewModel = viewModelFactory.Create<ConvosViewModel>();
            MainPage = new ConvosPage {BindingContext = viewModel};
        }
    }
}