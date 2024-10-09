using UnityEditor;
using UnityEngine;

namespace WSMGameStudio.About
{
    public class WSM_ExternalLinks
    {
        [MenuItem("WSM Game Studio/Recommended Assets/More Assets", false, 2)]
        public static void MoreAssets()
        {
            OpenUrl("https://assetstore.unity.com/publishers/29374");
        }

        [MenuItem("WSM Game Studio/Recommended Assets/Train Controller Collection", false, 0)]
        public static void TrainControllerCollection()
        {
            OpenUrl("https://assetstore.unity.com/lists/train-controller-104802");
        }

        [MenuItem("WSM Game Studio/Recommended Assets/Heavy Machinery Collection", false, 1)]
        public static void HeavyMachineryCollection()
        {
            OpenUrl("https://assetstore.unity.com/lists/heavy-machinery-150323");
        }

        public static void Facebook()
        {
            OpenUrl("https://www.facebook.com/WSMGameStudio/");
        }

        public static void Instagram()
        {
            OpenUrl("https://www.instagram.com/wsmgamestudio/");
        }

        public static void Twitter()
        {
            OpenUrl("https://twitter.com/WSMatis");
        }

        public static void Discord()
        {
            OpenUrl("https://discord.gg/6qSAB4z");
        }

        public static void Steam()
        {
            OpenUrl("https://store.steampowered.com/app/871390/Rock_n_Rush_Battle_Racing/");
        }

        public static void SupportRequest()
        {
            OpenUrl("mailto:wsmgamestudio@gmail.com");
        }

        public static void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }
    } 
}
