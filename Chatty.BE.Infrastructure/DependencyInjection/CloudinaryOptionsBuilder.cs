using Chatty.BE.Infrastructure.Config;
using Chatty.BE.Infrastructure.Config.Upload;
using Microsoft.Extensions.Configuration;

namespace Chatty.BE.Infrastructure.DependencyInjection;

internal static class CloudinaryOptionsBuilder
{
    public static CloudinaryOptions Build(IConfiguration configuration) =>
        new()
        {
            CloudName = ConfigurationHelpers.GetString(
                configuration,
                "CLOUDINARY_CLOUD_NAME",
                "Cloudinary:CloudName",
                required: false
            ),
            ApiKey = ConfigurationHelpers.GetString(
                configuration,
                "CLOUDINARY_API_KEY",
                "Cloudinary:ApiKey",
                required: false
            ),
            ApiSecret = ConfigurationHelpers.GetString(
                configuration,
                "CLOUDINARY_API_SECRET",
                "Cloudinary:ApiSecret",
                required: false
            ),
            Folder = ConfigurationHelpers.GetString(
                configuration,
                "CLOUDINARY_FOLDER",
                "Cloudinary:Folder",
                required: false
            ),
        };
}
