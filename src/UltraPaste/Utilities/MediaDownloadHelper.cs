using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UltraPaste.Utilities
{
    internal class MediaDownloadHelper
    {
        private static readonly Regex RegexLinkYouTube = new Regex(@"^(https?://)?(www\.)?(youtube\.com|youtu\.?be)/.+$",RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegexIdYouTube = new Regex(@"(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})",RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegexLinkBilibili = new Regex(@"^(https?://)?(www\.)?(bilibili\.com|b23\.tv)/.+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RegexIdBilibili = new Regex(@"(?:bilibili\.com\/video\/(BV[a-zA-Z0-9]{10}|av\d+)|b23\.tv\/([a-zA-Z0-9]{7}))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsValidYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;

            }

            try
            {
                if (!RegexLinkYouTube.IsMatch(url))
                {
                    return false;
                }

                Match match = RegexIdYouTube.Match(url);
                if (!match.Success)
                {
                    return false;
                }

                string videoId = match.Groups[1].Value;
                return videoId.Length == 11;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsValidBilibiliUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                if (!RegexLinkBilibili.IsMatch(url))
                {
                    return false;
                }

                Match match = RegexIdBilibili.Match(url);
                if (!match.Success)
                {
                    return false;
                }

                string videoId = match.Groups[1].Value;
                if (videoId.StartsWith("BV", StringComparison.OrdinalIgnoreCase))
                {
                    return videoId.Length == 12;
                }
                else if (videoId.StartsWith("av", StringComparison.OrdinalIgnoreCase))
                {
                    return videoId.Length > 2 && long.TryParse(videoId.Substring(2), out _);
                }

                string shortCode = match.Groups[2].Value;
                if (!string.IsNullOrEmpty(shortCode))
                {
                    return shortCode.Length == 7;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
