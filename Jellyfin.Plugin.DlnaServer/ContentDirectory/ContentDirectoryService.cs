using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.DlnaServer.Configuration;
using Jellyfin.Plugin.DlnaServer.Service;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DlnaServer.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ContentDirectoryService" />.
    /// </summary>
    public class ContentDirectoryService : BaseService, IContentDirectory
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly IDlnaManager _dlna;
        private readonly DlnaServerConfiguration _config;
        private readonly IUserManager _userManager;
        private readonly ILocalizationManager _localization;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IUserViewManager _userViewManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ITVSeriesManager _tvSeriesManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentDirectoryService"/> class.
        /// </summary>
        /// <param name="dlna">The <see cref="IDlnaManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="config">The <see cref="IServerConfigurationManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="userManager">The <see cref="IUserManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="httpClient">The <see cref="IHttpClientFactory"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="localization">The <see cref="ILocalizationManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="userViewManager">The <see cref="IUserViewManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
        public ContentDirectoryService(
            IDlnaManager dlna,
            IUserDataManager userDataManager,
            IImageProcessor imageProcessor,
            ILibraryManager libraryManager,
            DlnaServerConfiguration config,
            IUserManager userManager,
            ILogger logger,
            IHttpClientFactory httpClient,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IUserViewManager userViewManager,
            IMediaEncoder mediaEncoder,
            ITVSeriesManager tvSeriesManager)
            : base(logger, httpClient)
        {
            _dlna = dlna;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
            _config = config;
            _userManager = userManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _userViewManager = userViewManager;
            _mediaEncoder = mediaEncoder;
            _tvSeriesManager = tvSeriesManager;
        }

        /// <summary>
        /// Gets the system id. (A unique id which changes on when our definition changes.)
        /// </summary>
        private static int SystemUpdateId
        {
            get
            {
                var now = DateTime.UtcNow;

                return now.Year + now.DayOfYear + now.Hour;
            }
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            var response = ContentDirectoryXmlBuilder.GetXml();
            if (DlnaServerPlugin.Instance!.Configuration.EnableDebugLog)
            {
                Logger.LogDebug(response);
            }

            return response;
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var profile = _dlna.GetProfile(request.Headers) ?? _dlna.GetDefaultProfile();

            var serverAddress = request.RequestedUrl.Substring(0, request.RequestedUrl.IndexOf("/dlna", StringComparison.OrdinalIgnoreCase));

            var user = GetUser();

            if (user == null)
            {
                throw new ArgumentNullException($"Unable to extract User details from {_config.DlnaServerName}");
            }

            return new ControlHandler(
                Logger,
                _libraryManager,
                profile,
                serverAddress,
                null,
                _imageProcessor,
                _userDataManager,
                user,
                SystemUpdateId,
                _config,
                _localization,
                _mediaSourceManager,
                _userViewManager,
                _mediaEncoder,
                _tvSeriesManager)
                .ProcessControlRequestAsync(request);
        }

        /// <summary>
        /// Get the user to use for DLNA.
        /// </summary>
        /// <returns>The <see cref="User"/>.</returns>
        private User? GetUser()
        {
            var userId = _config.DefaultUserId;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = _userManager.GetUserById(Guid.Parse(userId));

                if (user != null)
                {
                    return user;
                }
            }

            foreach (var user in _userManager.Users)
            {
                if (user.HasPermission(PermissionKind.IsAdministrator))
                {
                    return user;
                }
            }

            return _userManager.Users.FirstOrDefault();
        }
    }
}
