using System;
using System.Threading;
using System.Threading.Tasks;

namespace TMRAgent.Twitch
{
    public class Auth
    {

        public enum AuthType
        {
            TwitchChat,
            PubSub
        }

        public bool TestAuth(AuthType authType)
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
                    if (ConfigurationHandler.Instance.Configuration.TwitchChat.RefreshToken == null ||
                        ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry == null ||
                        DateTime.Now.ToUniversalTime() >=
                        ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry)
                    {
                        return false;
                    }

                    break;

                case AuthType.PubSub:
                    if (ConfigurationHandler.Instance.Configuration.PubSub.RefreshToken == null ||
                        ConfigurationHandler.Instance.Configuration.PubSub.TokenExpiry == null ||
                        DateTime.Now.ToUniversalTime() >=
                        ConfigurationHandler.Instance.Configuration.PubSub.TokenExpiry)
                    {
                        return false;
                    }

                    break;
            }

            try
            {
                return api.Auth.ValidateAccessTokenAsync(authToken).WaitAsync(CancellationToken.None).Result != null;
            }
            catch (Exception)
            {
                return false;
            }
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
                            config.TwitchChat.AuthToken = refreshRequest.AccessToken;
                            config.TwitchChat.RefreshToken = refreshRequest.RefreshToken;
                            config.TwitchChat.TokenExpiry = DateTime.Now.ToUniversalTime().AddSeconds(refreshRequest.ExpiresIn);
                            break;
                        case AuthType.PubSub:
                            config.PubSub.AuthToken = refreshRequest.AccessToken;
                            config.PubSub.RefreshToken = refreshRequest.RefreshToken;
                            config.PubSub.TokenExpiry = DateTime.Now.ToUniversalTime().AddSeconds(refreshRequest.ExpiresIn);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(authType), authType, null);
                    }
                    
                    ConsoleUtil.WriteToConsole($"[OAuthRefresh] Successfully refreshed OAuth Tokens for {Enum.GetName(authType)}", ConsoleUtil.LogLevel.Info);

                    ConfigurationHandler.Instance.Save();
                    return true;
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[OAuthRefresh] Error while handling an OAuth Token Refresh for {Enum.GetName(authType)}: {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                return false;
            }

            return false;
        }

        private string GetAuthTokenFromAuthType(AuthType authType)
        {
            var authToken = "";
            switch (authType)
            {
                case AuthType.TwitchChat:
                    authToken = ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken;
                    break;

                case AuthType.PubSub:
                    authToken = ConfigurationHandler.Instance.Configuration.PubSub.AuthToken;
                    break;
            }

            return authToken;
        }

        private string GetRefreshTokenFromAuthType(AuthType authType)
        {
            var refreshToken = "";
            switch (authType)
            {
                case AuthType.TwitchChat:
                    refreshToken = ConfigurationHandler.Instance.Configuration.TwitchChat.RefreshToken;
                    break;

                case AuthType.PubSub:
                    refreshToken = ConfigurationHandler.Instance.Configuration.PubSub.RefreshToken;
                    break;
            }

            return refreshToken;
        }
    }
}
