using UnityEngine;

namespace UnityModule.Settings {

    public class XcodeArchiverSetting : Setting<XcodeArchiverSetting>, IEnvironmentSetting
    {
        [SerializeField]
        private bool shouldRunXcodeArchive = true;

        public bool ShouldRunXcodeArchive => shouldRunXcodeArchive;

        [SerializeField]
        private bool shouldExportAppStoreArchive = true;

        public bool ShouldExportAppStoreArchive => shouldExportAppStoreArchive;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Settings/Xcode Archiver Setting")]
        public static void CreateSettingAsset()
        {
            CreateAsset();
        }
#endif
    }

}