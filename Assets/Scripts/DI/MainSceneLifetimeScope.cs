using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;
using R3;

using Domain.Interfaces;
using Domain.ValueObjects;
using Application.UseCases;
using Infrastructure.Services;
using Infrastructure.Repositories;
using Presentation.Presenter;
using Presentation.View;
using Presentation.Interfaces;
using Presentation.ScriptableObjects;
using Presentation.Factories;
using Presentation.Events;

namespace DI
{
    public class MainSceneLifetimeScope : LifetimeScope
    {
        // コアコンポーネント
        [SerializeField] private ApiSettings apiSettings;
        [SerializeField] private AvatarAnimationSettingsSO avatarAnimationDirectionSettings;
        [SerializeField] private AnimationNameSettingsSO avatarAnimationNameSettings;

        // アバターカスタマイズ関連
        [SerializeField] private CameraView cameraView;
        [SerializeField] private AvatarSystemPage avatarSystemPage;

        // ルームカスタマイズ関連
        [SerializeField] private RoomSystemPage roomSystemPage;

        // ログイン関連
        [SerializeField] private LoginPage loginPage;
        [SerializeField] private LoginModal loginModal;

        protected override void Configure(IContainerBuilder builder)
        {
            // イベントバス機能のためのSubject<AvatarReadyEvent>を登録
            builder.RegisterInstance(new Subject<AvatarReadyEvent>()).AsSelf();

            // インフラストラクチャレイヤーの登録
            RegisterInfrastructure(builder);

            // アプリケーションレイヤーの登録
            RegisterApplication(builder);

            // プレゼンテーションレイヤーの登録
            RegisterPresentation(builder);
        }

        private void RegisterInfrastructure(IContainerBuilder builder)
        {
            // ScriptableObjectの登録
            builder.RegisterInstance(apiSettings);
            builder.RegisterInstance(avatarAnimationDirectionSettings);
            builder.RegisterInstance(avatarAnimationNameSettings);

            builder.Register(resolver =>
            {
                var settings = resolver.Resolve<ApiSettings>();
                return new ApiConnections(
                    settings.baseUrl,
                    settings.maxRetries,
                    settings.initialInterval,
                    settings.timeoutSeconds,
                    settings.appVersion,
                    settings.masterDataVersion
                );
            }, Lifetime.Singleton).As<ApiConnections>();

            // サービスの登録
            builder.Register<IAuthManagerService, FirebaseAuthManagerService>(Lifetime.Singleton);
            builder.Register<ITokenManagerService, FirebaseTokenManagerService>(Lifetime.Singleton);
            builder.Register<IVersionProviderService, VersionProviderService>(Lifetime.Singleton);
            builder.Register<IRequestSenderService, HttpApiClientService>(Lifetime.Singleton);
            builder.Register<IModelValidatorService, ModelValidatorService>(Lifetime.Singleton);
            builder.Register<ILogService, UnityLogService>(Lifetime.Singleton);
            builder.Register<IAssetLoader, AddressableAssetLoader>(Lifetime.Singleton);
            builder.Register<IAvatarCustomizationService, AvatarCustomizationService>(Lifetime.Singleton);
            builder.Register<IAvatarSkinColorService, AvatarSkinColorService>(Lifetime.Singleton);
            builder.Register<IAvatarHairColorService, AvatarHairColorService>(Lifetime.Singleton);

            // ファイルベースのリポジトリ
            string runtimeFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "avatar_customization.json");
            builder.Register<IAvatarParameterRepository>(resolver =>
            {
                return new JsonAvatarParameterRepository(runtimeFilePath);
            }, Lifetime.Singleton);
        }

        private void RegisterApplication(IContainerBuilder builder)
        {
            // UseCaseの登録
            builder.Register<LoginUseCase>(Lifetime.Singleton);
            builder.Register<AvatarCustomizationUseCase>(Lifetime.Singleton);
            builder.Register<AvatarLifecycleUseCase>(Lifetime.Singleton)
                .WithParameter<ILogService>(resolver => resolver.Resolve<ILogService>());
        }

        private void RegisterPresentation(IContainerBuilder builder)
        {
            // Register Factory
            builder.Register<IAvatarAnimationControllerFactory, AvatarAnimationControllerFactory>(Lifetime.Singleton);

            // PageManagerをIPageManagerとIStartable両方として登録
            builder.Register<PageManager>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            // InitializationPresenterをIStartableとして登録
            builder.Register<InitializationPresenter>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // AvatarPresenterをIStartableとして登録
            builder.Register<AvatarPresenter>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // RoomSystemPagePresenterをIStartableとして登録
            builder.Register<RoomSystemPagePresenter>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // LoginPresenterをIStartableとして登録
            builder.Register<LoginPresenter>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            // ビューの登録
            if (loginPage != null)
                builder.RegisterComponent(loginPage);

            if (loginModal != null)
                builder.RegisterComponent(loginModal);

            if (avatarSystemPage != null)
            {
                builder.RegisterComponent(avatarSystemPage);

                // AvatarSystemPageからUIDocumentを取得して登録
                avatarSystemPage.TryGetComponent<UIDocument>(out var uiDocument);
                builder.RegisterInstance(uiDocument);

                if (cameraView != null)
                    builder.RegisterComponent(cameraView);

                // InputHandlerの登録
                builder.Register<IInputHandler, UnityInputHandler>(Lifetime.Singleton);
            }
            if (roomSystemPage != null)
                builder.RegisterComponent(roomSystemPage);
        }
    }
}
