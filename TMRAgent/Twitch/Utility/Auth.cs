using System;
using System.Threading;
using System.Threading.Tasks;

namespace TMRAgent.Twitch.Utility
{
    public class Auth
    {
        private readonly Object Lockable = new();

        public enum AuthType
        {
            TwitchChat,
            PubSub,
            LiveStreamMonitorService
        }

        public bool HasTokenExpired(AuthType authType)
        {
            switch (authType)
            {
                case AuthType.TwitchChat:
                    return DateTime.Now.ToUniversalTime() >=
                           ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry;
                case AuthType.PubSub:
                    return DateTime.Now.ToUniversalTime() >=
                           ConfigurationHandler.Instance.Configuration.PubSub.TokenExpiry;
                default:
                    throw new ArgumentOutOfRangeException(nameof(authType), authType, null);
            }
        }

        public bool TestAuth(AuthType authType)
        {
            lock (Lockable)
            {
                var api = new TwitchLib.Api.TwitchAPI
                {
                    Settings =
                    {
                        ClientId = ConfigurationHandler.Instance.Configuration.AppClientId
                    }
                };

                var authToken = GetAuthTokenFromAuthType(authType);

                switch (authType)
                {
                    case AuthType.TwitchChat:
                    case AuthType.LiveStreamMonitorService:
                        if (ConfigurationHandler.Instance.Configuration.TwitchChat.RefreshToken == null ||
                            ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry == null ||
                            HasTokenExpired(authType))
                        {
                            Util.Log(
                                $"[OAuthChecker] {Enum.GetName(authType)} Token has expired, marking it as dirty! (exp: {ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry})",
                                Util.LogLevel.Info);
                            return false;
                        }

                        break;

                    case AuthType.PubSub:
                        if (ConfigurationHandler.Instance.Configuration.PubSub.RefreshToken == null ||
                            ConfigurationHandler.Instance.Configuration.PubSub.TokenExpiry == null ||
                            HasTokenExpired(authType))
                        {
                            Util.Log(
                                $"[OAuthChecker] {Enum.GetName(authType)} Token has expired, marking it as dirty! (exp: {ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry})",
                                Util.LogLevel.Info);
                            return false;
                        }

                        break;
                }

                try
                {
                    return api.Auth.ValidateAccessTokenAsync(authToken).WaitAsync(CancellationToken.None).Result !=
                           null;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool Validate(AuthType validateOn, bool AutomaticRefresh)
        {
            lock (Lockable)
            {
                Util.Log($"[OAuthChecker] Checking OAuth Tokens Validity for {Enum.GetName(validateOn)}",
                    Util.LogLevel.Info);
                var validationResult = TestAuth(validateOn);

                if (!validationResult)
                {
                    Util.Log($"[OAuthChecker] Failed to validate OAuth Token for {Enum.GetName(validateOn)}",
                        Util.LogLevel.Warn,
                        ConsoleColor.Yellow);

                    if (AutomaticRefresh)
                    {
                        if (!RefreshToken(validateOn).GetAwaiter().GetResult())
                        {
                            throw new Exception(
                                $"Unable to refresh Auth Token for {Enum.GetName(validateOn)}");
                        }

                        return true;
                    }

                    return false;
                }
                else
                {
                    Util.Log(
                        $"[OAuthChecker] Successfully validated OAuth Tokens for {Enum.GetName(validateOn)}, Expiry: {ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry}/UTC",
                        Util.LogLevel.Info);
                    return true;
                }
            }
        }

        public bool Validate(AuthType validateOn)
        {
            return Validate(validateOn, false);
        }

        public async Task<bool> RefreshToken(AuthType authType)
        {
            // Get our refresh token
            var refreshToken = GetRefreshTokenFromAuthType(authType);

            var config = ConfigurationHandler.Instance.Configuration;

            var api = new TwitchLib.Api.TwitchAPI();
            try
            {
                var refreshRequest =
                    await api.Auth.RefreshAuthTokenAsync(refreshToken, config.ClientSecret, config.AppClientId);

                if (refreshRequest != null)
                {
                    switch (authType)
                    {
                        case AuthType.TwitchChat:
                        case AuthType.LiveStreamMonitorService:
                            config.TwitchChat.AuthToken = refreshRequest.AccessToken;
                            config.TwitchChat.RefreshToken = refreshRequest.RefreshToken;
                            config.TwitchChat.TokenExpiry =
                                DateTime.Now.ToUniversalTime().AddSeconds(refreshRequest.ExpiresIn);
                            break;
                        case AuthType.PubSub:
                            config.PubSub.AuthToken = refreshRequest.AccessToken;
                            config.PubSub.RefreshToken = refreshRequest.RefreshToken;
                            config.PubSub.TokenExpiry =
                                DateTime.Now.ToUniversalTime().AddSeconds(refreshRequest.ExpiresIn);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(authType), authType, null);
                    }

                    Util.Log($"[OAuthRefresh] Successfully refreshed OAuth Tokens for {Enum.GetName(authType)}",
                        Util.LogLevel.Info);

                    ConfigurationHandler.Instance.Save();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Util.Log(
                    $"[OAuthRefresh] Error while handling an OAuth Token Refresh for {Enum.GetName(authType)}: {ex.Message}",
                    Util.LogLevel.Error, ConsoleColor.Red);
                return false;
            }
            return false;
        }

        private string GetAuthTokenFromAuthType(Auth.AuthType authType)
        {
            var authToken = "";
            switch (authType)
            {
                case Auth.AuthType.TwitchChat:
                    authToken = ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken;
                    break;

                case Auth.AuthType.PubSub:
                    authToken = ConfigurationHandler.Instance.Configuration.PubSub.AuthToken;
                    break;
            }

            return authToken;
        }

        private string GetRefreshTokenFromAuthType(Auth.AuthType authType)
        {
            var refreshToken = "";
            switch (authType)
            {
                case Auth.AuthType.TwitchChat:
                case Auth.AuthType.LiveStreamMonitorService:
                    refreshToken = ConfigurationHandler.Instance.Configuration.TwitchChat.RefreshToken;
                    break;

                case Auth.AuthType.PubSub:
                    refreshToken = ConfigurationHandler.Instance.Configuration.PubSub.RefreshToken;
                    break;
            }

            return refreshToken!;
        }
    }
}
