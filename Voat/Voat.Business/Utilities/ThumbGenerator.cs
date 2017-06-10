#region LICENSE

/*
    
    Copyright(c) Voat, Inc.

    This file is part of Voat.

    This source file is subject to version 3 of the GPL license,
    that is bundled with this package in the file LICENSE, and is
    available online at http://www.gnu.org/licenses/gpl-3.0.txt;
    you may not use this file except in compliance with the License.

    Software distributed under the License is distributed on an
    "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express
    or implied. See the License for the specific language governing
    rights and limitations under the License.

    All Rights Reserved.

*/

#endregion LICENSE

using Kaliko.ImageLibrary;
using Kaliko.ImageLibrary.Scaling;
using OpenGraph_Net;
using System;

using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Voat.Common;
using Voat.Configuration;
using Voat.Data.Models;
using Voat.Utilities.Components;

namespace Voat.Utilities
{
    //interface for dealing with writing files 
    public interface IFileManager<K, C>
    {
        void Upload(K key, C content);

        string Uri(K key);

        bool Exists(K key);

        void Delete(K key);
    }

    public static class ThumbGenerator
    {
        private static string _destinationPathThumbs = null;
        private static string _destinationPathAvatars = null;

        public static string DestinationPathThumbs { get { return _destinationPathThumbs; } }

        public static string DestinationPathAvatars { get { return _destinationPathAvatars; } }

        static ThumbGenerator()
        {
            //CORE_PORT: HttpContext not available 
            throw new NotImplementedException("Core Port: HttpContext access");
            /*
            //For UI/API
            if (HttpContext.Current != null)
            {
                _destinationPathThumbs = HttpContext.Current.Server.MapPath("~/Storage/Thumbs");
                _destinationPathAvatars = HttpContext.Current.Server.MapPath("~/Storage/Avatars");
            }

            //For Unit Testing
            else
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _destinationPathThumbs = Path.Combine(baseDir, @"Storage\Thumbs");
                _destinationPathAvatars = Path.Combine(baseDir, @"Storage\Avatars");
                if (!Directory.Exists(_destinationPathThumbs))
                {
                    Directory.CreateDirectory(_destinationPathThumbs);
                }
                if (!Directory.Exists(_destinationPathAvatars))
                {
                    Directory.CreateDirectory(_destinationPathAvatars);
                }
            }
            */
        }

        // setup default thumb resolution
        private const int MaxHeight = 70;

        private const int MaxWidth = 70;

        // generate a thumbnail while removing transparency and preserving aspect ratio
        public static async Task<string> GenerateThumbFromImageUrl(string imageUrl, int timeoutInMilliseconds = 3000, bool purgeTempFile = true)
        {
            //TODO: Return NULL if file lenght is zero as thumbnail did not generate to local disk
            var randomFileName = GenerateRandomFilename();
            var tempPath = Path.Combine(DestinationPathThumbs, $"{randomFileName}.jpg");

            var request = WebRequest.Create(imageUrl);
            request.Timeout = timeoutInMilliseconds; //Putts: extended this from 300 mills
            using (var response = request.GetResponse())
            {
                    //CORE_PORT: KalikoImage not available 
                    throw new NotImplementedException("Core Port: KalikoImage access");
                    /*
                    var originalImage = new KalikoImage(response.GetResponseStream()) { BackgroundColor = Color.Black };
                    originalImage.Scale(new PadScaling(MaxWidth, MaxHeight)).SaveJpg(tempPath, 90);
                    */
                }

                if (FileSystemUtility.FileExists(tempPath, DestinationPathThumbs))
                {
                    // call upload to storage method if CDN config is enabled
                    if (VoatSettings.Instance.UseContentDeliveryNetwork)
                    {
                        await CloudStorageUtility.UploadBlobToStorageAsync(tempPath, "thumbs");
                        if (purgeTempFile)
                        {
                            // delete local file after uploading to CDN
                            File.Delete(tempPath);
                        }
                    }
                    return Path.GetFileName(tempPath);
                }
                else
                {
                    return null;
                }

            }

            //CORE_PORT: Image handling has changed in core, commenting out method until we know what we are doing
            public static async Task<bool> GenerateAvatar(object inputImage, string userName, string mimetype, bool purgeTempFile = true)
            {
                throw new NotImplementedException("Core Port CacheHandler.ExpireItem");
            }
            /*
            // store uploaded avatar
            public static async Task<bool> GenerateAvatar(Image inputImage, string userName, string mimetype, bool purgeTempFile = true)
            {
                try
                {
                    // store avatar locally
                    var originalImage = new KalikoImage(inputImage);
                    originalImage.Scale(new PadScaling(MaxWidth, MaxHeight)).SaveJpg(DestinationPathAvatars + '\\' + userName + ".jpg", 90);
                    if (!VoatSettings.Instance.UseContentDeliveryNetwork)
                    {
                        return true;
                    }

                    // call upload to storage since CDN is enabled in config
                    string tempAvatarLocation = DestinationPathAvatars + '\\' + userName + ".jpg";

                    // the avatar file was not found at expected path, abort
                    if (!FileSystemUtility.FileExists(tempAvatarLocation, DestinationPathAvatars))
                    {
                        return false;
                    }
                    else if (VoatSettings.Instance.UseContentDeliveryNetwork)
                    {
                        // upload to CDN
                        await CloudStorageUtility.UploadBlobToStorageAsync(tempAvatarLocation, "avatars");
                        if (purgeTempFile)
                        {
                            // delete local file after uploading to CDN
                            File.Delete(tempAvatarLocation);
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    EventLogger.Log(ex);
                    return false;
                }
            }
            */
                // Generate a random filename for a thumbnail and make sure that the file does not exist.
                private static string GenerateRandomFilename()
        {
            string rndFileName;

            // if CDN flag is active, check if file exists on CDN, otherwise check if file exists on local storage
            if (VoatSettings.Instance.UseContentDeliveryNetwork)
            {
                // make sure blob with same name doesn't exist already
                do
                {
                    rndFileName = Guid.NewGuid().ToString();
                } while (CloudStorageUtility.BlobExists(rndFileName, "thumbs"));

                rndFileName = Guid.NewGuid().ToString();
            }
            else
            {
                do
                {
                    rndFileName = Guid.NewGuid().ToString();
                } while (FileSystemUtility.FileExists(rndFileName, DestinationPathThumbs));
            }

            return rndFileName;
        }

        
        public static async Task<string> GenerateThumbFromWebpageUrl(string websiteUrl, bool purgeTempFile = true)
        {
            var extension = Path.GetExtension(websiteUrl);
            var imageExtensions = new string[] { ".jpg", ".png", ".gif", ".jpeg" };

            var thumbFileName = (string)null;

            try
            {
                if (imageExtensions.Any(x => x.IsEqual(extension)))
                {
                    // generate a thumbnail if submission is a direct link to image or video
                    thumbFileName = await GenerateThumbFromImageUrl(websiteUrl, purgeTempFile: purgeTempFile);
                }
                if (String.IsNullOrEmpty(thumbFileName))
                {
                    var graphUri = new Uri(websiteUrl);
                    var graph = OpenGraph.ParseUrl(graphUri, userAgent: "Voat.co OpenGraph Parser");

                    // open graph failed to find og:image element, abort thumbnail generation
                    if (graph.Image != null)
                    {
                        thumbFileName = await GenerateThumbFromImageUrl(graph.Image.ToString(), purgeTempFile: purgeTempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                // thumnail generation failed, skip adding thumbnail
                EventLogger.Log(ex);
            }
            return thumbFileName;
        }
    }
}
